using System.Globalization;
using System.Text.RegularExpressions;

namespace ACZeroPCremap.Core;

public enum MouseDirection { Left, Right, Up, Down, WheelUp, WheelDown }

public sealed record MouseAxisBinding(string Value, string Prefix, string Axis, int From, int To)
{
    public bool IsWheel => Axis == "MouseZ";
    public bool IsAbsolute => Axis is "MouseAX" or "MouseAY";
    public int Magnitude => Math.Max(From, To);
    public MouseDirection Direction => IsWheel ? (From == 0 ? MouseDirection.WheelUp : MouseDirection.WheelDown)
        : Axis is "MouseX" or "MouseAX" ? (From == 0 ? MouseDirection.Right : MouseDirection.Left)
        : From == 0 ? MouseDirection.Down : MouseDirection.Up;
    public string ConditionIdentity => Regex.Replace(Prefix, @"\s+", "").ToUpperInvariant();
    public string? CommentToken => Prefix.Length == 0 ? null : (Prefix.StartsWith('!') ? "!" : "") +
        Bindings.DikToken(Regex.Match(Prefix, @"0x[0-9a-fA-F]{2}", RegexOptions.IgnoreCase).Value);
    public string Identity => ConditionIdentity + Axis.ToUpperInvariant() +
        string.Create(CultureInfo.InvariantCulture, $":{From}:{To}");
    public InputChoice Choice => new(Value, "MouseAxis." + (IsAbsolute ? "Absolute" : "") + Direction);
}

public static class MouseAxes
{
    // Only the documented shape present in the supplied INI is editable:
    // optional single DIK condition, one zero endpoint, and a positive magnitude.
    // Unknown expressions remain untouched/read-only.
    private static readonly Regex Expression = new(@"\A(?<prefix>(?<neg>!)?(?<key>0x[0-9a-f]{2})\s*\+\s*)?(?<axis>Mouse(?:AX|AY|X|Y|Z))\s*:\s*(?<from>[0-9]{1,5})\s*:\s*(?<to>[0-9]{1,5})\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static MouseAxisBinding? Parse(string value)
    {
        if (value.IndexOfAny(['\r', '\n', '\0']) >= 0) return null;
        var match = Expression.Match(value);
        if (!match.Success || !int.TryParse(match.Groups["from"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var from) ||
            !int.TryParse(match.Groups["to"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var to) ||
            from > 65535 || to > 65535 || (from == 0) == (to == 0)) return null;
        if (match.Groups["key"].Success && Bindings.Find(match.Groups["key"].Value)?.Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) != true) return null;
        var axis = match.Groups["axis"].Value.ToUpperInvariant() switch
        {
            "MOUSEX" => "MouseX", "MOUSEY" => "MouseY", "MOUSEAX" => "MouseAX", "MOUSEAY" => "MouseAY", _ => "MouseZ"
        };
        return new(value, match.Groups["prefix"].Value, axis, from, to);
    }

    public static string Create(MouseDirection direction, string? template = null)
    {
        var previous = template is null ? null : Parse(template);
        var wheel = direction is MouseDirection.WheelUp or MouseDirection.WheelDown;
        var horizontal = direction is MouseDirection.Left or MouseDirection.Right;
        var absolute = !wheel && previous?.IsAbsolute == true;
        var axis = wheel ? "MouseZ" : absolute ? (horizontal ? "MouseAX" : "MouseAY") : horizontal ? "MouseX" : "MouseY";
        var magnitude = previous is not null && previous.IsWheel == wheel ? previous.Magnitude : wheel ? 1 : absolute ? 256 : horizontal ? 8 : 2;
        var negative = direction is MouseDirection.Left or MouseDirection.Up or MouseDirection.WheelDown;
        var amount = magnitude.ToString(CultureInfo.InvariantCulture);
        return (previous?.Prefix ?? "") + axis + (negative ? $":{amount}:0" : $":0:{amount}");
    }

    public static IReadOnlyList<InputChoice> Choices => Enum.GetValues<MouseDirection>().Select(d => Parse(Create(d))!.Choice).ToArray();
}

public sealed class MouseMovementCapture(long startedAt)
{
    private (double X, double Y)? origin;
    public long StartedAt { get; } = startedAt;
    public void ResetOrigin() => origin = null;

    public MouseDirection? Observe(double x, double y, long now)
    {
        if (now - StartedAt < 250 || !double.IsFinite(x) || !double.IsFinite(y)) return null;
        if (origin is null) { origin = (x, y); return null; }
        var dx = x - origin.Value.X;
        var dy = y - origin.Value.Y;
        var ax = Math.Abs(dx);
        var ay = Math.Abs(dy);
        if (Math.Max(ax, ay) < 24) return null;
        if (ax > ay * 1.35) return dx < 0 ? MouseDirection.Left : MouseDirection.Right;
        if (ay > ax * 1.35) return dy < 0 ? MouseDirection.Up : MouseDirection.Down;
        return null;
    }
}
