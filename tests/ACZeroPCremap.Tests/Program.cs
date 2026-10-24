using System.Text;
using ACZeroPCremap.Core;
using ACZeroPCremap.Core.Localization;
using UiText = ACZeroPCremap.Core.Localization.Text;

var tests = new List<(string Name, Action Run)>();
void Test(string name, Action run) => tests.Add((name, run));
byte[] Bytes(string s) => new UTF8Encoding(false, true).GetBytes(s);
string Text(IniDocument document) => Encoding.UTF8.GetString(document.ToBytes());
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected: {expected}; actual: {actual}");
}
void SameBytes(byte[] expected, byte[] actual)
{
    if (!expected.AsSpan().SequenceEqual(actual)) throw new Exception("Byte sequences differ.");
}
void Throws<T>(Action run) where T : Exception
{
    try { run(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
void WithFile(Action<string> run)
{
    var dir = Path.Combine(Path.GetTempPath(), "ACZeroPCremap.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, "dinput.ini");
    File.WriteAllBytes(path, Bytes("; test\r\n[KeyboardFlight]\r\nA= 0x24 ;DIK_J Gun\r\nA= Mouse1 ; keep\r\n[0]\r\nFFB= one\r\nFFB= two\r\n"));
    try { run(path); }
    finally { Directory.Delete(dir, recursive: true); }
}
void Change(ConfigSession session) => session.Document.Replace("KeyboardFlight", session.Document.Entries("KeyboardFlight")[0].Id, "0x25");

var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dinput.ini"));
Test("Original fixture round-trips byte for byte", () => SameBytes(fixture, IniDocument.Parse(fixture).ToBytes()));
Test("BOM, mixed endings, blank lines and missing final newline survive", () =>
{
    byte[] bytes = [0xEF, 0xBB, 0xBF, .. Bytes("; Юникод\r\n[KeyboardUI]\nA=0x1C\r\n\r\n;tail")];
    SameBytes(bytes, IniDocument.Parse(bytes).ToBytes());
});
Test("Empty file survives", () => SameBytes([], IniDocument.Parse([]).ToBytes()));
Test("Non UTF-8 and binary documents are rejected", () =>
{
    Throws<InvalidDataException>(() => IniDocument.Parse([0xFF, 0xFE, 0x00, 0x00]));
    Throws<InvalidDataException>(() => IniDocument.Parse(Bytes("a\0b")));
});
Test("Repeated assignments remain separate entries", () =>
{
    var doc = IniDocument.Parse(fixture);
    Equal(3, doc.Entries("KeyboardUI").Count(e => e.Key == "A"));
    Equal(4, doc.Entries("0").Count(e => e.Key == "FFB"));
});
Test("Only selected binding and its DIK token change", () =>
{
    var doc = IniDocument.Parse(fixture);
    var entry = doc.Entries("KeyboardFlight").Single(e => e.Key == "A" && e.Value == "0x24");
    doc.Replace("KeyboardFlight", entry.Id, "0x25");
    var expected = Encoding.UTF8.GetString(fixture).Replace("A=    0x24 ;DIK_J", "A=    0x25 ;DIK_K");
    SameBytes(Bytes(expected), doc.ToBytes());
    Equal(true, doc.Entries("KeyboardFlight").Any(e => e.Key == "A" && e.Value == "Mouse1"));
});
Test("Mouse replacement removes stale DIK but retains other comment text", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardFlight]\nA= 0x24 ;DIK_J Gun\n"));
    doc.Replace("KeyboardFlight", doc.Entries("KeyboardFlight")[0].Id, "Mouse2");
    Equal("[KeyboardFlight]\nA= Mouse2 ; Gun\n", Text(doc));
});
Test("Arbitrary comments remain unchanged", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardFlight]\nA=0x24 ;my comment DIK_J\n"));
    doc.Replace("KeyboardFlight", doc.Entries("KeyboardFlight")[0].Id, "0x25");
    Equal("[KeyboardFlight]\nA=0x25 ;my comment DIK_J\n", Text(doc));
});
Test("Unknown complex bindings are read-only and preserved", () =>
{
    var original = Bytes("[KeyboardFlight]\nLX-=0x1D + 0x38 + MouseX:8:0\nA=UnknownMouse:2\n");
    var doc = IniDocument.Parse(original);
    var entry = doc.Entries("KeyboardFlight").First();
    Throws<InvalidOperationException>(() => doc.Replace("KeyboardFlight", entry.Id, "0x25"));
    Throws<InvalidOperationException>(() => doc.Remove("KeyboardFlight", entry.Id));
    SameBytes(original, doc.ToBytes());
});
Test("Unknown keys and unknown DIK codes are read-only", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\nNewAction=0x24\nA=0xFF\n"));
    Equal(true, doc.Entries("KeyboardUI").All(e => !e.Editable));
});
Test("New entry goes after the last binding for that action", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardFlight]\r\nA=Mouse1\r\nB=Mouse2\r\n"));
    doc.Add("KeyboardFlight", "A", "0x24");
    Equal("[KeyboardFlight]\r\nA=Mouse1\r\nA= 0x24 ;DIK_J\r\nB=Mouse2\r\n", Text(doc));
});
Test("Adding an absent action does not change the next section", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\nA=0x1C\n\n[device]\nA=B1\n"));
    doc.Add("KeyboardUI", "B", "Mouse2");
    Equal("[KeyboardUI]\nA=0x1C\nB= Mouse2\n\n[device]\nA=B1\n", Text(doc));
});
Test("Adding at EOF preserves absence of final newline", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\nA=0x1C"));
    doc.Add("KeyboardUI", "B", "Mouse2");
    Equal("[KeyboardUI]\nA=0x1C\nB= Mouse2", Text(doc));
});
Test("Deleting one alternative preserves the others", () =>
{
    var doc = IniDocument.Parse(fixture);
    var entry = doc.Entries("KeyboardUI").First(e => e.Key == "A");
    doc.Remove("KeyboardUI", entry.Id);
    Equal(2, doc.Entries("KeyboardUI").Count(e => e.Key == "A"));
});
Test("Duplicate assignment is rejected without modifying document", () =>
{
    var doc = IniDocument.Parse(fixture);
    Throws<InvalidOperationException>(() => doc.Add("KeyboardFlight", "A", "mouse1"));
    SameBytes(fixture, doc.ToBytes());
});
Test("Applying unchanged value preserves existing duplicate rows", () =>
{
    var bytes = Bytes("[KeyboardUI]\nA=0x24\nA=0x24\n");
    var doc = IniDocument.Parse(bytes);
    doc.Replace("KeyboardUI", doc.Entries("KeyboardUI")[0].Id, "0x24");
    SameBytes(bytes, doc.ToBytes());
});
Test("Removing last line preserves missing final newline", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\nA=0x24\nB=0x25"));
    doc.Remove("KeyboardUI", doc.Entries("KeyboardUI")[1].Id);
    Equal("[KeyboardUI]\nA=0x24", Text(doc));
});
Test("Missing, repeated and device sections cannot be edited", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\nA=0x1C\n[keyboardui]\nA=0x39\n[0]\nA=B1\n"));
    Equal(2, doc.SectionCount("KeyboardUI"));
    Throws<InvalidOperationException>(() => doc.Add("KeyboardUI", "B", "Mouse2"));
    Throws<InvalidOperationException>(() => doc.Add("KeyboardFlight", "B", "Mouse2"));
    Throws<InvalidOperationException>(() => doc.Add("0", "B", "Mouse2"));
});
Test("Commented bindings do not become active", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardUI]\n ;A=0x1C\nA=0x39\n"));
    Equal(1, doc.Entries("KeyboardUI").Count);
});
Test("Conflicts are per section and include intended LB/RB sharing", () =>
{
    var doc = IniDocument.Parse(fixture);
    Equal(true, doc.Conflicts("KeyboardFlight").Any(c => c.InputName == "Z" && c.Actions.Contains("LB") && c.Actions.Contains("RB")));
    var separate = IniDocument.Parse(Bytes("[KeyboardUI]\nA=0x24\n[KeyboardFlight]\nB=0x24\n"));
    Equal(0, separate.Conflicts("KeyboardUI").Count);
});
Test("Save backs up exact original bytes and reloads edited values", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path);
    Change(session);
    Equal(true, session.IsDirty);
    var backup = session.Save()!;
    SameBytes(original, File.ReadAllBytes(backup));
    Equal(false, session.IsDirty);
    Equal("0x25", ConfigSession.Open(path).Document.Entries("KeyboardFlight")[0].Value);
    Equal("Mouse1", session.Document.Entries("KeyboardFlight")[1].Value);
}));
Test("Save without edits never writes or creates a backup", () => WithFile(path =>
{
    var session = ConfigSession.Open(path);
    var time = File.GetLastWriteTimeUtc(path);
    Equal<string?>(null, session.Save());
    Equal(time, File.GetLastWriteTimeUtc(path));
    Equal(0, Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak").Length);
}));
Test("External edit prevents overwrite and retains local changes", () => WithFile(path =>
{
    var session = ConfigSession.Open(path);
    Change(session);
    var external = Bytes("[KeyboardFlight]\nA=Mouse3\n");
    File.WriteAllBytes(path, external);
    Throws<ExternalChangeException>(() => session.Save());
    SameBytes(external, File.ReadAllBytes(path));
    Equal(true, session.IsDirty);
    Equal(0, Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak").Length);
}));
Test("Discard only resets memory", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path);
    Change(session);
    session.DiscardChanges();
    SameBytes(original, session.Document.ToBytes());
    SameBytes(original, File.ReadAllBytes(path));
}));
Test("Restore is exact and backs up the replaced file", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path);
    Change(session);
    var firstBackup = session.Save()!;
    var edited = File.ReadAllBytes(path);
    var restoreBackup = session.Restore(firstBackup);
    SameBytes(original, File.ReadAllBytes(path));
    SameBytes(edited, File.ReadAllBytes(restoreBackup));
    Equal(false, session.IsDirty);
}));
Test("Restore refuses dirty documents and unsupported backup contents", () => WithFile(path =>
{
    var session = ConfigSession.Open(path);
    Change(session);
    Throws<InvalidOperationException>(() => session.Restore(path + ".bak"));
    session.DiscardChanges();
    File.WriteAllText(path + ".bak", "[Unrelated]\nx=y\n");
    Throws<InvalidDataException>(() => session.Restore(path + ".bak"));
}));
Test("Backup failure blocks save before writing INI", () => WithFile(path =>
{
    var clock = new FixedClock();
    var blocked = path + "." + clock.GetLocalNow().ToString("yyyyMMdd-HHmmss-fff") + ".bak";
    Directory.CreateDirectory(blocked);
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path, clock);
    Change(session);
    Throws<UnauthorizedAccessException>(() => session.Save());
    SameBytes(original, File.ReadAllBytes(path));
    Equal(true, session.IsDirty);
}));
Test("Replacement failure preserves original and dirty state", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path);
    Change(session);
    using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        Throws<IOException>(() => session.Save());
    SameBytes(original, File.ReadAllBytes(path));
    Equal(true, session.IsDirty);
    Equal(0, Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length);
}));
Test("Backup filename collisions never overwrite old copies", () => WithFile(path =>
{
    var session = ConfigSession.Open(path, new FixedClock());
    Change(session);
    var first = session.Save()!;
    var originalBackup = File.ReadAllBytes(first);
    session.Document.Add("KeyboardFlight", "B", "Mouse2");
    var second = session.Save()!;
    Equal(false, first == second);
    SameBytes(originalBackup, File.ReadAllBytes(first));
}));
Test("Scan codes distinguish extended keys and special DIK values", () =>
{
    Equal("0x1D", KeyScanCode.ToDik(0x11, 0x1D, false));
    Equal("0x9D", KeyScanCode.ToDik(0x11, 0x1D, true));
    Equal("0x2A", KeyScanCode.ToDik(0x10, 0x2A, false));
    Equal("0x36", KeyScanCode.ToDik(0x10, 0x36, false));
    Equal("0x38", KeyScanCode.ToDik(0x12, 0x38, false));
    Equal("0xB8", KeyScanCode.ToDik(0x12, 0x38, true));
    Equal("0x1C", KeyScanCode.ToDik(0x0D, 0x1C, false));
    Equal("0x9C", KeyScanCode.ToDik(0x0D, 0x1C, true));
    Equal("0x48", KeyScanCode.ToDik(0x68, 0x48, false));
    Equal("0xC8", KeyScanCode.ToDik(0x26, 0x48, true));
    Equal("0x45", KeyScanCode.ToDik(0x90, 0x45, true));
    Equal("0xC5", KeyScanCode.ToDik(0x13, 0x45, false));
    Equal("0xB7", KeyScanCode.ToDik(0x2C, 0x37, true));
    Equal<string?>(null, KeyScanCode.ToDik(0xFF, 0, false));
});
Test("Every keyboard display label has a locale-independent DIK token", () =>
{
    Equal(true, Bindings.Choices.Where(c => c.Value.StartsWith("0x")).All(c => Bindings.DikToken(c.Value)?.StartsWith("DIK_") == true));
    Equal("DIK_NUMPADENTER", Bindings.DikToken("0x9C"));
    Equal("DIK_LMENU", Bindings.DikToken("0x38"));
    Equal("DIK_TAB", Bindings.DikToken("0x0F"));
    Equal("DIK_NUMPAD4", Bindings.DikToken("0x4B"));
});

