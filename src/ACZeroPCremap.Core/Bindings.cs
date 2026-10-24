using ACZeroPCremap.Core.Localization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ACZeroPCremap.Core;

public sealed record InputChoice(string Value, string ResourceKey)
{
    public string Name => Text.Get(ResourceKey);
    public override string ToString() => $"{Name}  ·  {Value}";
}

public static class Bindings
{
    // DirectInput DIK values. These are physical keys, not characters or virtual keys.
    public static IReadOnlyList<InputChoice> Choices { get; } = BuildChoices();
    public static IReadOnlyList<string> ActionKeys { get; } =
    ["HatL", "HatR", "HatU", "HatD", "LX-", "LX+", "LY-", "LY+", "RX-", "RX+", "RY-", "RY+",
     "LT", "RT", "LB", "RB", "A", "B", "X", "Y", "LSC", "RSC", "Menu", "View"];

    public static bool IsKnownAction(string key) => ActionKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
    public static bool IsKeyboardSection(string section) => section.Equals("KeyboardFlight", StringComparison.OrdinalIgnoreCase) ||
        section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase);
    public static InputChoice? Find(string value) => Choices.FirstOrDefault(c => c.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
    public static InputChoice? FindEditable(string value) => Find(value) ?? MouseAxes.Parse(value)?.Choice;

    public static bool UsesMouse(string value) => Regex.IsMatch(value, @"\bMouse[A-Za-z0-9_]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Match actual bindings, not modifier keys embedded in larger expressions.
    public static string? InputIdentity(string section, string value)
    {
        if (!IsKeyboardSection(section)) return null;
        var compact = Regex.Replace(value, @"\s+", "");
        if (Find(compact) is { } simple) return simple.Value;
        if (MouseAxes.Parse(value) is { } axis) return axis.Identity;
        var strength = Regex.Match(compact, @"^(0x[0-9a-f]{2}|Mouse[1-3]):([0-9]{1,5})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (strength.Success && int.TryParse(strength.Groups[2].Value, out var amount) && amount is > 0 and <= 65535)
            return Find(strength.Groups[1].Value)?.Value;
        return UsesMouse(compact) ? compact.ToUpperInvariant() : null;
    }

    public static string InputName(string section, string value) => FindEditable(value)?.Name ?? value;

    // Names describe the output actions in the supplied INI, not the physical
    // layout of a PS2/Xbox controller. Flight uses the ZERO scheme, Normal mode.
    public static string ActionName(string section, string key) =>
        section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase) ? MenuActionName(key) : FlightActionName(key);

    private static string FlightActionName(string key) => key.ToUpperInvariant() switch
    {
        "HATL" => Text.Get("Flight.HATL"), "HATR" => Text.Get("Flight.HATR"),
        "HATU" => Text.Get("Flight.HATU"), "HATD" => Text.Get("Flight.HATD"),
        "LX-" => Text.Get("Flight.LX-"), "LX+" => Text.Get("Flight.LX+"),
        "LY-" => Text.Get("Flight.LY-"), "LY+" => Text.Get("Flight.LY+"),
        "RX-" => Text.Get("Camera.Left"), "RX+" => Text.Get("Camera.Right"),
        "RY-" => Text.Get("Camera.Up"), "RY+" => Text.Get("Camera.Down"),
        "LT" => Text.Get("Flight.LT"), "RT" => Text.Get("Flight.RT"),
        "LB" => Text.Get("Flight.LB"), "RB" => Text.Get("Flight.RB"),
        "A" => Text.Get("Flight.A"), "B" => Text.Get("Flight.B"),
        "X" => Text.Get("Flight.X"), "Y" => Text.Get("Flight.Y"),
        "LSC" => Text.Get("Flight.LSC"), "RSC" => Text.Get("Flight.RSC"),
        "MENU" => Text.Get("Flight.MENU"), "VIEW" => Text.Get("Flight.VIEW"), _ => Text.Format("UnknownAction", key)
    };

    private static string MenuActionName(string key) => key.ToUpperInvariant() switch
    {
        "HATL" or "LX-" => Text.Get("Menu.Left"), "HATR" or "LX+" => Text.Get("Menu.Right"),
        "HATU" or "LY-" => Text.Get("Menu.Up"), "HATD" or "LY+" => Text.Get("Menu.Down"),
        "RX-" => Text.Get("Camera.Left"), "RX+" => Text.Get("Camera.Right"),
        "RY-" => Text.Get("Camera.Up"), "RY+" => Text.Get("Camera.Down"),
        "A" => Text.Get("Menu.A"), "B" => Text.Get("Menu.B"), "MENU" => Text.Get("Menu.MENU"),
        "X" => Text.Get("Menu.X"), "Y" => Text.Get("Menu.Y"),
        "LB" => Text.Get("Menu.LB"), "RB" => Text.Get("Menu.RB"),
        "VIEW" => Text.Get("Menu.VIEW"),
        "LT" or "RT" or "LSC" or "RSC" => Text.Get("Menu.Unused"),
        _ => Text.Format("UnknownAction", key)
    };

    public static string SectionNote(string section) => section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase)
        ? Text.Get("Menu.Note")
        : Text.Get("Flight.Note");

    public static string ActionDescription(string section, string key)
    {
        if (section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase)) return key.ToUpperInvariant() switch
        {
            "LT" or "RT" or "LSC" or "RSC" => Text.Get("Menu.UnusedHint"),
            "LB" or "RB" or "Y" or "VIEW" => Text.Get("Menu.ContextHint"),
            "X" => Text.Get("Menu.DisplayHint"),
            "RX-" or "RX+" or "RY-" or "RY+" => Text.Get("Menu.CameraHint"),
            _ => ActionName(section, key)
        };
        return key.ToUpperInvariant() switch
        {
            "HATL" => Text.Get("Flight.HATLHint"),
            "HATR" => Text.Get("Flight.HATRHint"),
            "HATU" => Text.Get("Flight.HATUHint"),
            "HATD" => Text.Get("Flight.HATDHint"),
            "LX-" or "LX+" => Text.Get("Flight.RollHint"),
            "LY-" => Text.Get("Flight.PitchDownHint"),
            "LY+" => Text.Get("Flight.PitchUpHint"),
            "LT" => Text.Get("Flight.BrakeHint"),
            "RT" => Text.Get("Flight.ThrottleHint"),
            "LB" or "RB" => Text.Get("Flight.YawHint"),
            "A" => Text.Get("Flight.GunHint"),
            "B" => Text.Get("Flight.MissileHint"),
            "Y" => Text.Get("Flight.TargetHint"),
            "X" => Text.Get("Flight.WeaponHint"),
            "VIEW" => Text.Get("Flight.RadarHint"),
            "LSC" => Text.Get("Flight.BoresightHint"),
            "RSC" => Text.Get("Flight.ViewHint"),
            "RX-" or "RX+" or "RY-" or "RY+" => Text.Get("Flight.CameraHint"),
            _ => ActionName(section, key)
        };
    }

    public static string? DikToken(string value)
    {
        var choice = Find(value);
        if (choice is null || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return null;
        return Tokens.GetValueOrDefault(int.Parse(value.AsSpan(2), NumberStyles.HexNumber));
    }

    private static readonly Dictionary<int, string> Tokens = MakeTokens();

    private static Dictionary<int, string> MakeTokens()
    {
        var result = new Dictionary<int, string>();
        // File tokens must never depend on localized display names.
        for (var i = 0; i < 10; i++) result[0x02 + i] = "DIK_" + "1234567890"[i];
        foreach (var (start, letters) in new[] { (0x10, "QWERTYUIOP"), (0x1E, "ASDFGHJKL"), (0x2C, "ZXCVBNM") })
            for (var i = 0; i < letters.Length; i++) result[start + i] = "DIK_" + letters[i];
        for (var i = 0; i < 10; i++) result[0x3B + i] = $"DIK_F{i + 1}";
        result[0x57] = "DIK_F11"; result[0x58] = "DIK_F12";
        foreach (var (code, digit) in new[] { (0x52, 0), (0x4F, 1), (0x50, 2), (0x51, 3), (0x4B, 4), (0x4C, 5), (0x4D, 6), (0x47, 7), (0x48, 8), (0x49, 9) })
            result[code] = $"DIK_NUMPAD{digit}";
        foreach (var (code, token) in new (int, string)[]
        {
            (0x01,"ESCAPE"),(0x0E,"BACK"),(0x0F,"TAB"),(0x1C,"RETURN"),(0x1D,"LCONTROL"),
            (0x2A,"LSHIFT"),(0x36,"RSHIFT"),(0x38,"LMENU"),(0x3A,"CAPITAL"),
            (0x45,"NUMLOCK"),(0x46,"SCROLL"),(0x9C,"NUMPADENTER"),(0x9D,"RCONTROL"),
            (0xB5,"DIVIDE"),(0xB7,"SYSRQ"),(0xB8,"RMENU"),(0xC5,"PAUSE"),
            (0xC7,"HOME"),(0xC8,"UP"),(0xC9,"PRIOR"),(0xCB,"LEFT"),(0xCD,"RIGHT"),
            (0xCF,"END"),(0xD0,"DOWN"),(0xD1,"NEXT"),(0xD2,"INSERT"),(0xD3,"DELETE"),
            (0xDB,"LWIN"),(0xDC,"RWIN"),(0xDD,"APPS"),(0x39,"SPACE"),
            (0x0C,"MINUS"),(0x0D,"EQUALS"),(0x1A,"LBRACKET"),(0x1B,"RBRACKET"),
            (0x27,"SEMICOLON"),(0x28,"APOSTROPHE"),(0x29,"GRAVE"),(0x2B,"BACKSLASH"),
            (0x33,"COMMA"),(0x34,"PERIOD"),(0x35,"SLASH"),(0x37,"MULTIPLY"),
            (0x4A,"SUBTRACT"),(0x4E,"ADD"),(0x53,"DECIMAL"),(0x56,"OEM_102")
        }) result[code] = "DIK_" + token;
        return result;
    }

    private static IReadOnlyList<InputChoice> BuildChoices()
    {
        int[] codes =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1E, 0x1F, 0x20,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31,
            0x32, 0x0C, 0x0D, 0x0E, 0x0F, 0x1A, 0x1B, 0x1C, 0x1D, 0x27, 0x28, 0x29,
            0x2A, 0x2B, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x45, 0x46,
            0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52,
            0x53, 0x56, 0x9C, 0x9D, 0xB5, 0xB7, 0xB8, 0xC5, 0xC7, 0xC8, 0xC9, 0xCB,
            0xCD, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xDB, 0xDC, 0xDD, 0x3B, 0x3C, 0x3D,
            0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x57, 0x58
        ];
        var keys = codes.Select(code => new InputChoice($"0x{code:X2}", $"Key.{code:X2}")).ToList();
        keys.AddRange([new("Mouse1", "Mouse.Left"), new("Mouse2", "Mouse.Right"), new("Mouse3", "Mouse.Middle")]);
        return keys;
    }
}

public static class KeyScanCode
{
    public static string? ToDik(int virtualKey, int scanCode, bool extended)
    {
        var code = virtualKey switch
        {
            0x13 => 0xC5, // Pause uses an E1 sequence.
            0x90 => 0x45, // NumLock's E0 prefix does not imply DIK_PAUSE.
            0x2C => 0xB7,
            _ => scanCode is > 0 and < 128 ? scanCode | (extended ? 0x80 : 0) : -1
        };
        var value = $"0x{code:X2}";
        return Bindings.Find(value)?.Value;
    }
}
