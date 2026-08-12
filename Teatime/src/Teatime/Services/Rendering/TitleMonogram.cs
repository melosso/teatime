using System.Globalization;
using Teatime.Models;

namespace Teatime.Services.Rendering;

/// <summary>Cover-fallback letter for a title, skipping leading articles.</summary>
public sealed class TitleMonogram
{
    private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-GB");

    private static readonly Dictionary<string, string[]> ArticlesByLanguage =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ["a", "an", "the"],
            ["de"] = ["ein", "eine", "einen", "einem", "eines", "einer", "der", "die", "das", "den", "dem", "des"],
            ["fr"] = ["un", "une", "le", "la", "les"],
            ["es"] = ["un", "una", "unos", "unas", "el", "la", "los", "las"],
            ["nl"] = ["een", "de", "het"],
            ["pl"] = [],
        };

    private static readonly string[] ElidedArticles = ["l", "un", "une", "le", "la"];

    private static volatile TitleMonogram _current = From(null);

    public static TitleMonogram Current
    {
        get => _current;
        set => _current = value;
    }

    private readonly CultureInfo _culture;
    private readonly HashSet<string> _articles;
    private readonly bool _elides;

    private TitleMonogram(CultureInfo culture, HashSet<string> articles, bool elides)
    {
        _culture = culture;
        _articles = articles;
        _elides = elides;
    }

    public static TitleMonogram From(LocaleOptions? locale)
    {
        var culture = ResolveCulture(locale?.Culture);
        var language = culture.TwoLetterISOLanguageName;
        var articles = ArticlesByLanguage.TryGetValue(language, out var known)
            ? known
            : ArticlesByLanguage["en"];

        return new TitleMonogram(
            culture,
            new HashSet<string>(articles, StringComparer.OrdinalIgnoreCase),
            elides: language.Equals("fr", StringComparison.OrdinalIgnoreCase));
    }

    public string Letter(string? title)
    {
        var word = FirstMeaningfulWord(title);
        return word.Length == 0 ? "T" : _culture.TextInfo.ToUpper(word[..1]);
    }

    private string FirstMeaningfulWord(string? title)
    {
        var words = (title ?? string.Empty).Split(
            [' ', '\t', '\n', '\r', '\u00A0'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var i = 0; i < words.Length; i++)
        {
            var word = Strip(words[i]);
            if (word.Length == 0) continue;

            var last = i == words.Length - 1;
            if (!last && _articles.Contains(word)) continue;

            if (_elides && Elide(word) is { Length: > 0 } elided) return elided;

            return word;
        }

        return string.Empty;
    }

    private static string? Elide(string word)
    {
        var apostrophe = word.IndexOfAny(['\'', '’']);
        if (apostrophe <= 0 || apostrophe == word.Length - 1) return null;

        var prefix = word[..apostrophe];
        if (!ElidedArticles.Contains(prefix, StringComparer.OrdinalIgnoreCase)) return null;

        return Strip(word[(apostrophe + 1)..]);
    }

    private static string Strip(string word)
    {
        var start = 0;
        var end = word.Length;
        while (start < end && !char.IsLetterOrDigit(word[start])) start++;
        while (end > start && !char.IsLetterOrDigit(word[end - 1])) end--;
        return word[start..end];
    }

    private static CultureInfo ResolveCulture(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return DefaultCulture;
        try { return CultureInfo.GetCultureInfo(name); }
        catch (CultureNotFoundException) { return DefaultCulture; }
    }
}