Test("Device sections expose no editable rows and reject every mutation", () =>
{
    var bytes = Bytes("[KeyboardFlight]\nA=0x24\n[0]\nA=0x24\nFFB=one\nFFB=two\n[1234abcd]\nA=B1\n[1234abcd]\nB=HoriSP:0:1\n");
    var doc = IniDocument.Parse(bytes);
    foreach (var section in new[] { "0", "1234abcd" })
    {
        Equal(true, doc.Entries(section).All(e => !e.Editable));
        Throws<InvalidOperationException>(() => doc.Add(section, "A", "0x25"));
        foreach (var entry in doc.Entries(section))
        {
            Throws<InvalidOperationException>(() => doc.Replace(section, entry.Id, "0x25"));
            Throws<InvalidOperationException>(() => doc.Remove(section, entry.Id));
        }
    }
    SameBytes(bytes, doc.ToBytes());
});
Test("Keyboard and mouse edits save without changing any opaque device data", () => WithFile(path =>
{
    const string before = "[0]\r\nName=Pad\r\nA=B2\r\nFFB=one\r\nFFB=two\r\n";
    const string middle = "[1234abcd]\nName=Device\nLX-=!B10 + X:32640:16\n[1234abcd]\nA=HoriSP:0:1\n";
    const string after = "[Unknown]\r\nA=0x24\r\nCustom= value ; keep";
    var originalText = before + "[KeyboardFlight]\r\nA= 0x24 ;DIK_J Gun\r\nA=Mouse1 ; mouse\r\nB=0x25\r\n" +
        middle + "[KeyboardUI]\nA=0x1C\n" + after;
    byte[] original = [0xEF, 0xBB, 0xBF, .. Bytes(originalText)];
    File.WriteAllBytes(path, original);
    var session = ConfigSession.Open(path);
    var doc = session.Document;
    doc.Replace("KeyboardFlight", doc.Entries("KeyboardFlight").First().Id, "0x25");
    doc.Remove("KeyboardFlight", doc.Entries("KeyboardFlight").Single(e => e.Key == "B").Id);
    doc.Add("KeyboardUI", "B", "Mouse2");
    doc.SetFlightMouseEnabled(false);
    var expected = originalText.Replace("A= 0x24 ;DIK_J Gun", "A= 0x25 ;DIK_K Gun")
        .Replace("B=0x25\r\n", "")
        .Replace("A=Mouse1 ; mouse", "; ACZeroPCremap mouse disabled: A=Mouse1 ; mouse")
        .Replace("[KeyboardUI]\nA=0x1C\n", "[KeyboardUI]\nA=0x1C\nB= Mouse2\r\n");
    byte[] expectedBytes = [0xEF, 0xBB, 0xBF, .. Bytes(expected)];
    var backup = session.Save()!;
    SameBytes(original, File.ReadAllBytes(backup));
    SameBytes(expectedBytes, File.ReadAllBytes(path));
    var reopened = ConfigSession.Open(path);
    reopened.Document.SetFlightMouseEnabled(true);
    reopened.Save();
    SameBytes([0xEF, 0xBB, 0xBF, .. Bytes(expected.Replace("; ACZeroPCremap mouse disabled: ", ""))], File.ReadAllBytes(path));
}));
Test("Whole-file restore retains compatibility with device-only backups", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var deviceOnly = Bytes("[0]\nA=B1\nFFB=one\nFFB=two\n[1234abcd]\nA=HoriSP:0:1");
    var selectedBackup = path + ".selected.bak";
    File.WriteAllBytes(selectedBackup, deviceOnly);
    var session = ConfigSession.Open(path);
    var backup = session.Restore(selectedBackup);
    SameBytes(original, File.ReadAllBytes(backup));
    SameBytes(deviceOnly, File.ReadAllBytes(path));
    Equal(true, session.Document.Entries("0").All(e => !e.Editable));
}));

