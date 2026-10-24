using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ACZeroPCremap.App;
using ACZeroPCremap.Core;
using UiText = ACZeroPCremap.Core.Localization.Text;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Offscreen previews must not depend on a GPU/desktop render channel.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        var root = Path.GetFullPath(args[0]);
        var artifacts = Path.Combine(root, "artifacts", "ui-checks");
        Directory.CreateDirectory(artifacts);
        // English must not silently follow the operating system's UI language.
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        var app = new ACZeroPCremap.App.App(openStartupWindow: false);
        app.InitializeComponent();
        // Tests own their window lifetime while pumping the dispatcher for rendering.
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Check(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en", "Built-in WPF commands use the English app culture");
        var errors = new List<string>();
        var listener = new BindingErrorListener(errors);
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        try
        {
            var preferences = new ACZeroPCremap.Core.Localization.LanguagePreferences(Path.Combine(artifacts, "language-settings.json"));
            var main = new MainWindow(openStartupFile: false, languagePreferences: preferences, manageWindowPlacement: false);
            Check(main.Background is SolidColorBrush brush && brush.Color == Color.FromRgb(0x19, 0x08, 0x07), "Window uses the reference menu's crimson theme");
            var model = (MainViewModel)main.DataContext;
            Check(main.Icon is not null, "Winged-key icon is embedded and used by application windows");
            Render(main, Path.Combine(artifacts, "main-no-file.png"), 1044, 740);
            // Read-only opening of the original fixture; all persistence tests use temporary copies.
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            Check(model.Sections.Count == 2, "Only keyboard/mouse Flight and Menus tabs are available");
            Check(model.Sections[0].Actions.Single(a => a.Key == "A").Rows.Count == 2, "Alternative mouse binding displayed");
            Check(model.Sections[1].Actions.Single(a => a.Key == "A").Rows.Count == 3, "Three menu alternatives displayed");
            Check(!model.SaveCommand.CanExecute(null), "Save is disabled for unchanged document");
            var flight = model.Sections[0];
            var menus = model.Sections[1];
            Check(flight.Groups.Select(g => g.Name).SequenceEqual(new[] { "Flight", "Weapons", "Camera", "Wingman" }), "Flight actions have four ordered gameplay groups");
            Check(flight.Groups.SelectMany(g => g.Actions).Select(a => a.Key).Order().SequenceEqual(flight.Actions.Select(a => a.Key).Order()), "Grouping preserves every action exactly once");
            // Popup controls need a loaded presentation source, not just a measured tree.
            main.WindowStartupLocation = WindowStartupLocation.Manual;
            main.Left = -10000;
            main.Top = -10000;
            main.ShowInTaskbar = false;
            Console.WriteLine("Opening the offscreen window for control-template checks.");
            main.Show();
            Console.WriteLine("Offscreen window opened.");
            main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Render(main, Path.Combine(artifacts, "main-flight.png"), 1044, 740);
            var interactionSession = (ConfigSession)typeof(MainViewModel).GetField("session", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(model)!;
            var originalBindings = interactionSession.Document.ToBytes();
            var mouseToggle = Descendants<CheckBox>((DependencyObject)main.Content).Single();
            Check(mouseToggle.IsChecked == true && mouseToggle.IsEnabled && flight.ShowMouseOption && !menus.ShowMouseOption, "Mouse switch is available only for flight and reflects active mouse inputs");
            mouseToggle.IsChecked = false;
            Check(!interactionSession.Document.HasFlightMouseBindings && interactionSession.Document.HasDisabledFlightMouseBindings && model.SaveCommand.CanExecute(null), "Unchecking mouse removes all active flight mouse inputs in memory");
            Check(flight.Actions.Single(a => a.Key == "A").Rows.Count == 1 && menus.Actions.Single(a => a.Key == "A").Rows.Count == 3, "Mouse switch preserves keyboard alternatives and menu bindings");
            Check(File.ReadAllBytes(Path.Combine(root, "dinput.ini")).AsSpan().SequenceEqual(originalBindings), "Mouse switch does not write the INI before Save");
            Render(main, Path.Combine(artifacts, "main-mouse-off.png"), 1044, 740);
            mouseToggle.IsChecked = true;
            Check(interactionSession.Document.ToBytes().AsSpan().SequenceEqual(originalBindings) && !model.SaveCommand.CanExecute(null), "Rechecking mouse restores all original bytes and clears the dirty state");
            var groupSelector = Descendants<ComboBox>((DependencyObject)main.Content).Single(c => ReferenceEquals(c.ItemsSource, flight.GroupOptions));
            Check(Descendants<TextBlock>(groupSelector).Any(t => t.Text == "All groups"), "Themed dropdown shows the display name rather than a record dump");
            groupSelector.IsDropDownOpen = true;
            main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var groupPopup = (System.Windows.Controls.Primitives.Popup)groupSelector.Template.FindName("PART_Popup", groupSelector);
            Check(Descendants<TextBlock>(groupPopup.Child).Any(t => t.Text == "Weapons"), "Themed dropdown popup displays selectable group names");
            groupSelector.IsDropDownOpen = false;
            var tabs = Descendants<TabControl>((DependencyObject)main.Content).Single();
            var search = Descendants<TextBox>((DependencyObject)main.Content).Single(t => Equals(t.Tag, "ActionSearch"));
            var originalGun = flight.Actions.Single(a => a.Key == "A");
            search.Text = "gUn Mouse1";
            Check(flight.SearchText == "gUn Mouse1", "Search box updates the model while typing");
            Check(flight.Groups.Count == 1 && flight.Groups[0].Name == "Weapons" && flight.Groups[0].Actions.Count == 1, "Search combines words across action and binding names without case sensitivity");
            Check(ReferenceEquals(flight.Groups[0].Actions[0], originalGun) && originalGun.Rows.Count == 2, "Search retains the original action and every alternative binding");
            Render(main, Path.Combine(artifacts, "main-search.png"), 1044, 740);
            flight.SearchText = "0x24";
            Check(flight.Groups.SelectMany(g => g.Actions).Single().Key == "A", "Search matches raw hex codes");
            flight.SearchText = "mousex";
            Check(flight.Groups.SelectMany(g => g.Actions).Select(a => a.Key).Order().SequenceEqual(new[] { "LX+", "LX-" }.Order()), "Search includes mouse axis expressions");
            flight.SearchText = "";
            flight.SelectedGroupId = "Camera";
            Check(flight.Groups.Count == 1 && flight.Groups[0].Actions.Count == 5, "Category filter shows only camera actions");
            flight.SearchText = "missile";
            Check(flight.HasNoResults && flight.Groups.Count == 0, "Search and category combine and produce a clear empty state");
            Render(main, Path.Combine(artifacts, "main-no-results.png"), 844, 520);
            flight.ClearSearchCommand.Execute(null);
            Check(flight.SelectedGroupId == "Camera" && flight.Groups[0].Actions.Count == 5, "Clearing search preserves the category");
            flight.ResetFiltersCommand.Execute(null);
            Check(flight.SelectedGroupId == "All" && flight.SearchText == "" && flight.Groups.Sum(g => g.Actions.Count) == 24, "Reset restores all actions");
            flight.SearchText = "gun";
            flight.SelectedGroupId = "Weapons";
            menus.SearchText = "confirm";
            Check(menus.Groups.SelectMany(g => g.Actions).Single().Key == "A" && flight.SearchText == "gun", "Flight and menu searches are independent");
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            Check(flight.SelectedGroupId == "Weapons" && flight.SearchText == "gun" && flight.Groups.SelectMany(g => g.Actions).Single().Key == "A", "Document refresh preserves filters and updates matching actions");
            Check(!model.SaveCommand.CanExecute(null), "Grouping and filtering do not modify the INI");
            var unknownPath = Path.Combine(artifacts, "unknown-action.ini");
            File.WriteAllText(unknownPath, "[KeyboardFlight]\nCustomAction=0x24\n");
            model.OpenInitial(unknownPath);
            flight.ResetFiltersCommand.Execute(null);
            var unknown = flight.Groups.Single(g => g.Name == "Other").Actions.Single();
            Check(unknown.Key == "CustomAction" && !unknown.AddCommand.CanExecute(null) && !unknown.Rows[0].IsEditable, "Unknown actions remain visible and read-only in Other");
            Check(menus.Actions.Count == 0 && menus.Problem.Length > 0, "Missing sections show no phantom action results");
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            flight.ResetFiltersCommand.Execute(null);
            menus.ResetFiltersCommand.Execute(null);
            tabs.SelectedIndex = 1;
            Render(main, Path.Combine(artifacts, "main-menu.png"), 844, 520);
            Check(menus.Groups.Where(g => g.Name is not ("Navigation" or "Selection" or "Unused")).All(g => g.Description == UiText.Get("Menu.ReplayGroupNote")) &&
                menus.Groups.Where(g => g.Name is "Navigation" or "Selection").All(g => g.Description.Length == 0), "Menu replay labels exclude navigation, selection and unused entries");
            var unused = menus.Groups.Single(g => g.Name == "Unused");
            Check(unused.Description == UiText.Get("Menu.UnusedHint") && unused.Actions.All(a => a.Description == unused.Description) &&
                unused.Description.Contains("unknown", StringComparison.OrdinalIgnoreCase) && !unused.Description.Contains("replay", StringComparison.OrdinalIgnoreCase),
                "Unused group and action descriptions explain unknown INI entries without claiming replay functions");
            menus.SelectedGroupId = "Unused";
            Render(main, Path.Combine(artifacts, "main-menu-unused.png"), 1044, 740);
            menus.SelectedGroupId = "Camera";
            Render(main, Path.Combine(artifacts, "main-menu-replay.png"), 1044, 740);
            menus.ResetFiltersCommand.Execute(null);
            Check(tabs.SelectedIndex == 1, "Menu tab remains selected");
            Check(model.Sections[0].Actions.Single(a => a.Key == "A").Name == "Fire gun", "Flight action uses English gameplay name");
            Check(model.Sections[1].Actions.Single(a => a.Key == "A").Name == "Confirm selection", "Menu action uses separate English gameplay name");
            Check(model.Sections[0].Actions.Single(a => a.Key == "LY-").Name == "Pitch down", "Forward flight axis lowers nose");
            Check(model.Sections[0].ConflictText.Contains("Yaw left"), "Shared bindings use English gameplay names");
            Check(UiText.Get("Save") == "Save" && UiText.Culture.TwoLetterISOLanguageName == "en", "App language stays English on a Russian host");
            Check(typeof(MainViewModel).Assembly.GetTypes().All(t => !t.Name.Contains("Gamepad", StringComparison.OrdinalIgnoreCase)) &&
                typeof(MainViewModel).Assembly.GetReferencedAssemblies().All(a => !a.Name!.StartsWith("Vortice", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.StartsWith("SharpGen", StringComparison.OrdinalIgnoreCase)), "Gamepad windows, capture backend and DirectInput package dependencies are absent");
            var deviceOnlyPath = Path.Combine(artifacts, "device-only.ini");
            var deviceOnlyBytes = System.Text.Encoding.UTF8.GetBytes("[0]\nA=B1\nFFB=one\nFFB=two\n");
            File.WriteAllBytes(deviceOnlyPath, deviceOnlyBytes);
            model.OpenInitial(deviceOnlyPath);
            Check(model.Sections.Count == 2 && model.Sections.All(s => s.Actions.Count == 0 && s.Problem.Length > 0) && !model.SaveCommand.CanExecute(null),
                "Device-only documents expose no editable gamepad tab or bindings");
            Check(File.ReadAllBytes(deviceOnlyPath).AsSpan().SequenceEqual(deviceOnlyBytes), "Viewing a device-only file does not change its bytes");
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            var warningDocument = IniDocument.Parse(File.ReadAllBytes(Path.Combine(root, "dinput.ini")));
            var keyboardWarning = new BindingDialog("KeyboardFlight", "Y", "0x24", warningDocument);
            var warningText = (TextBlock)keyboardWarning.FindName("SharedWarning");
            Check(warningText.Visibility == Visibility.Visible && warningText.Text.Contains("Fire gun"), "Keyboard assignment warns when another action in the same tab uses the input");
            Render(keyboardWarning, Path.Combine(artifacts, "binding-shared-warning.png"), 624, 510);
            ((ComboBox)keyboardWarning.FindName("Choices")).SelectedItem = Bindings.Find("0x3F");
            Check(warningText.Visibility == Visibility.Collapsed && ((Button)keyboardWarning.FindName("ApplyButton")).IsEnabled, "Choosing an unused key clears the warning without blocking Apply");
            keyboardWarning.Close();
            var crossTabWarning = new BindingDialog("KeyboardUI", "Y", "0x1D", warningDocument);
            Check(((TextBlock)crossTabWarning.FindName("SharedWarning")).Visibility == Visibility.Collapsed, "A key used only in Flight does not warn in Menus");
            crossTabWarning.Close();
            void AddConflictingBinding(bool accept)
            {
                main.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var assignment = app.Windows.OfType<BindingDialog>().Single();
                    ((ComboBox)assignment.FindName("Choices")).SelectedItem = Bindings.Find("0x24");
                    main.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var confirmation = app.Windows.OfType<MessageDialog>().Single();
                        Render(confirmation, Path.Combine(artifacts, "shared-input-confirmation.png"), 624, 300);
                        var answer = ((WrapPanel)confirmation.FindName("Buttons")).Children.Cast<Button>()
                            .Single(b => Equals(b.Content, UiText.Get(accept ? "Yes" : "No")));
                        answer.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }));
                    ((Button)assignment.FindName("ApplyButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
                flight.Actions.Single(a => a.Key == "B").AddCommand.Execute(null);
            }
            interactionSession = (ConfigSession)typeof(MainViewModel).GetField("session", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(model)!;
            var beforeConflict = interactionSession.Document.ToBytes();
            AddConflictingBinding(false);
            Check(interactionSession.Document.ToBytes().AsSpan().SequenceEqual(beforeConflict), "Declining the shared-input confirmation leaves the document unchanged");
            AddConflictingBinding(true);
            Check(interactionSession.Document.Entries("KeyboardFlight").Any(e => e.Key == "B" && e.Value == "0x24") &&
                interactionSession.Document.Entries("KeyboardFlight").Any(e => e.Key == "A" && e.Value == "0x24"), "Accepting the shared-input warning keeps both assignments");
            interactionSession.DiscardChanges();
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            var confirmation = new MessageDialog(UiText.Get("UnsavedPrompt"), "ACZeroPCremap", MessageBoxButton.YesNoCancel, savePrompt: true);
            var labels = ((WrapPanel)confirmation.FindName("Buttons")).Children.Cast<Button>().Select(b => (string)b.Content).ToArray();
            Check(labels.SequenceEqual(new[] { "Save", "Don’t save", "Cancel" }), "Confirmation buttons use app resources rather than OS language");
            Render(confirmation, Path.Combine(artifacts, "confirmation-dialog.png"), 564, 270);
            confirmation.Close();
            var dialog = new BindingDialog("KeyboardFlight", "A", "0x24");
            Check(((TextBlock)dialog.FindName("ActionLabel")).Text == "Fire gun", "Assignment dialog uses English gameplay name");
            var handle = new WindowInteropHelper(dialog).EnsureHandle();
            var button = (Button)dialog.FindName("CaptureButton");
            var choices = (ComboBox)dialog.FindName("Choices");
            var apply = (Button)dialog.FindName("ApplyButton");
            var mouseTarget = (TextBlock)dialog.FindName("CaptureStatus");
            Check(apply.IsEnabled, "Existing binding can be applied");

            // Real queued input passes through ComponentDispatcher before the window hook.
            // A SendMessage-only check misses keys consumed by WPF controls (notably Space).
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(button.Content.ToString() == UiText.Get("MouseAxis.Stop"), "Input capture button becomes Stop");
            Render(dialog, Path.Combine(artifacts, "input-capture-stop.png"), 664, 600);
            var stopLabel = Descendants<ContentPresenter>(button).First();
            Check(!RaiseMouse(stopLabel, MouseButton.Left, up: false).Handled && !RaiseMouse(stopLabel, MouseButton.Left, up: true).Handled,
                "Clicking the Stop label passes through capture on both press and release");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(choices.IsEnabled && apply.IsEnabled && (choices.SelectedItem as InputChoice)?.Value == "0x24" && dialog.Result is null &&
                button.Content.ToString() == UiText.Get("Capture") && ((TextBlock)dialog.FindName("CaptureStatus")).Text == UiText.Get("CaptureStopped"),
                "Input Stop preserves the binding instead of assigning Mouse1 or applying it");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            // A negative wheel delta is a packed unsigned WPARAM on x64,
            // not a virtual-key code that fits IntPtr.ToInt32().
            var inputHook = typeof(BindingDialog).GetMethod("InputHook", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            foreach (var mouseMessage in new[] { 0x020A, 0x020E })
                inputHook.Invoke(dialog, [handle, mouseMessage, new IntPtr(0xFF880000L), IntPtr.Zero, false]);
            Check(!apply.IsEnabled, "Packed negative wheel messages do not overflow or finish keyboard capture");
            var space = new MSG
            {
                hwnd = handle, message = 0x0100, wParam = (IntPtr)0x20,
                lParam = (IntPtr)(1L | (0x39L << 16))
            };
            Check(ComponentDispatcher.RaiseThreadMessage(ref space), "Queued Space is consumed before WPF button handling");
            Check(choices.SelectedItem is InputChoice spaceChoice && spaceChoice.Value == "0x39", "Queued Space assigns DIK_SPACE");
            Check(apply.IsEnabled, "Space can be applied after queued capture");
            Check(button.Content.ToString() == UiText.Get("Capture"), "Keyboard capture restores the capture label after success");

            // Capture only messages for this dialog, and release held keys even
            // when queued input would otherwise go through WPF's keyboard sink.
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var unrelated = space;
            unrelated.hwnd = new WindowInteropHelper(main).Handle;
            var previousChoice = choices.SelectedItem;
            // WPF may handle a key for its own window; verify that the capture
            // dialog ignores it rather than asserting the whole thread leaves it unhandled.
            ComponentDispatcher.RaiseThreadMessage(ref unrelated);
            Check(ReferenceEquals(choices.SelectedItem, previousChoice), "Capture ignores messages for other windows");
            Check(!apply.IsEnabled, "Other windows cannot complete capture");
            var repeated = space;
            repeated.lParam = (IntPtr)(space.lParam.ToInt64() | (1L << 30));
            Check(ComponentDispatcher.RaiseThreadMessage(ref repeated), "Queued auto-repeat is consumed");
            Check(!apply.IsEnabled, "Auto-repeat cannot complete capture");
            Check(ComponentDispatcher.RaiseThreadMessage(ref space), "Fresh Space completes capture after auto-repeat");

            // Feed native key messages into this dialog's own window procedure. No global hooks or desktop automation.
            foreach (var (vk, scan, extended, expected) in new (int, int, bool, string)[]
            {
                (0x1B, 0x01, false, "0x01"), (0x09, 0x0F, false, "0x0F"),
                (0x0D, 0x1C, false, "0x1C"), (0x0D, 0x1C, true, "0x9C"),
                (0x11, 0x1D, true, "0x9D"), (0x10, 0x36, false, "0x36"),
                (0x12, 0x38, true, "0xB8"), (0x26, 0x48, true, "0xC8")
            })
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(!choices.IsEnabled && !apply.IsEnabled, "Capture suspends regular controls");
                var bits = 1L | ((long)scan << 16) | (extended ? 1L << 24 : 0);
                var message = new MSG { hwnd = handle, message = vk == 0x12 ? 0x0104 : 0x0100, wParam = (IntPtr)vk, lParam = (IntPtr)bits };
                // Simulate release before the fresh press so a key physically
                // held by the user running these checks cannot invalidate it.
                var release = message;
                release.message = vk == 0x12 ? 0x0105 : 0x0101;
                release.lParam = (IntPtr)(bits | (1L << 30) | (1L << 31));
                Check(ComponentDispatcher.RaiseThreadMessage(ref release), $"Queued release {expected}");
                Check(ComponentDispatcher.RaiseThreadMessage(ref message), $"Queued key {expected} consumed before WPF");
                Check(choices.SelectedItem is InputChoice choice && choice.Value == expected,
                    $"Queued capture {expected} (selected: {(choices.SelectedItem as InputChoice)?.Value}; status: {((TextBlock)dialog.FindName("CaptureStatus")).Text})");
                Check(choices.IsEnabled && apply.IsEnabled, "Capture restores regular controls");
            }
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            SendMessage(handle, 0x0100, (IntPtr)0xFF, IntPtr.Zero);
            Check(!apply.IsEnabled, "Unsupported key cannot be applied during capture");
            SendMessage(handle, 0x0100, (IntPtr)0x4B, (IntPtr)(1L | (0x25L << 16)));
            RaiseMouse(button, MouseButton.Left, up: false);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            RaiseMouse(button, MouseButton.Left, up: true);
            Check(!apply.IsEnabled && choices.SelectedItem is InputChoice openingChoice && openingChoice.Value == "0x25",
                "The click that starts capture does not assign the left mouse button");
            foreach (var (mouseButton, expected) in new[] { (MouseButton.Left, "Mouse1"), (MouseButton.Right, "Mouse2"), (MouseButton.Middle, "Mouse3") })
            {
                if (apply.IsEnabled) button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                RaiseMouse(mouseTarget, mouseButton, up: true);
                var down = RaiseMouse(mouseTarget, mouseButton, up: false);
                Check(down.Handled && choices.SelectedItem is InputChoice mouseChoice && mouseChoice.Value == expected && apply.IsEnabled,
                    "Mouse capture assigns " + expected);
                Check(RaiseMouse(mouseTarget, mouseButton, up: true).Handled && dialog.Result is null,
                    "Captured mouse release is consumed without applying a binding: " + expected);
            }
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            RaiseMouse(apply, MouseButton.Left, up: true);
            Check(RaiseMouse(apply, MouseButton.Left, up: false).Handled && apply.IsEnabled &&
                RaiseMouse(apply, MouseButton.Left, up: true).Handled && dialog.Result is null,
                "Capturing over Apply cannot activate it on the captured press or release");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var beforeUnsupportedMouse = choices.SelectedItem;
            RaiseMouse(button, MouseButton.XButton1, up: true);
            Check(RaiseMouse(button, MouseButton.XButton1, up: false).Handled && !apply.IsEnabled && ReferenceEquals(choices.SelectedItem, beforeUnsupportedMouse),
                "Unsupported side mouse buttons keep capture active and preserve the previous binding");
            Render(dialog, Path.Combine(artifacts, "binding-unsupported-input.png"), 624, 510);
            RaiseMouse(button, MouseButton.XButton1, up: true);
            RaiseMouse(mouseTarget, MouseButton.Left, up: true);
            RaiseMouse(mouseTarget, MouseButton.Left, up: false);
            RaiseMouse(mouseTarget, MouseButton.Left, up: true);
            foreach (var (delta, expected) in new[] { (120, "MouseZ:0:1"), (-120, "MouseZ:1:0") })
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta) { RoutedEvent = Mouse.PreviewMouseWheelEvent };
                button.RaiseEvent(wheel);
                Check(wheel.Handled && choices.SelectedItem is InputChoice wheelChoice && wheelChoice.Value == expected && apply.IsEnabled && dialog.Result is null,
                    "Wheel capture selects and previews " + expected + " without applying");
            }
            Render(dialog, Path.Combine(artifacts, "binding-dialog.png"), 624, 510);
            dialog.Close();
            var axisDocument = IniDocument.Parse(File.ReadAllBytes(Path.Combine(root, "dinput.ini")));
            var axisDialog = new BindingDialog("KeyboardFlight", "RX-", "0x38 + MouseAX:256:0", axisDocument);
            new WindowInteropHelper(axisDialog).EnsureHandle();
            var axisButton = (Button)axisDialog.FindName("AxisCaptureButton");
            var axisPanel = (Border)axisDialog.FindName("AxisCapturePanel");
            var axisChoices = (ComboBox)axisDialog.FindName("Choices");
            Check(((Button)axisDialog.FindName("ApplyButton")).IsEnabled && (axisChoices.SelectedItem as InputChoice)?.Value == "0x38 + MouseAX:256:0",
                "Existing absolute mouse axes open for editing without losing their condition");
            axisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(axisPanel.Visibility == Visibility.Visible && !axisChoices.IsEnabled, "Separate movement capture opens a dedicated area");
            Check(axisButton.Content.ToString() == UiText.Get("MouseAxis.Stop"), "Movement capture button becomes Stop");
            Render(axisDialog, Path.Combine(artifacts, "mouse-axis-capture.png"), 664, 600);
            Check(!RaiseMouse(axisButton, MouseButton.Left, up: false).Handled && !RaiseMouse(axisButton, MouseButton.Left, up: true).Handled,
                "Stop receives both mouse press and release during movement capture");
            axisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var observeMotion = typeof(BindingDialog).GetMethod("ObserveAxisMovement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            observeMotion.Invoke(axisDialog, [100d, 100d, Environment.TickCount64 + 300]);
            observeMotion.Invoke(axisDialog, [150d, 100d, Environment.TickCount64 + 350]);
            Check(axisPanel.Visibility == Visibility.Collapsed && axisChoices.IsEnabled && ((Button)axisDialog.FindName("ApplyButton")).IsEnabled &&
                (axisChoices.SelectedItem as InputChoice)?.Value == "0x38 + MouseAX:256:0" && axisDialog.Result is null &&
                axisButton.Content.ToString() == UiText.Get("MouseAxis.Capture") && ((TextBlock)axisDialog.FindName("CaptureStatus")).Text == UiText.Get("CaptureStopped"),
                "Stop ends capture and ignores further motion without changing or applying the previous selection");
            axisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var beforeEntry = typeof(BindingDialog).GetField("movementCapture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(axisDialog);
            axisPanel.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            Check(!ReferenceEquals(beforeEntry, typeof(BindingDialog).GetField("movementCapture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(axisDialog)),
                "Entering the capture box rearms the delay instead of assigning the approach motion");
            var motion = (MouseMovementCapture)typeof(BindingDialog).GetField("movementCapture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(axisDialog)!;
            observeMotion.Invoke(axisDialog, [100d, 100d, motion.StartedAt + 300]);
            observeMotion.Invoke(axisDialog, [100d, 50d, motion.StartedAt + 350]);
            Check(axisPanel.Visibility == Visibility.Collapsed && (axisChoices.SelectedItem as InputChoice)?.Value == "0x38 + MouseAY:256:0",
                "Movement capture retains absolute mode, coefficient and Alt while changing direction");
            Check(axisButton.Content.ToString() == UiText.Get("MouseAxis.Capture"), "Successful movement capture restores the capture button label");
            Render(axisDialog, Path.Combine(artifacts, "mouse-axis-selected.png"), 664, 600);
            axisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            axisButton.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
            Check((axisChoices.SelectedItem as InputChoice)?.Value == "0x38 + MouseAY:256:0" && axisPanel.Visibility == Visibility.Visible && !axisChoices.IsEnabled,
                "Wheel input does not replace a binding or end movement-only capture");
            axisDialog.Close();
            var newAxisDialog = new BindingDialog("KeyboardFlight", "LX-", null, axisDocument);
            ((Button)newAxisDialog.FindName("AxisCaptureButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var newMotion = (MouseMovementCapture)typeof(BindingDialog).GetField("movementCapture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(newAxisDialog)!;
            var observeNew = typeof(BindingDialog).GetMethod("ObserveAxisMovement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            observeNew.Invoke(newAxisDialog, [100d, 100d, newMotion.StartedAt + 300]);
            observeNew.Invoke(newAxisDialog, [150d, 100d, newMotion.StartedAt + 350]);
            Check(((ComboBox)newAxisDialog.FindName("Choices")).SelectedItem is InputChoice addedAxis && addedAxis.Value == "!0x38 + MouseX:0:8",
                "Adding a movement alternative inherits the action's existing coefficient and Alt condition");
            newAxisDialog.Close();
            foreach (var captureButtonName in new[] { "CaptureButton", "AxisCaptureButton" })
            {
            var cancelCapture = new BindingDialog("KeyboardFlight", "A", "0x24")
            {
                Owner = main, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000
            };
            main.Dispatcher.BeginInvoke(new Action(() =>
            {
                ((Button)cancelCapture.FindName(captureButtonName)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var cancelButton = (Button)cancelCapture.FindName("CancelButton");
                Check(!RaiseMouse(cancelButton, MouseButton.Left, up: false).Handled, "Cancel click is not captured as Mouse1");
                cancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }));
            Check(cancelCapture.ShowDialog() == false && cancelCapture.Result is null, "Cancel closes capture without assigning anything: " + captureButtonName);
            }
            var languageSelector = (ComboBox)main.FindName("LanguageSelector");
            var sessionField = typeof(MainViewModel).GetField("session", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var activeSession = (ConfigSession)sessionField.GetValue(model)!;
            var gun = activeSession.Document.Entries("KeyboardFlight").First(e => e.Key == "A" && e.Value == "0x24");
            activeSession.Document.Replace("KeyboardFlight", gun.Id, "0x25");
            var dirtyBytes = activeSession.Document.ToBytes();
            flight.SearchText = "0x25";
            flight.SelectedGroupId = "Weapons";
            tabs.SelectedIndex = 1;
            menus.SelectedGroupId = "Unused";
            foreach (var language in ACZeroPCremap.Core.Localization.LanguagePreferences.Languages.Where(l => l.Code != "en"))
            {
                languageSelector.SelectedValue = language.Code;
                main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Check(UiText.Culture.Name == language.Code && model.SelectedLanguage == language.Code, "Language picker applies " + language.NativeName);
                Check(preferences.Load() == language.Code, "Language preference persists separately: " + language.Code);
                Check(ReferenceEquals(activeSession, sessionField.GetValue(model)) && activeSession.Document.ToBytes().AsSpan().SequenceEqual(dirtyBytes) && model.SaveCommand.CanExecute(null), "Language switch preserves the session and unsaved bytes: " + language.Code);
                Check(flight.SearchText == "0x25" && flight.SelectedGroupId == "Weapons" && menus.SelectedGroupId == "Unused" && tabs.SelectedIndex == 1, "Language switch preserves both keyboard tabs and their filters: " + language.Code);
                var activeCombos = Descendants<ComboBox>((DependencyObject)main.Content).ToArray();
                var categoryCombo = activeCombos.Single(c => ReferenceEquals(c.ItemsSource, menus.GroupOptions));
                Check(Equals(categoryCombo.SelectedValue, "Unused") && Descendants<TextBlock>(categoryCombo).Any(t => t.Text == UiText.Get("Group.Unused")), "Translated category selector keeps its visible selection: " + language.Code);
                Check(flight.Title == UiText.Get("FlightTab") && flight.Actions.Single(a => a.Key == "A").Name == UiText.Get("Flight.A"), "Action names and tabs update live: " + language.Code);
                Check(Descendants<TextBlock>((DependencyObject)main.Content).Any(t => t.Text == UiText.Get("UI.ControlTitle")), "XAML resources update live: " + language.Code);
                Check(model.Sections.Count == 2 && menus.Groups.Count == 1 && menus.Groups[0].Description == UiText.Get("Menu.UnusedHint"),
                    $"Unknown INI description updates live and no gamepad tab returns: {language.Code} (groups: {menus.Groups.Count}; search: {menus.SearchText}; category: {menus.SelectedGroupId})");
                Render(main, Path.Combine(artifacts, "main-" + language.Code + ".png"), 1044, 740);
                var translatedDialog = new BindingDialog("KeyboardFlight", "A", "0x24");
                Check(((Button)translatedDialog.FindName("ApplyButton")).Content.ToString() == UiText.Get("Apply"), "New dialogs use selected language: " + language.Code);
                var translatedAxisButton = (Button)translatedDialog.FindName("AxisCaptureButton");
                translatedAxisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(translatedAxisButton.Content.ToString() == UiText.Get("MouseAxis.Stop"), "Stop uses selected language: " + language.Code);
                translatedAxisButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(translatedAxisButton.Content.ToString() == UiText.Get("MouseAxis.Capture") &&
                    ((TextBlock)translatedDialog.FindName("CaptureStatus")).Text == UiText.Get("CaptureStopped"), "Stopped capture restores translated label and status: " + language.Code);
                var translatedInputButton = (Button)translatedDialog.FindName("CaptureButton");
                translatedInputButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(translatedInputButton.Content.ToString() == UiText.Get("MouseAxis.Stop"), "Input Stop uses selected language: " + language.Code);
                translatedInputButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(translatedInputButton.Content.ToString() == UiText.Get("Capture") &&
                    ((TextBlock)translatedDialog.FindName("CaptureStatus")).Text == UiText.Get("CaptureStopped"), "Stopped input capture restores translated label and status: " + language.Code);
                Render(translatedDialog, Path.Combine(artifacts, "binding-" + language.Code + ".png"), 624, 510);
                translatedDialog.Close();
            }
            languageSelector.SelectedValue = "en";
            activeSession.DiscardChanges();
            model.OpenInitial(Path.Combine(root, "dinput.ini"));
            flight.ResetFiltersCommand.Execute(null);
            // Simulate the usable physical display area at common DPI settings.
            // Native per-monitor behavior is checked separately below.
            tabs.SelectedIndex = 0;
            menus.ResetFiltersCommand.Execute(null);
            foreach (var (displayWidth, displayHeight) in new[] { (1366d, 720d), (1920d, 1040d) })
            foreach (var scale in new[] { 1d, 1.25, 1.5 })
            {
                var bounds = WindowLayout.Fit(null, new(0, 0, displayWidth, displayHeight, scale));
                var width = (int)(bounds.Width / scale) - 2;
                var height = (int)(bounds.Height / scale) - 34;
                Render(main, Path.Combine(artifacts, $"layout-{displayWidth}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png"), width, height);
                var rootPanel = (FrameworkElement)main.Content;
                var saveButton = (Button)main.FindName("SaveFileButton");
                var savePosition = saveButton.TranslatePoint(new Point(), rootPanel);
                var list = Descendants<ScrollViewer>(rootPanel).Single(s => Equals(s.Tag, "ActionList"));
                Check(savePosition.Y >= 0 && savePosition.Y + saveButton.ActualHeight <= rootPanel.ActualHeight && list.ViewportHeight >= 40,
                    $"Save remains visible and actions can scroll at {displayWidth} px / {scale:P0} DPI (list viewport: {list.ViewportHeight:F0} DIP)");
            }
            foreach (var code in new[] { "ru", "de", "fr", "es-ES", "pt-BR" })
            {
                languageSelector.SelectedValue = code;
                Render(main, Path.Combine(artifacts, "compact-" + code + ".png"), 884, 422);
                var compactList = Descendants<ScrollViewer>((DependencyObject)main.Content).Single(s => Equals(s.Tag, "ActionList"));
                Check(compactList.ViewportHeight >= 40, "Translated compact layout retains a scrollable action list: " + code);
                var moreButton = (Button)main.FindName("MoreActionsButton");
                moreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                var moreItems = moreButton.ContextMenu.Items.Cast<MenuItem>().ToArray();
                Check(moreButton.IsVisible && moreItems.Length == 3 && ReferenceEquals(moreItems[0].Command, model.DiscardCommand) &&
                    ReferenceEquals(moreItems[1].Command, model.ReloadCommand) && ReferenceEquals(moreItems[2].Command, model.RestoreCommand) &&
                    moreItems[2].Header.ToString() == UiText.Get("Restore"), "Compact menu retains all secondary actions and their translations: " + code);
                moreButton.ContextMenu.IsOpen = false;
            }
            languageSelector.SelectedValue = "en";
            var placementPreferences = new ACZeroPCremap.Core.Localization.LanguagePreferences(Path.Combine(artifacts, "window-placement-settings.json"));
            placementPreferences.Save("fr");
            placementPreferences.SaveWindowPlacement(new(40000, 40000, 1100, 820));
            var placed = new MainWindow(false, placementPreferences) { ShowInTaskbar = false, ShowActivated = false, Opacity = 0 };
            placed.Show();
            placed.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(placed.Left < 40000 && placed.Top < 40000 && placed.ActualWidth > 0, "A removed monitor's saved location returns to an available screen");
            Check(IsInsideWorkArea(placed), "Initial native window bounds fit the monitor work area");
            Render(placed, Path.Combine(artifacts, "compact-no-file.png"), 884, 422);
            var noFilePath = (TextBox)placed.FindName("FilePathBox");
            Check(placed.IsCompact && noFilePath.TextWrapping == TextWrapping.Wrap && noFilePath.LineCount >= 3,
                "Compact no-file hint shows the location and first-launch reminder on separate readable lines");
            ((MainViewModel)placed.DataContext).OpenInitial(Path.Combine(root, "dinput.ini"));
            Check(noFilePath.TextWrapping == TextWrapping.NoWrap, "Opening a file restores the compact single-line path");
            placed.WindowState = WindowState.Maximized;
            placed.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(IsInsideWorkArea(placed), "Maximization respects the taskbar work area");
            placed.WindowState = WindowState.Minimized;
            placed.Close();
            var savedPlacement = placementPreferences.LoadWindowPlacement();
            Check(savedPlacement is { IsValid: true, Maximized: true } && savedPlacement.Left > -30000 && savedPlacement.Top > -30000 && placementPreferences.Load() == "fr",
                "Closing a minimized window preserves normal bounds, previous maximized state and language");
            var restored = new MainWindow(false, placementPreferences) { ShowInTaskbar = false, ShowActivated = false, Opacity = 0 };
            restored.Show();
            restored.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(restored.WindowState == WindowState.Maximized, "Window state restores on the next launch");
            restored.Close();
            // Exercise the shared window template as well as content-only previews.
            main.WindowStartupLocation = WindowStartupLocation.Manual;
            main.Left = -10000;
            main.Top = -10000;
            main.ShowInTaskbar = false;
            main.MinWidth = 900;
            main.MinHeight = 720;
            main.Width = 1100;
            main.Height = 820;
            main.Show();
            main.UpdateLayout();
            main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(main.Template.FindName("Maximize", main) is Button maximize && maximize.Visibility == Visibility.Visible && SystemCommands.MaximizeWindowCommand.CanExecute(main, main), "Custom main-window caption exposes working window commands");
            var frame = new RenderTargetBitmap((int)main.ActualWidth, (int)main.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            frame.Render(main);
            var frameEncoder = new PngBitmapEncoder();
            frameEncoder.Frames.Add(BitmapFrame.Create(frame));
            using (var file = File.Create(Path.Combine(artifacts, "main-window-frame.png"))) frameEncoder.Save(file);
            SystemCommands.CloseWindowCommand.Execute(main, main);
            main.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check(!main.IsVisible, "Custom close command closes a clean window");
            Check(errors.Count == 0, "No WPF binding errors: " + string.Join(" | ", errors));
            Console.WriteLine($"UI checks passed. Rendered previews: {artifacts}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { app.Shutdown(); }
    }

    private static void Render(Window window, string path, int width, int height)
    {
        if (window.IsVisible)
        {
            // Keep the loaded native window and content at the same dimensions;
            // otherwise its dispatcher relayout overwrites the preview's arrange.
            window.MinWidth = window.MinHeight = 0;
            window.Width = width + 2;
            window.Height = height + 34;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var context = background.RenderOpen()) context.DrawRectangle(window.Background, null, new Rect(0, 0, width, height));
        image.Render(background);
        image.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) yield return result;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static MouseButtonEventArgs RaiseMouse(UIElement target, MouseButton button, bool up)
    {
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, button)
        {
            RoutedEvent = up ? Mouse.PreviewMouseUpEvent : Mouse.PreviewMouseDownEvent
        };
        target.RaiseEvent(args);
        return args;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private static bool IsInsideWorkArea(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var monitor = MonitorFromWindow(handle, 1);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfo(monitor, ref info) && GetWindowRect(handle, out var rect) &&
            rect.Left >= info.Work.Left - 2 && rect.Top >= info.Work.Top - 2 && rect.Right <= info.Work.Right + 2 && rect.Bottom <= info.Work.Bottom + 2;
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    private sealed class BindingErrorListener(List<string> errors) : System.Diagnostics.TraceListener
    {
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) errors.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }
}
