using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ACZeroPCremap.Core.Localization;

namespace ACZeroPCremap.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly WindowPlacementController? placement;

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); private set => SetValue(IsCompactProperty, value); }

    public MainWindow() : this(openStartupFile: true) { }

    public MainWindow(bool openStartupFile, LanguagePreferences? languagePreferences = null, bool manageWindowPlacement = true)
    {
        InitializeComponent();
        languagePreferences ??= new(LanguagePreferences.DefaultPath);
        viewModel = new(this, languagePreferences: languagePreferences);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.FilePath)) UpdateCompactLayout();
        };
        DataContext = viewModel;
        if (manageWindowPlacement) placement = new(this, languagePreferences);
        SizeChanged += (_, _) => UpdateCompactLayout();
        if (openStartupFile) Loaded += (_, _) =>
        {
            var argument = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
            var adjacent = Path.Combine(AppContext.BaseDirectory, "dinput.ini");
            viewModel.OpenInitial(argument ?? (File.Exists(adjacent) ? adjacent : null));
        };
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = !viewModel.CanLeave();
        if (!e.Cancel) placement?.Save();
    }

    private void UpdateCompactLayout()
    {
        IsCompact = ActualHeight < 720 || ActualWidth < 1000;
        MainRoot.Margin = IsCompact ? new(12, 8, 12, 8) : new(24, 18, 24, 18);
        HeaderGrid.Margin = new(0, 0, 0, IsCompact ? 6 : 12);
        HeaderPath.Visibility = HeaderSubtitle.Visibility = QuotePanel.Visibility = LanguageLabel.Visibility =
            ConfigHeader.Visibility = FooterNote.Visibility = BuildText.Visibility = SecondaryActions.Visibility = IsCompact ? Visibility.Collapsed : Visibility.Visible;
        MoreActionsButton.Visibility = IsCompact ? Visibility.Visible : Visibility.Collapsed;
        QuoteColumn.Width = IsCompact ? new(0) : new(1, GridUnitType.Star);
        HeaderTitle.FontSize = IsCompact ? 22 : 27;
        LanguagePanel.Width = IsCompact ? 180 : 210;
        ConfigPanel.Padding = IsCompact ? new(10, 5, 10, 5) : new(12, 8, 12, 8);
        FilePathBox.TextWrapping = IsCompact && viewModel.HasFile ? TextWrapping.NoWrap : TextWrapping.Wrap;
        Toolbar.Margin = new(0, 0, 0, IsCompact ? 2 : 10);
        FooterPanel.Margin = new(0, IsCompact ? 6 : 12, 0, 0);
    }

    private void OpenMoreActions(object sender, RoutedEventArgs e)
    {
        MoreActionsButton.ContextMenu.PlacementTarget = MoreActionsButton;
        MoreActionsButton.ContextMenu.IsOpen = true;
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        // Always start at the first result after typing or changing the category.
        foreach (var list in Descendants<ScrollViewer>(this).Where(s => Equals(s.Tag, "ActionList"))) list.ScrollToTop();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F || Keyboard.Modifiers != ModifierKeys.Control) return;
        var search = Descendants<TextBox>(this).FirstOrDefault(t => Equals(t.Tag, "ActionSearch"));
        if (search is null) return;
        search.Focus();
        search.SelectAll();
        e.Handled = true;
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
}
