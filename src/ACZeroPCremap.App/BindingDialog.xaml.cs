using ACZeroPCremap.Core.Localization;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using ACZeroPCremap.Core;

namespace ACZeroPCremap.App;

public partial class BindingDialog : Window
{
    private HwndSource? source;
    private bool capturing;
    private bool capturingAxes;
    private MouseMovementCapture? movementCapture;
    private readonly ObservableCollection<InputChoice> inputChoices = new(Bindings.Choices.Concat(MouseAxes.Choices));
    private readonly string? initialAxisTemplate;
    private string? activeAxisTemplate;
    private readonly IniDocument? document;
    private readonly string section;
    private readonly string actionKey;
    private readonly HashSet<int> initiallyHeld = [];
    private readonly HashSet<MouseButton> consumedMouseReleases = [];
    public InputChoice? Result { get; private set; }

    public BindingDialog(string section, string key, string? currentValue, IniDocument? document = null)
    {
        this.document = document;
        this.section = section;
        actionKey = key;
        initialAxisTemplate = MouseAxes.Parse(currentValue ?? "")?.Value ?? document?.Entries(section)
            .Where(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Select(e => MouseAxes.Parse(e.Value)).FirstOrDefault(a => a is not null)?.Value;
        InitializeComponent();
        _ = new WindowPlacementController(this, minWidth: 580, minHeight: 400);
        SizeChanged += (_, _) => DialogRoot.Margin = new(ActualHeight < 600 ? 14 : 26);
        ActionLabel.Text = Bindings.ActionName(section, key);
        ActionHint.Text = Bindings.ActionDescription(section, key);
        Choices.ItemsSource = inputChoices;
        if (currentValue is not null) SelectInput(currentValue);
        // Moving from the capture button into the box is not the intended gesture.
        AxisCapturePanel.MouseEnter += (_, _) => { if (capturingAxes) movementCapture = new(Environment.TickCount64); };
        SourceInitialized += (_, _) =>
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(InputHook);
            // WPF can consume queued keys (e.g. Space on a focused Button) before
            // DispatchMessage, so an HwndSource hook alone never sees those keys.
            ComponentDispatcher.ThreadFilterMessage += FilterInput;
        };
        Closed += (_, _) =>
        {
            ComponentDispatcher.ThreadFilterMessage -= FilterInput;
            source?.RemoveHook(InputHook);
        };
        Deactivated += (_, _) => StopCapture(Text.Get("CaptureLostFocus"));
    }

    private void StartCapture(object sender, RoutedEventArgs e)
    {
        if (capturing && !capturingAxes) StopCapture(Text.Get("CaptureStopped"));
        else BeginCapture(axes: false);
    }
    private void StartAxesCapture(object sender, RoutedEventArgs e)
    {
        if (capturingAxes) StopCapture(Text.Get("CaptureStopped"));
        else BeginCapture(axes: true);
    }

    private void BeginCapture(bool axes)
    {
        if (capturing) return;
        initiallyHeld.Clear();
        for (var vk = 1; vk < 256; vk++)
            if ((GetAsyncKeyState(vk) & 0x8000) != 0) initiallyHeld.Add(vk);
        capturing = true;
        capturingAxes = axes;
        CaptureButton.Content = Text.Get(axes ? "Capture" : "MouseAxis.Stop");
        AxisCaptureButton.Content = Text.Get(axes ? "MouseAxis.Stop" : "MouseAxis.Capture");
        activeAxisTemplate = GetAxisTemplate(movement: axes);
        movementCapture = axes ? new(Environment.TickCount64) : null;
        AxisCapturePanel.Visibility = axes ? Visibility.Visible : Visibility.Collapsed;
        CaptureHelpText.Visibility = axes ? Visibility.Collapsed : Visibility.Visible;
        AxisCaptureNote.Text = Text.Get(activeAxisTemplate is null ? "MouseAxis.NewNote" : "MouseAxis.RetainNote");
        Choices.IsEnabled = false;
        ApplyButton.IsEnabled = false;
        CaptureStatus.Text = Text.Get(axes ? "MouseAxis.Waiting" : "CaptureWaiting");
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (capturing && !IsCaptureControlClick(e))
        {
            e.Handled = true;
            if (capturingAxes) consumedMouseReleases.Add(e.ChangedButton);
            else if (!initiallyHeld.Contains(MouseVirtualKey(e.ChangedButton)))
            {
                consumedMouseReleases.Add(e.ChangedButton);
                var value = e.ChangedButton switch
                {
                    MouseButton.Left => "Mouse1",
                    MouseButton.Right => "Mouse2",
                    MouseButton.Middle => "Mouse3",
                    _ => null
                };
                if (value is null) CaptureStatus.Text = Text.Get("CaptureUnsupported");
                else
                {
                    SelectInput(value);
                    StopCapture(Text.Format("CaptureSelected", Choices.SelectedItem));
                }
            }
        }
        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        initiallyHeld.Remove(MouseVirtualKey(e.ChangedButton));
        // Consume the release too, so the captured click cannot also activate
        // a newly enabled Apply button or another control beneath the pointer.
        if (consumedMouseReleases.Remove(e.ChangedButton) || capturing && !IsCaptureControlClick(e)) e.Handled = true;
        base.OnPreviewMouseUp(e);
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (capturing)
        {
            e.Handled = true;
            if (!capturingAxes && e.Delta != 0) CaptureAxis(e.Delta > 0 ? MouseDirection.WheelUp : MouseDirection.WheelDown);
        }
        base.OnPreviewMouseWheel(e);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (capturingAxes)
        {
            var point = e.GetPosition(AxisCapturePanel);
            if (point.X >= 0 && point.Y >= 0 && point.X <= AxisCapturePanel.ActualWidth && point.Y <= AxisCapturePanel.ActualHeight)
                ObserveAxisMovement(point.X, point.Y, Environment.TickCount64);
            else movementCapture?.ResetOrigin();
        }
        base.OnPreviewMouseMove(e);
    }

