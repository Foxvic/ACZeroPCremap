using System.Windows;
using System.Windows.Controls;
using ACZeroPCremap.Core.Localization;

namespace ACZeroPCremap.App;

/// <summary>App-owned buttons stay in the app language even on a differently localized OS.</summary>
public partial class MessageDialog : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public MessageDialog(string message, string title, MessageBoxButton buttons, bool savePrompt = false)
    {
        InitializeComponent();
        _ = new WindowPlacementController(this, minWidth: 400, minHeight: 180);
        Title = title;
        MessageText.Text = message;
        if (buttons == MessageBoxButton.OK) AddButton("OK", MessageBoxResult.OK, isDefault: true);
        else
        {
            AddButton(savePrompt ? "Save" : "Yes", MessageBoxResult.Yes, isDefault: true);
            AddButton(savePrompt ? "DontSave" : "No", MessageBoxResult.No);
            if (buttons == MessageBoxButton.YesNoCancel) AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true);
        }
    }

    private void AddButton(string resource, MessageBoxResult result, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button
        {
            Content = Text.Get(resource), IsDefault = isDefault, IsCancel = isCancel,
            MinWidth = 85, Margin = new Thickness(8, 0, 0, 6)
        };
        if (isDefault) button.SetResourceReference(StyleProperty, "PrimaryButton");
        button.Click += (_, _) => { Result = result; DialogResult = result is MessageBoxResult.Yes or MessageBoxResult.OK; };
        Buttons.Children.Add(button);
    }

    public static MessageBoxResult Show(Window owner, string message, string title,
        MessageBoxButton buttons = MessageBoxButton.OK, bool savePrompt = false)
    {
        var dialog = new MessageDialog(message, title, buttons, savePrompt) { Owner = owner };
        dialog.ShowDialog();
        return dialog.Result;
    }
}