Test("Portable preferences resolve beside the EXE independently of the working directory", () => WithFile(path =>
{
    var previous = Environment.CurrentDirectory;
    try
    {
        Environment.CurrentDirectory = Path.GetDirectoryName(path)!;
        Equal(Path.Combine(AppContext.BaseDirectory, "ACZeroPCremap.settings.json"), LanguagePreferences.DefaultPath);
    }
    finally { Environment.CurrentDirectory = previous; }
}));
Test("Legacy ACZeroRemap mouse-disable comments restore after the rename", () =>
{
    var original = Bytes("[KeyboardFlight]\r\nA=Mouse1 ; keep\r\nLX-=!0x38 + MouseX:8:0\n[KeyboardUI]\nA=Mouse1\n");
    var disabled = Bytes("[KeyboardFlight]\r\n; ACZeroRemap mouse disabled: A=Mouse1 ; keep\r\n; ACZeroRemap mouse disabled: LX-=!0x38 + MouseX:8:0\n[KeyboardUI]\nA=Mouse1\n");
    var document = IniDocument.Parse(disabled);
    Equal(true, document.HasDisabledFlightMouseBindings);
    document.SetFlightMouseEnabled(false);
    SameBytes(disabled, document.ToBytes());
    document.SetFlightMouseEnabled(true);
    SameBytes(original, document.ToBytes());
});
Test("Six requested languages are available and preference writes never touch the INI", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var preferences = new LanguagePreferences(path + ".settings.json");
    Equal("en", preferences.Load());
    Equal("en,ru,es-ES,pt-BR,de,fr", string.Join(',', LanguagePreferences.Languages.Select(l => l.Code)));
    foreach (var language in LanguagePreferences.Languages)
    {
        preferences.Save(language.Code);
        Equal(language.Code, new LanguagePreferences(preferences.FilePath).Load());
    }
    SameBytes(original, File.ReadAllBytes(path));
    Equal(0, Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length);
    Throws<ArgumentException>(() => preferences.Save("zh-Hans"));
}));
Test("Invalid, missing or unsupported preferences fall back to English", () => WithFile(path =>
{
    var preferences = new LanguagePreferences(path + ".settings.json");
    foreach (var json in new[] { "{", "null", "{}", "{\"Language\":\"zh-Hans\"}", "{\"Language\":null}", "{\"Language\":123}" })
    {
        File.WriteAllText(preferences.FilePath, json);
        Equal("en", preferences.Load());
    }
    Equal("en", LanguagePreferences.CultureFor("unsupported").Name);
}));
Test("Window placement and language share portable preferences without overwriting each other", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var preferences = new LanguagePreferences(path + ".settings.json");
    File.WriteAllText(preferences.FilePath, "{\"Language\":\"ru\"}");
    Equal<WindowPlacement?>(null, preferences.LoadWindowPlacement());
    var placement = new WindowPlacement(-1800, 100, 1320, 900, 1.5, true);
    preferences.SaveWindowPlacement(placement);
    Equal("ru", preferences.Load());
    Equal(placement, preferences.LoadWindowPlacement());
    preferences.Save("fr");
    Equal("fr", preferences.Load());
    Equal(placement, new LanguagePreferences(preferences.FilePath).LoadWindowPlacement());
    SameBytes(original, File.ReadAllBytes(path));
    Equal(0, Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length);
}));
Test("Invalid saved window bounds are ignored", () => WithFile(path =>
{
    var preferences = new LanguagePreferences(path + ".settings.json");
    File.WriteAllText(preferences.FilePath, "{\"Language\":\"de\",\"Window\":{\"Left\":0,\"Top\":0,\"Width\":-1,\"Height\":600}}");
    Equal("de", preferences.Load());
    Equal<WindowPlacement?>(null, preferences.LoadWindowPlacement());
    Throws<ArgumentException>(() => preferences.SaveWindowPlacement(new(0, 0, double.NaN, 600)));
}));
Test("Window bounds fit common screen resolutions and DPI scales including saved oversized windows", () =>
{
    foreach (var (width, height) in new[] { (1366d, 720d), (1920d, 1040d), (1280d, 680d) })
    foreach (var scale in new[] { 1d, 1.25, 1.5 })
    foreach (var saved in new WindowPlacement?[] { null, new(-50, -20, 2400, 1800), new(1300, 700, 1100, 820) })
    {
        var screen = new ScreenWorkArea(0, 0, width, height, scale);
        var fitted = WindowLayout.Fit(saved, screen);
        Equal(true, fitted.IsValid && fitted.Left >= 0 && fitted.Top >= 0 &&
            fitted.Left + fitted.Width <= width && fitted.Top + fitted.Height <= height);
    }
    var normal = WindowLayout.Fit(null, new(0, 0, 1920, 1040));
    Equal(1100d, normal.Width); Equal(820d, normal.Height);
});
Test("Window restore handles removed monitors, negative coordinates and DPI changes", () =>
{
    var restored = WindowLayout.Fit(new(-1700, 80, 900, 650), new(0, 0, 1920, 1040));
    Equal(510d, restored.Left); Equal(195d, restored.Top);
    var secondary = WindowLayout.Fit(new(-1700, 80, 900, 650), new(-1920, 0, 1920, 1040));
    Equal(-1700d, secondary.Left); Equal(80d, secondary.Top);
    var scaled = WindowLayout.Fit(new(100, 100, 900, 650, 1, true), new(0, 0, 3840, 2100, 1.5));
    Equal(1350d, scaled.Width); Equal(975d, scaled.Height); Equal(true, scaled.Maximized);
    var tiny = WindowLayout.Fit(null, new(0, 0, 800, 560, 2));
    Equal(true, tiny.Width < 800 && tiny.Height < 560);
});
Test("Every language contains every resource and preserves format placeholders", () =>
{
    var resources = new System.Resources.ResourceManager("ACZeroPCremap.Core.Localization.Strings", typeof(UiText).Assembly);
    var english = resources.GetResourceSet(System.Globalization.CultureInfo.GetCultureInfo("en"), true, false)!;
    var keys = english.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key).Order().ToArray();
    string Placeholders(string value) => string.Join(',', System.Text.RegularExpressions.Regex.Matches(value, @"\{([0-9]+)[^}]*\}")
        .Select(m => m.Groups[1].Value).Order());
    foreach (var language in LanguagePreferences.Languages)
    {
        var culture = LanguagePreferences.CultureFor(language.Code);
        var localized = resources.GetResourceSet(culture, true, false) ?? throw new Exception("Missing exact resource set: " + language.Code);
        Equal(string.Join('|', keys), string.Join('|', localized.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key).Order()));
        foreach (var key in keys)
        {
            var value = localized.GetString(key)!;
            Equal(false, string.IsNullOrWhiteSpace(value));
            Equal(Placeholders(english.GetString(key)!), Placeholders(value));
            _ = string.Format(culture, value, "A", "B", "C");
        }
    }
});
Test("Changing language localizes inputs and leaves edited INI bytes identical", () =>
{
    var originalCulture = UiText.Culture;
    byte[]? expected = null;
    try
    {
        foreach (var language in LanguagePreferences.Languages)
        {
            UiText.Culture = LanguagePreferences.CultureFor(language.Code);
            var document = IniDocument.Parse(fixture);
            var entry = document.Entries("KeyboardFlight").Single(e => e.Key == "A" && e.Value == "0x24");
            document.Replace("KeyboardFlight", entry.Id, "0x25");
            expected ??= document.ToBytes();
            SameBytes(expected, document.ToBytes());
            Equal(UiText.Get("Key.39"), Bindings.Find("0x39")!.Name);
            Equal("DIK_SPACE", Bindings.DikToken("0x39"));
            Equal("MouseX:0:8", MouseAxes.Create(MouseDirection.Right));
        }
    }
    finally { UiText.Culture = originalCulture; }
});

