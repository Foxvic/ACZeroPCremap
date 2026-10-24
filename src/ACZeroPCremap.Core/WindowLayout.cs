namespace ACZeroPCremap.Core;

// Coordinates and dimensions are physical screen pixels. Keeping the DPI with
// them lets a saved window retain its readable logical size on another screen.
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, double DpiScale = 1, bool Maximized = false)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top) &&
        double.IsFinite(Width) && double.IsFinite(Height) && Width > 0 && Height > 0 &&
        double.IsFinite(DpiScale) && DpiScale is >= 0.5 and <= 8;
}

public sealed record ScreenWorkArea(double Left, double Top, double Width, double Height, double DpiScale = 1);

public static class WindowLayout
{
    public static WindowPlacement Fit(WindowPlacement? saved, ScreenWorkArea screen,
        double defaultWidth = 1100, double defaultHeight = 820, double minWidth = 760, double minHeight = 440)
    {
        if (!double.IsFinite(screen.Left) || !double.IsFinite(screen.Top) ||
            !double.IsFinite(screen.Width) || !double.IsFinite(screen.Height) || screen.Width <= 0 || screen.Height <= 0 ||
            !double.IsFinite(screen.DpiScale) || screen.DpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(screen));
        if (saved?.IsValid != true) saved = null;
        var scale = screen.DpiScale;
        var margin = Math.Min(12 * scale, Math.Min(screen.Width, screen.Height) / 10);
        var availableWidth = screen.Width - 2 * margin;
        var availableHeight = screen.Height - 2 * margin;
        var width = Math.Clamp((saved is null ? defaultWidth : saved.Width / saved.DpiScale) * scale,
            Math.Min(minWidth * scale, availableWidth), availableWidth);
        var height = Math.Clamp((saved is null ? defaultHeight : saved.Height / saved.DpiScale) * scale,
            Math.Min(minHeight * scale, availableHeight), availableHeight);
        var visible = saved is not null && saved.Left < screen.Left + screen.Width && saved.Top < screen.Top + screen.Height &&
            saved.Left + saved.Width > screen.Left && saved.Top + saved.Height > screen.Top;
        var left = visible ? saved!.Left : screen.Left + (screen.Width - width) / 2;
        var top = visible ? saved!.Top : screen.Top + (screen.Height - height) / 2;
        return new(Math.Clamp(left, screen.Left + margin, screen.Left + screen.Width - margin - width),
            Math.Clamp(top, screen.Top + margin, screen.Top + screen.Height - margin - height), width, height, scale, saved?.Maximized ?? false);
    }
}
