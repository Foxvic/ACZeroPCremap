using ACZeroPCremap.Core.Localization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACZeroPCremap.Core;

public sealed record BindingEntry(Guid Id, string Section, string Key, string Value, string Comment)
{
    public bool Editable => Bindings.IsKeyboardSection(Section) && Bindings.IsKnownAction(Key) && Bindings.FindEditable(Value) is not null;
    public string DisplayName => Bindings.FindEditable(Value)?.Name ?? Value;
}

public sealed record BindingConflict(string InputName, IReadOnlyList<string> Actions);

public sealed class IniDocument
{
    private sealed class Line(string text, string ending)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Text { get; set; } = text;
        public string Ending { get; set; } = ending;
    }

    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly Regex Header = new(@"^\s*\[([^\]]+)\]\s*(?:;.*)?$", RegexOptions.Compiled);
    private static readonly Regex Assignment = new(@"^(\s*[^;=\s][^=]*?\s*=\s*)([^;]*?)(\s*)(;.*)?$", RegexOptions.Compiled);
    private readonly List<Line> lines;
    private readonly bool bom;
    private readonly string newLine;
    private readonly bool finalNewLine;
    private const string MouseDisabledPrefix = "; ACZeroPCremap mouse disabled: ";
    private const string LegacyMouseDisabledPrefix = "; ACZeroRemap mouse disabled: ";

    private IniDocument(List<Line> lines, bool bom)
    {
        this.lines = lines;
        this.bom = bom;
        newLine = lines.FirstOrDefault(l => l.Ending.Length > 0)?.Ending ?? "\r\n";
        finalNewLine = lines.LastOrDefault()?.Ending.Length > 0;
    }

    public static IniDocument Parse(byte[] bytes)
    {
        var bom = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF });
        string text;
        try { text = Utf8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0)); }
        catch (DecoderFallbackException ex) { throw new InvalidDataException(Text.Get("Error.Utf8Only"), ex); }
        if (text.Contains('\0')) throw new InvalidDataException(Text.Get("Error.BinaryFile"));
        var lines = new List<Line>();
        foreach (Match match in Regex.Matches(text, @"([^\r\n]*)(\r\n|\n|\r|$)"))
            if (match.Length > 0) lines.Add(new(match.Groups[1].Value, match.Groups[2].Value));
        return new(lines, bom);
    }

    public byte[] ToBytes()
    {
        var body = Utf8.GetBytes(string.Concat(lines.Select(l => l.Text + l.Ending)));
        return bom ? [0xEF, 0xBB, 0xBF, .. body] : body;
    }

    public int SectionCount(string section) => lines.Count(l => Header.Match(l.Text) is { Success: true } m && Equal(m.Groups[1].Value.Trim(), section));

    // Retain recognition of legacy device-only backups for whole-file restore.
    // Device sections are opaque data; no device editing API is exposed.
    internal bool HasDeviceSections => lines.Any(l => Header.Match(l.Text) is { Success: true } match &&
        Regex.IsMatch(match.Groups[1].Value.Trim(), @"^(0|[0-9a-fA-F]{8})$"));

    public string? SectionProblem(string section) => SectionCount(section) switch
    {
        0 => Text.Format("Error.MissingSection", section),
        > 1 => Text.Format("Error.RepeatedSection", section),
        _ => null
    };

    public IReadOnlyList<BindingEntry> Entries(string section)
    {
        var result = new List<BindingEntry>();
        var current = "";
        foreach (var line in lines)
        {
            var header = Header.Match(line.Text);
            if (header.Success) { current = header.Groups[1].Value.Trim(); continue; }
            if (!Equal(current, section)) continue;
            var match = Assignment.Match(line.Text);
            if (!match.Success) continue;
            var key = match.Groups[1].Value.Split('=')[0].Trim();
            result.Add(new(line.Id, current, key, match.Groups[2].Value.Trim(), match.Groups[4].Value));
        }
        return result;
    }

    public void Replace(string section, Guid id, string value)
    {
        EnsureSection(section);
        var entry = Entries(section).Single(e => e.Id == id);
        if (!entry.Editable) throw new InvalidOperationException(Text.Get("Error.ReadOnlyBinding"));
        var input = ValidateValue(value);
        if (Equal(entry.Value, input) || Bindings.InputIdentity(section, entry.Value) == Bindings.InputIdentity(section, input)) return;
        EnsureNoDuplicate(section, entry.Key, input, id);
        var line = lines.Single(l => l.Id == id);
        var match = Assignment.Match(line.Text);
        var comment = match.Groups[4].Value;
        var oldAxis = MouseAxes.Parse(entry.Value);
        var newAxis = MouseAxes.Parse(input);
        if ((oldAxis is null || newAxis is null || oldAxis.ConditionIdentity != newAxis.ConditionIdentity) && Regex.IsMatch(comment, @"^;\s*!?DIK_\w+"))
        {
            var token = newAxis?.CommentToken ?? Bindings.DikToken(input) ?? "";
            comment = Regex.Replace(comment, @"(?<=;)\s*!?DIK_\w+", token.Length > 0 ? token : "", RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        line.Text = match.Groups[1].Value + input + match.Groups[3].Value + comment;
    }

    public void Add(string section, string key, string value)
    {
        EnsureSection(section);
        if (!Bindings.IsKnownAction(key)) throw new InvalidOperationException(Text.Get("Error.UnknownAction"));
        var input = ValidateValue(value);
        EnsureNoDuplicate(section, key, input, null);
        var entries = Entries(section).Where(e => Equal(e.Key, key)).ToArray();
        int index;
        if (entries.Length > 0) index = lines.FindIndex(l => l.Id == entries[^1].Id) + 1;
        else
        {
            var start = lines.FindIndex(l => Header.Match(l.Text) is { Success: true } m && Equal(m.Groups[1].Value.Trim(), section));
            var next = lines.FindIndex(start + 1, l => Header.IsMatch(l.Text));
            index = next >= 0 ? next : lines.Count;
            while (index > start + 1 && string.IsNullOrWhiteSpace(lines[index - 1].Text)) index--;
        }
        if (index > 0 && lines[index - 1].Ending.Length == 0) lines[index - 1].Ending = newLine;
        var ending = index == lines.Count && !finalNewLine ? "" : newLine;
        var token = Bindings.DikToken(input);
        lines.Insert(index, new($"{key}= {input}" + (token is null ? "" : $" ;{token}"), ending));
    }

    public void Remove(string section, Guid id)
    {
        EnsureSection(section);
        var entry = Entries(section).Single(e => e.Id == id);
        if (!entry.Editable) throw new InvalidOperationException(Text.Get("Error.ReadOnlyBinding"));
        var index = lines.FindIndex(l => l.Id == id);
        var wasLast = index == lines.Count - 1;
        lines.RemoveAt(index);
        if (wasLast && !finalNewLine && lines.Count > 0) lines[^1].Ending = "";
    }

    public IReadOnlyList<BindingConflict> Conflicts(string section) => Entries(section)
        .Where(e => Bindings.IsKnownAction(e.Key))
        .Select(e => (Entry: e, Identity: Bindings.InputIdentity(section, e.Value)))
        .Where(e => e.Identity is not null)
        .GroupBy(e => e.Identity!, StringComparer.OrdinalIgnoreCase)
        .Select(g => new BindingConflict(Bindings.InputName(section, g.Key),
            g.Select(e => e.Entry.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()))
        .Where(c => c.Actions.Count > 1).ToArray();

    public IReadOnlyList<string> ConflictingActions(string section, string key, string value)
    {
        var identity = Bindings.InputIdentity(section, value);
        if (identity is null) return [];
        return Entries(section).Where(e => Bindings.IsKnownAction(e.Key) && !Equal(e.Key, key) &&
            Bindings.InputIdentity(section, e.Value) is { } other && Equal(identity, other))
            .Select(e => e.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string SharedInputWarning(string section, string key, string value)
    {
        var actions = ConflictingActions(section, key, value);
        return actions.Count == 0 ? "" : Text.Format("Binding.SharedWarning", Bindings.InputName(section, value),
            string.Join(", ", actions.Select(action => Bindings.ActionName(section, action))));
    }

    public bool HasFlightMouseBindings => Entries("KeyboardFlight").Any(e => Bindings.UsesMouse(e.Value));
    public bool HasDisabledFlightMouseBindings => FlightLines().Any(l => IsDisabledMouseLine(l.Text));

    public void SetFlightMouseEnabled(bool enabled)
    {
        EnsureSection("KeyboardFlight");
        foreach (var line in FlightLines())
        {
            if (enabled)
            {
                if (DisabledMousePrefix(line.Text) is { } prefix) line.Text = line.Text[prefix.Length..];
            }
            else if (Assignment.Match(line.Text) is { Success: true } match && Bindings.UsesMouse(match.Groups[2].Value))
                line.Text = MouseDisabledPrefix + line.Text;
        }
    }

    private IEnumerable<Line> FlightLines()
    {
        var section = "";
        foreach (var line in lines)
        {
            var header = Header.Match(line.Text);
            if (header.Success) { section = header.Groups[1].Value.Trim(); continue; }
            if (Equal(section, "KeyboardFlight")) yield return line;
        }
    }

    private static bool IsDisabledMouseLine(string text) => DisabledMousePrefix(text) is not null;

    private static string? DisabledMousePrefix(string text)
    {
        foreach (var prefix in new[] { MouseDisabledPrefix, LegacyMouseDisabledPrefix })
            if (text.StartsWith(prefix, StringComparison.Ordinal) &&
                Assignment.Match(text[prefix.Length..]) is { Success: true } match && Bindings.UsesMouse(match.Groups[2].Value))
                return prefix;
        return null;
    }

    private void EnsureSection(string section)
    {
        if (!Bindings.IsKeyboardSection(section))
            throw new InvalidOperationException(Text.Get("Error.DeviceSections"));
        if (SectionProblem(section) is { } problem) throw new InvalidOperationException(problem);
    }

    private static string ValidateValue(string value) => Bindings.FindEditable(value)?.Value
        ?? throw new InvalidOperationException(Text.Get("Error.UnsupportedInput"));

    private void EnsureNoDuplicate(string section, string key, string value, Guid? except)
    {
        if (Entries(section).Any(e => e.Id != except && Equal(e.Key, key) &&
            Bindings.InputIdentity(section, e.Value) == Bindings.InputIdentity(section, value)))
            throw new InvalidOperationException(Text.Get("Error.DuplicateBinding"));
    }

    private static bool Equal(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);
}
