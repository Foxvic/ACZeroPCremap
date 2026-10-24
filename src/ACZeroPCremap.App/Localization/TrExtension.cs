using System.Windows.Markup;
using System.ComponentModel;
using System.Windows.Data;
using ACZeroPCremap.Core.Localization;

namespace ACZeroPCremap.App.Localization;

/// <summary>Live resource binding, including inside shared control templates.</summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TrExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding($"[{key}]")
    { Source = LocalizedStrings.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}

public sealed class LocalizedStrings : INotifyPropertyChanged
{
    public static LocalizedStrings Instance { get; } = new();
    private LocalizedStrings() => Text.CultureChanged += (_, _) => PropertyChanged?.Invoke(this, new("Item[]"));
    public string this[string key] => Text.Get(key);
    public event PropertyChangedEventHandler? PropertyChanged;
}
