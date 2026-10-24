using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ACZeroPCremap.Core;
using ACZeroPCremap.Core.Localization;

namespace ACZeroPCremap.App;

/// <summary>Native pixel coordinates avoid guessing WPF's virtual-screen origin on mixed-DPI monitors.</summary>
internal sealed class WindowPlacementController
{
    private readonly Window window;
    private readonly LanguagePreferences? preferences;
    private readonly double defaultWidth, defaultHeight, minWidth, minHeight;
    private HwndSource? source;
    private WindowPlacement? normalBounds;
    private bool maximized, adjusting, queued;
    private IntPtr monitor;

    public WindowPlacementController(Window window, LanguagePreferences? preferences = null, double minWidth = 760, double minHeight = 440)
    {
        this.window = window;
        this.preferences = preferences;
        this.minWidth = minWidth;
        this.minHeight = minHeight;
        defaultWidth = double.IsFinite(window.Width) ? window.Width : 600;
        defaultHeight = double.IsFinite(window.Height) ? window.Height : 340;
        window.SourceInitialized += (_, _) =>
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source.AddHook(WindowMessage);
            Place(preferences?.LoadWindowPlacement(), centerOnOwner: preferences is null);
        };
        window.ContentRendered += OnFirstRender;
        window.LocationChanged += (_, _) => RememberBounds();
        window.SizeChanged += (_, _) => RememberBounds();
        window.StateChanged += (_, _) =>
        {
            if (window.WindowState != WindowState.Minimized) maximized = window.WindowState == WindowState.Maximized;
            RememberBounds();
        };
        window.DpiChanged += (_, _) => QueueFit();
        window.Closed += (_, _) => source?.RemoveHook(WindowMessage);
    }

    private void OnFirstRender(object? sender, EventArgs e)
    {
        window.ContentRendered -= OnFirstRender;
        // SizeToContent dialogs have their actual height only after layout.
        if (window.SizeToContent != SizeToContent.Manual)
        {
            var bounds = ReadBounds();
            window.SizeToContent = SizeToContent.Manual;
            Place(bounds, centerOnOwner: true);
        }
        RememberBounds();
    }

    public void Save()
    {
        RememberBounds();
        if (preferences is null || normalBounds is null) return;
        try { preferences.SaveWindowPlacement(normalBounds with { Maximized = maximized }); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        { /* A read-only portable folder must not prevent closing the editor. */ }
    }

    private void RememberBounds()
    {
        if (adjusting || source is null || window.WindowState != WindowState.Normal) return;
        normalBounds = ReadBounds();
    }

    private WindowPlacement ReadBounds()
    {
        GetWindowRect(source!.Handle, out var rect);
        return new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, GetDpiForWindow(source.Handle) / 96d);
    }

    private ScreenWorkArea WorkArea(IntPtr targetMonitor)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(targetMonitor, ref info)) throw new InvalidOperationException("Cannot read the monitor work area.");
        GetDpiForMonitor(targetMonitor, 0, out var dpi, out _);
        var scale = dpi > 0 ? dpi / 96d : GetDpiForWindow(source!.Handle) / 96d;
        return new(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top, scale);
    }

    private void Place(WindowPlacement? saved, bool centerOnOwner = false, bool useCurrentMonitor = false)
    {
        if (source is null) return;
        adjusting = true;
        try
        {
            var target = IntPtr.Zero;
            if (!useCurrentMonitor && saved?.IsValid == true)
            {
                var rect = new NativeRect { Left = Pixel(saved.Left), Top = Pixel(saved.Top), Right = Pixel(saved.Left + saved.Width), Bottom = Pixel(saved.Top + saved.Height) };
                target = MonitorFromRect(ref rect, 0);
            }
            var owner = window.Owner is null ? IntPtr.Zero : new WindowInteropHelper(window.Owner).Handle;
            if (target == IntPtr.Zero) target = MonitorFromWindow(centerOnOwner && owner != IntPtr.Zero ? owner : source.Handle, 1);
            monitor = target;
            var screen = WorkArea(target);
            var fitted = WindowLayout.Fit(saved, screen, defaultWidth, defaultHeight, minWidth, minHeight);
            if (centerOnOwner && owner != IntPtr.Zero && GetWindowRect(owner, out var ownerRect))
                fitted = WindowLayout.Fit(fitted with { Left = (ownerRect.Left + (double)ownerRect.Right - fitted.Width) / 2,
                    Top = (ownerRect.Top + (double)ownerRect.Bottom - fitted.Height) / 2 }, screen, defaultWidth, defaultHeight, minWidth, minHeight);
            window.MinWidth = Math.Min(minWidth, Math.Max(1, screen.Width / screen.DpiScale - 24));
            window.MinHeight = Math.Min(minHeight, Math.Max(1, screen.Height / screen.DpiScale - 24));
            window.MaxWidth = screen.Width / screen.DpiScale;
            window.MaxHeight = screen.Height / screen.DpiScale;
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                if (window.SizeToContent == SizeToContent.Manual)
                {
                    window.Width = fitted.Width / screen.DpiScale;
                    window.Height = fitted.Height / screen.DpiScale;
                }
                SetWindowPos(source.Handle, IntPtr.Zero, Pixel(fitted.Left), Pixel(fitted.Top), Pixel(fitted.Width), Pixel(fitted.Height), 0x0014);
                normalBounds = fitted;
            }
            else if (window.WindowState == WindowState.Maximized) normalBounds = fitted;
            if (saved?.Maximized == true) window.WindowState = WindowState.Maximized;
        }
        finally { adjusting = false; }
    }

    private void QueueFit()
    {
        if (queued || source is null) return;
        queued = true;
        window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            queued = false;
            if (source?.IsDisposed != false || window.WindowState == WindowState.Minimized) return;
            Place(window.WindowState == WindowState.Normal ? ReadBounds() : normalBounds, useCurrentMonitor: true);
        }));
    }

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is 0x007E or 0x001A or 0x0232) QueueFit(); // display/work-area change, finished moving/resizing
        else if (message == 0x0047 && MonitorFromWindow(hwnd, 1) != monitor) QueueFit();
        else if (message == 0x0024) // WM_GETMINMAXINFO: borderless maximization must respect the taskbar.
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(MonitorFromWindow(hwnd, 1), ref info))
            {
                var sizes = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                sizes.MaxPosition = new() { X = info.Work.Left - info.Monitor.Left, Y = info.Work.Top - info.Monitor.Top };
                sizes.MaxSize = new() { X = info.Work.Right - info.Work.Left, Y = info.Work.Bottom - info.Work.Top };
                sizes.MaxTrackSize = sizes.MaxSize;
                var scale = GetDpiForWindow(hwnd) / 96d;
                sizes.MinTrackSize = new() { X = Math.Min(sizes.MaxSize.X, Pixel(window.MinWidth * scale)), Y = Math.Min(sizes.MaxSize.Y, Pixel(window.MinHeight * scale)) };
                Marshal.StructureToPtr(sizes, lParam, false);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private static int Pixel(double value) => (int)Math.Clamp(Math.Round(value), int.MinValue / 2d, int.MaxValue / 2d);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref NativeRect rect, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int kind, out uint x, out uint y);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