    private void ObserveAxisMovement(double x, double y, long now)
    {
        if (capturingAxes && movementCapture?.Observe(x, y, now) is { } direction) CaptureAxis(direction);
    }

    private string? GetAxisTemplate(bool movement = false)
    {
        var selected = MouseAxes.Parse((Choices.SelectedItem as InputChoice)?.Value ?? "");
        return selected is not null && (!movement || !selected.IsWheel) ? selected.Value : initialAxisTemplate;
    }

    private void CaptureAxis(MouseDirection direction)
    {
        SelectInput(MouseAxes.Create(direction, capturingAxes ? activeAxisTemplate : GetAxisTemplate()));
        StopCapture(Text.Format("CaptureSelected", Choices.SelectedItem));
    }

    private void SelectInput(string value)
    {
        var choice = inputChoices.FirstOrDefault(c => c.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (choice is null && Bindings.FindEditable(value) is { } supported)
        {
            choice = supported;
            inputChoices.Add(choice);
        }
        Choices.SelectedItem = choice;
    }

    private bool IsCaptureControlClick(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return false;
        for (var target = e.OriginalSource as DependencyObject; target is not null;
            target = target is Visual ? VisualTreeHelper.GetParent(target) : LogicalTreeHelper.GetParent(target))
            if (ReferenceEquals(target, CancelButton) || ReferenceEquals(target, capturingAxes ? AxisCaptureButton : CaptureButton)) return true;
        return false;
    }

    private static int MouseVirtualKey(MouseButton button) => button switch
    {
        MouseButton.Left => 0x01, MouseButton.Right => 0x02, MouseButton.Middle => 0x04,
        MouseButton.XButton1 => 0x05, MouseButton.XButton2 => 0x06, _ => 0
    };

    private void FilterInput(ref MSG message, ref bool handled)
    {
        // This event belongs to the UI thread; do not intercept other windows or
        // controls outside the capture dialog, or messages already handled elsewhere.
        if (handled || source is null || message.hwnd != source.Handle) return;
        InputHook(message.hwnd, message.message, message.wParam, message.lParam, ref handled);
    }

    private IntPtr InputHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!capturing || capturingAxes || message is not (0x0100 or 0x0104 or 0x0101 or 0x0105)) return IntPtr.Zero;
        var vk = wParam.ToInt32();
        if (message is 0x0101 or 0x0105) // WM_KEYUP / WM_SYSKEYUP
        {
            initiallyHeld.Remove(vk);
            handled = true;
            return IntPtr.Zero;
        }
        if (message is not (0x0100 or 0x0104)) return IntPtr.Zero;
        handled = true;
        var bits = lParam.ToInt64();
        if (initiallyHeld.Contains(vk) || (bits & (1L << 30)) != 0) return IntPtr.Zero;
        var value = KeyScanCode.ToDik(vk, (int)((bits >> 16) & 0xFF), (bits & (1L << 24)) != 0);
        if (value is null)
        {
            CaptureStatus.Text = Text.Get("CaptureUnsupported");
            return IntPtr.Zero;
        }
        Choices.SelectedItem = Bindings.Find(value);
        StopCapture(Text.Format("CaptureSelected", Choices.SelectedItem));
        return IntPtr.Zero;
    }

    private void StopCapture(string message)
    {
        if (!capturing) return;
        capturing = false;
        capturingAxes = false;
        CaptureButton.Content = Text.Get("Capture");
        AxisCaptureButton.Content = Text.Get("MouseAxis.Capture");
        movementCapture = null;
        AxisCapturePanel.Visibility = Visibility.Collapsed;
        CaptureHelpText.Visibility = Visibility.Visible;
        Choices.IsEnabled = true;
        ApplyButton.IsEnabled = Choices.SelectedItem is InputChoice;
        CaptureStatus.Text = message;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ApplyButton is not null) ApplyButton.IsEnabled = !capturing && Choices.SelectedItem is InputChoice;
        if (SharedWarning is not null)
        {
            SharedWarning.Text = Choices.SelectedItem is InputChoice choice ? document?.SharedInputWarning(section, actionKey, choice.Value) ?? "" : "";
            SharedWarning.Visibility = SharedWarning.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void Apply(object sender, RoutedEventArgs e)
    {
        Result = Choices.SelectedItem as InputChoice;
        DialogResult = Result is not null;
    }

    private void Cancel(object sender, RoutedEventArgs e) => DialogResult = false;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
