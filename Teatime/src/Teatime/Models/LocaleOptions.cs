namespace Teatime.Models;

/// <summary>Locale settings for localization: date formatting and the UI string table.</summary>
public sealed class LocaleOptions
{
    /// <summary>BCP 47 culture name (e.g., "nl-NL"). Defaults to invariant format if null.</summary>
    public string? Culture { get; set; }

    /// <summary>Selects the UI string file <c>content/locale/{code}.json</c> (e.g. "nl").
    /// Falls back to <c>lang</c>, then "en". Missing or corrupt file uses the built-in English defaults.</summary>
    public string? Code { get; set; }
}