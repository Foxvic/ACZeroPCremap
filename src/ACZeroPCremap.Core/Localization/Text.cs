using System.Globalization;
using System.Resources;

namespace ACZeroPCremap.Core.Localization;

/// <summary>Shared app strings with an explicit English fallback.</summary>
public static class Text
{
    private static readonly ResourceManager Resources = new("ACZeroPCremap.Core.Localization.Strings", typeof(Text).Assembly);

    private static CultureInfo culture = CultureInfo.GetCultureInfo("en");
    public static event EventHandler? CultureChanged;
    public static CultureInfo Culture
    {
        get => culture;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (culture.Name == value.Name) return;
            culture = value;
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string Get(string key) => Resources.GetString(key, Culture)
        ?? throw new MissingManifestResourceException($"Missing UI resource: {key}");

    public static string Format(string key, params object?[] args) => string.Format(Culture, Get(key), args);
}
