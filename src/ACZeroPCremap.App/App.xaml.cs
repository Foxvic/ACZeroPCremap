using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ACZeroPCremap.Core.Localization;

namespace ACZeroPCremap.App;

public partial class App : Application
{
    private readonly bool openStartupWindow;
    public App() : this(openStartupWindow: true) { }
    public App(bool openStartupWindow)
    {
        this.openStartupWindow = openStartupWindow;
        // Also localize built-in WPF commands, such as the text box Copy menu.
        ApplyCulture();
        Text.CultureChanged += OnCultureChanged;
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(SystemCommands.CloseWindowCommand,
            (sender, args) => { ((Window)sender).Close(); args.Handled = true; }));
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(SystemCommands.MinimizeWindowCommand,
            (sender, args) => { SystemCommands.MinimizeWindow((Window)sender); args.Handled = true; }));
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(SystemCommands.MaximizeWindowCommand,
            (sender, args) =>
            {
                var window = (Window)sender;
                if (window.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
                else SystemCommands.MaximizeWindow(window);
                args.Handled = true;
            }));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (openStartupWindow)
        {
            Text.Culture = LanguagePreferences.CultureFor(new LanguagePreferences(LanguagePreferences.DefaultPath).Load());
            new MainWindow().Show();
        }
    }

    private static void ApplyCulture()
    {
        CultureInfo.DefaultThreadCurrentUICulture = Text.Culture;
        CultureInfo.CurrentUICulture = Text.Culture;
    }
    private void OnCultureChanged(object? sender, EventArgs e) => ApplyCulture();
    protected override void OnExit(ExitEventArgs e)
    {
        Text.CultureChanged -= OnCultureChanged;
        base.OnExit(e);
    }
}