Test("Flight mouse switch disables every mouse input without changing other sections", () =>
{
    var document = IniDocument.Parse(fixture);
    var flight = document.Entries("KeyboardFlight");
    var originalKeyboard = flight.Where(e => !Bindings.UsesMouse(e.Value)).Select(e => e.Id).ToArray();
    var otherSections = Encoding.UTF8.GetString(fixture).Split("[KeyboardFlight]")[0];
    Equal(true, document.HasFlightMouseBindings);
    document.SetFlightMouseEnabled(false);
    Equal(false, document.HasFlightMouseBindings);
    Equal(true, document.HasDisabledFlightMouseBindings);
    Equal(string.Join(',', originalKeyboard), string.Join(',', document.Entries("KeyboardFlight").Select(e => e.Id)));
    Equal(true, Text(document).StartsWith(otherSections));
    Equal(true, document.Entries("KeyboardUI").Any(e => Bindings.UsesMouse(e.Value)));
    document.SetFlightMouseEnabled(true);
    SameBytes(fixture, document.ToBytes());
});
Test("Mouse switch preserves BOM, comments, mixed line endings and missing final newline", () =>
{
    byte[] bytes = [0xEF, 0xBB, 0xBF, .. Bytes("[KeyboardFlight]\r\n A= Mouse1 ; keep\nLX-= !0x38 + mouseX:8:0\rX=MouseZ:0:1\nB=0x26 ; Mouse2 is only a comment\n; A=Mouse3\n[KeyboardUI]\nA=Mouse1\n[0]\nA=B2")];
    var document = IniDocument.Parse(bytes);
    document.SetFlightMouseEnabled(false);
    var disabled = document.ToBytes();
    document.SetFlightMouseEnabled(false);
    SameBytes(disabled, document.ToBytes());
    Equal(1, document.Entries("KeyboardFlight").Count);
    Equal("0x26", document.Entries("KeyboardFlight").Single().Value);
    document.SetFlightMouseEnabled(true);
    SameBytes(bytes, document.ToBytes());
    document.SetFlightMouseEnabled(true);
    SameBytes(bytes, document.ToBytes());
});
Test("Mouse detection ignores identifiers and comments that only mention a mouse", () =>
{
    Equal(false, Bindings.UsesMouse("NotMouse1"));
    Equal(false, Bindings.UsesMouse("SomeMouseX:0:1"));
    foreach (var value in new[] { "Mouse1", "mouse3", "MouseZ:0:1", "!0x38 + MouseAY:256:0", "Mouse4", "MouseWheel:0:1" })
        Equal(true, Bindings.UsesMouse(value));
    var document = IniDocument.Parse(Bytes("[KeyboardFlight]\nA=0x24 ;Mouse1\n;A=Mouse2\n"));
    var original = document.ToBytes();
    document.SetFlightMouseEnabled(false);
    document.SetFlightMouseEnabled(true);
    Equal(false, document.HasFlightMouseBindings);
    Equal(false, document.HasDisabledFlightMouseBindings);
    SameBytes(original, document.ToBytes());
});
Test("Mouse switch refuses missing or repeated flight sections without mutation", () =>
{
    foreach (var content in new[] { "[KeyboardUI]\nA=Mouse1\n", "[KeyboardFlight]\nA=Mouse1\n[keyboardflight]\nB=Mouse2\n" })
    {
        var document = IniDocument.Parse(Bytes(content));
        var original = document.ToBytes();
        Throws<InvalidOperationException>(() => document.SetFlightMouseEnabled(false));
        Throws<InvalidOperationException>(() => document.SetFlightMouseEnabled(true));
        SameBytes(original, document.ToBytes());
    }
});
Test("Disabled flight mouse bindings survive saving and reopening and restore exactly", () => WithFile(path =>
{
    File.WriteAllBytes(path, fixture);
    var session = ConfigSession.Open(path);
    session.Document.SetFlightMouseEnabled(false);
    Equal(true, session.IsDirty);
    var backup = session.Save()!;
    SameBytes(fixture, File.ReadAllBytes(backup));
    session = ConfigSession.Open(path);
    Equal(false, session.Document.HasFlightMouseBindings);
    Equal(true, session.Document.HasDisabledFlightMouseBindings);
    session.Document.SetFlightMouseEnabled(true);
    session.Save();
    SameBytes(fixture, File.ReadAllBytes(path));
}));
Test("Restoring flight mouse preserves keyboard edits made while the mouse is disabled", () =>
{
    var document = IniDocument.Parse(fixture);
    document.SetFlightMouseEnabled(false);
    var entry = document.Entries("KeyboardFlight").Single(e => e.Key == "A");
    document.Replace("KeyboardFlight", entry.Id, "0x25");
    document.SetFlightMouseEnabled(true);
    SameBytes(Bytes(Encoding.UTF8.GetString(fixture).Replace("A=    0x24 ;DIK_J", "A=    0x25 ;DIK_K")), document.ToBytes());
});
Test("Shared input lookup is scoped to one keyboard tab", () =>
{
    var document = IniDocument.Parse(Bytes("[KeyboardFlight]\nA=0x24\nA=Mouse1\nB=0x25\n[KeyboardUI]\nB=0x24\n[0]\nX=B1\n[1234abcd]\nY=B1\n"));
    Equal("A", string.Join(',', document.ConflictingActions("KeyboardFlight", "B", "0x24")));
    Equal(0, document.ConflictingActions("KeyboardFlight", "A", "0x24").Count);
    Equal(0, document.ConflictingActions("KeyboardUI", "B", "0x24").Count);
    Equal(0, document.ConflictingActions("0", "B", "B1").Count);
    Equal(0, document.ConflictingActions("1234abcd", "B", "B1").Count);
    Equal("A", string.Join(',', document.ConflictingActions("KeyboardFlight", "B", "Mouse1")));
});
Test("Shared input warnings include digital strengths without treating modifiers as standalone bindings", () =>
{
    var document = IniDocument.Parse(Bytes("[KeyboardFlight]\nRX-=0x47:16384\nLX-= !0x38 + MouseX:8:0\n[0]\nA=B1:32767\n"));
    Equal("RX-", string.Join(',', document.ConflictingActions("KeyboardFlight", "Y", "0x47")));
    Equal(0, document.ConflictingActions("KeyboardFlight", "Y", "0x38").Count);
    Equal(true, document.SharedInputWarning("KeyboardFlight", "Y", "0x47").Contains(Bindings.ActionName("KeyboardFlight", "RX-")));
    Equal("", document.SharedInputWarning("KeyboardFlight", "RX-", "0x47"));
});

