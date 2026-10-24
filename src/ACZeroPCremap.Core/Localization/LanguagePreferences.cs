using System.Globalization;
using System.Text.Json;

namespace ACZeroPCremap.Core.Localization;

public sealed record AppLanguage(string Code, string NativeName);

public sealed class LanguagePreferences(string path)
{
    public static IReadOnlyList<AppLanguage> Languages { get; } =
    [new("en", "English"), new("ru", "Русский"), new("es-ES", "Español"),
     new("pt-BR", "Português (Brasil)"), new("de", "Deutsch"), new("fr", "Français")];
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "ACZeroPCremap.settings.json");
    public string FilePath { get; } = Path.GetFullPath(path);

    public string Load()
    {
        var settings = Read();
        return Languages.Any(l => l.Code == settings.Language) ? settings.Language! : "en";
    }

    public void Save(string code)
    {
        if (!Languages.Any(l => l.Code == code)) throw new ArgumentException("Unsupported application language.", nameof(code));
        Write(Read() with { Language = code });
    }

    public WindowPlacement? LoadWindowPlacement() => Read().Window is { IsValid: true } placement ? placement : null;

    public void SaveWindowPlacement(WindowPlacement placement)
    {
        if (!placement.IsValid) throw new ArgumentException("Invalid window placement.", nameof(placement));
        Write(Read() with { Window = placement });
    }

    private Settings Read()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    private void Write(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings));
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static CultureInfo CultureFor(string code) => CultureInfo.GetCultureInfo(Languages.Any(l => l.Code == code) ? code : "en");
    private sealed record Settings(string? Language = "en", WindowPlacement? Window = null);
}
