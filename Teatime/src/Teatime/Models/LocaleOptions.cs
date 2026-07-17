namespace Teatime.Models;

/// <summary>Locale settings for localization, currently used for date formatting.</summary>
public sealed class LocaleOptions
{
    /// <summary>BCP 47 culture name (e.g., "nl-NL"). Defaults to invariant format if null.</summary>
    public string? Culture { get; set; }
}