Test("Supplied mouse axes are recognized without changing their bytes", () =>
{
    var doc = IniDocument.Parse(fixture);
    var count = 0;
    foreach (var section in new[] { "KeyboardFlight", "KeyboardUI" })
        foreach (var entry in doc.Entries(section).Where(e => MouseAxes.Parse(e.Value) is not null))
        {
            Equal(true, entry.Editable);
            count++;
        }
    Equal(true, count >= 10);
    SameBytes(fixture, doc.ToBytes());
});
Test("Mouse axis parser rejects unknown conditions, bad ranges and line injection", () =>
{
    foreach (var value in new[] { "MouseX:0:0", "MouseX:1:2", "MouseX:-8:0", "MouseX:65536:0", "MouseX:8:0\n",
        "MouseX:8:0\r\n[0]", "0x38\n + MouseX:8:0", "MouseX:\r\n8:0", "MouseX:8:0; comment", "MouseX:0:999999999999", "0xFF + MouseY:2:0",
        "0x1D + 0x38 + MouseX:8:0", "MouseAXX:0:256", "Mouse4", "!0x38" })
        Equal<MouseAxisBinding?>(null, MouseAxes.Parse(value));
    Equal(true, MouseAxes.Parse("!0x38 + MouseX:8:0") is not null);
    Equal(true, MouseAxes.Parse("0x38 + MouseAY:0:256")?.IsAbsolute == true);
    Equal(true, MouseAxes.Parse("MouseZ:1:0")?.IsWheel == true);
});
Test("Axis capture creates directions and retains existing mode, coefficient and condition", () =>
{
    Equal("MouseX:8:0", MouseAxes.Create(MouseDirection.Left));
    Equal("MouseX:0:8", MouseAxes.Create(MouseDirection.Right));
    Equal("MouseY:2:0", MouseAxes.Create(MouseDirection.Up));
    Equal("MouseY:0:2", MouseAxes.Create(MouseDirection.Down));
    Equal("MouseZ:0:1", MouseAxes.Create(MouseDirection.WheelUp));
    Equal("MouseZ:1:0", MouseAxes.Create(MouseDirection.WheelDown));
    Equal("!0x38 + MouseX:0:12", MouseAxes.Create(MouseDirection.Right, "!0x38 + MouseY:12:0"));
    Equal("0x38 + MouseAY:256:0", MouseAxes.Create(MouseDirection.Up, "0x38 + MouseAX:0:256"));
    Equal("MouseZ:0:4", MouseAxes.Create(MouseDirection.WheelUp, "MouseZ:4:0"));
});
Test("Axis replacement changes only its value and retains comments and device data", () =>
{
    var original = "[KeyboardFlight]\r\nRX-= 0x38 + MouseAX:256:0 ;DIK_LMENU(Alt) + Mouse Abs\r\nA=0x24\r\n[0]\nA=B1\nFFB=one\nFFB=two";
    var doc = IniDocument.Parse(Bytes(original));
    var entry = doc.Entries("KeyboardFlight").First();
    doc.Replace("KeyboardFlight", entry.Id, MouseAxes.Create(MouseDirection.Up, entry.Value));
    SameBytes(Bytes(original.Replace("MouseAX:256:0", "MouseAY:256:0")), doc.ToBytes());
});
Test("Equivalent axis expressions preserve bytes and duplicate alternatives are rejected", () =>
{
    var original = Bytes("[KeyboardFlight]\nLX-= !0x38 + mousex : 0008 : 0 ; keep\n");
    var doc = IniDocument.Parse(original);
    doc.Replace("KeyboardFlight", doc.Entries("KeyboardFlight").Single().Id, "!0x38+MouseX:8:0");
    SameBytes(original, doc.ToBytes());
    Throws<InvalidOperationException>(() => doc.Add("KeyboardFlight", "LX-", "!0x38+MouseX:8:0"));
    SameBytes(original, doc.ToBytes());
    Equal(0, doc.ConflictingActions("KeyboardFlight", "RX-", "0x38 + MouseX:8:0").Count);
    Equal("LX-", string.Join(',', doc.ConflictingActions("KeyboardFlight", "RX-", "!0x38 + MouseX:8:0")));
});
Test("Changing an axis condition updates only its initial DIK comment token", () =>
{
    var doc = IniDocument.Parse(Bytes("[KeyboardFlight]\nLX-= !0x38 + MouseX:8:0 ;!DIK_LMENU custom\n"));
    doc.Replace("KeyboardFlight", doc.Entries("KeyboardFlight").Single().Id, "0x1D + MouseX:8:0");
    Equal("[KeyboardFlight]\nLX-= 0x1D + MouseX:8:0 ;DIK_LCONTROL custom\n", Text(doc));
});
Test("New wheel and movement bindings survive save, mouse toggle and whole-file restore", () => WithFile(path =>
{
    var original = File.ReadAllBytes(path);
    var session = ConfigSession.Open(path);
    session.Document.Add("KeyboardFlight", "X", "MouseZ:0:1");
    session.Document.Add("KeyboardFlight", "LX-", "!0x38 + MouseX:8:0");
    var edited = session.Document.ToBytes();
    var backup = session.Save()!;
    SameBytes(original, File.ReadAllBytes(backup));
    var reopened = ConfigSession.Open(path);
    Equal(true, reopened.Document.Entries("KeyboardFlight").Any(e => e.Value == "MouseZ:0:1" && e.Editable));
    reopened.Document.SetFlightMouseEnabled(false);
    reopened.Save();
    reopened = ConfigSession.Open(path);
    reopened.Document.SetFlightMouseEnabled(true);
    SameBytes(edited, reopened.Document.ToBytes());
    reopened.DiscardChanges();
    reopened.Restore(backup);
    SameBytes(original, File.ReadAllBytes(path));
}));
Test("Mouse movement capture waits for arming and ignores jitter and ambiguous diagonals", () =>
{
    var capture = new MouseMovementCapture(1000);
    Equal<MouseDirection?>(null, capture.Observe(10, 10, 1100));
    Equal<MouseDirection?>(null, capture.Observe(100, 100, 1250));
    Equal<MouseDirection?>(null, capture.Observe(105, 103, 1300));
    Equal<MouseDirection?>(null, capture.Observe(130, 130, 1350));
    Equal<MouseDirection?>(null, capture.Observe(double.NaN, 0, 1360));
    Equal<MouseDirection?>(MouseDirection.Right, capture.Observe(145, 105, 1400));
    capture.ResetOrigin();
    Equal<MouseDirection?>(null, capture.Observe(300, 300, 1450));
    Equal<MouseDirection?>(MouseDirection.Up, capture.Observe(300, 260, 1500));
});
Test("Mouse movement capture distinguishes all four directions", () =>
{
    foreach (var (x, y, expected) in new[] { (50d, 100d, MouseDirection.Left), (150d, 100d, MouseDirection.Right),
        (100d, 50d, MouseDirection.Up), (100d, 150d, MouseDirection.Down) })
    {
        var capture = new MouseMovementCapture(0);
        capture.Observe(100, 100, 250);
        Equal<MouseDirection?>(expected, capture.Observe(x, y, 300));
    }
});

var failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{ex}"); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} checks passed.");
return failed == 0 ? 0 : 1;

sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
}
