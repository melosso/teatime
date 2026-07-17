using System.Globalization;
using System.Text;
using System.Text.Json;
using Teatime.Models;

namespace Teatime.Services.Rendering;

/// <summary>Server-side UI string table. English defaults are the floor; content/locale/{code}.json
/// overrides them per key. Swapped atomically on content reload. Never served to clients.</summary>
public sealed class Localization
{
    private static readonly JsonSerializerOptions LocaleJsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = false
    };

    private static readonly Dictionary<string, string> Defaults = new(StringComparer.Ordinal)
    {
        ["readMore"] = "Keep reading",
        ["loadMore"] = "Load more posts",
        ["pagerNewer"] = "Newer",
        ["pagerOlder"] = "Older",
        ["pagerStatus"] = "Page {0} of {1}",
        ["pagerAria"] = "Pagination",
        ["postNavPrevious"] = "Previous",
        ["postNavNext"] = "Next",
        ["postNavAria"] = "Adjacent posts",
        ["pageNavAria"] = "Adjacent pages",
        ["minRead"] = "min read",
        ["share"] = "Share",
        ["shareTitle"] = "Share this post",
        ["shareCopy"] = "Copy link",
        ["shareCopied"] = "Copied",
        ["emptyNoPosts"] = "No posts yet.",
        ["homeEmpty"] = "No posts yet. Drop a Markdown file in content/posts/.",
        ["authorEmpty"] = "No posts here yet.",
        ["homeHeadingLatest"] = "Latest posts",
        ["homeHeadingPaged"] = "Posts, page {0}",
        ["pageTitlePaged"] = "Page {0}",
        ["taggedHeading"] = "Tagged “{0}”",
        ["taggedHeadingPaged"] = "Tagged “{0}”, page {1}",
        ["taggedTitle"] = "Tagged {0}",
        ["taggedTitlePaged"] = "Tagged {0}, page {1}",
        ["authorPagedTitle"] = "{0}, page {1}",
        ["tagsHeading"] = "Tags",
        ["tagsIntro"] = "Browse writing by topic.",
        ["tagsEmpty"] = "No tags yet.",
        ["archiveHeading"] = "Archive",
        ["archiveIntro"] = "Every post, newest first.",
        ["authorsHeading"] = "Authors",
        ["authorsIntro"] = "The people behind the writing.",
        ["authorsEmpty"] = "No authors yet.",
        ["lastUpdated"] = "Last updated on",
        ["skipToContent"] = "Skip to content",
        ["searchAria"] = "Search",
        ["searchHeading"] = "Search posts, tags and authors",
        ["searchPlaceholder"] = "Search posts, tags and authors...",
        ["searchResultsAria"] = "Search results",
        ["searchClose"] = "Close search",
        ["searchNavigate"] = "Navigate",
        ["searchSelect"] = "Select",
        ["searchEsc"] = "Close",
        ["searchSearching"] = "Searching",
        ["searchNoResults"] = "No results found.",
        ["searchError"] = "Something went wrong. Try again.",
        ["searchFailed"] = "Search failed.",
        ["searchGroupAuthors"] = "Authors",
        ["searchGroupTags"] = "Tags",
        ["searchGroupPosts"] = "Posts",
        ["searchResultSingular"] = "result found.",
        ["searchResultPlural"] = "results found.",
        ["rssFeed"] = "RSS feed",
        ["themeToggle"] = "Toggle dark mode",
        ["notFoundTitle"] = "Page Not Found",
        ["notFoundMessage"] = "The page you're looking for doesn't exist.",
        ["notFoundHome"] = "Return home",
    };

    private readonly IReadOnlyDictionary<string, string> _map;

    private Localization(IReadOnlyDictionary<string, string> map) => _map = map;

    public static Localization Default { get; } = new(Defaults);

    private static volatile Localization _current = Default;

    public static Localization Current
    {
        get => _current;
        set => _current = value;
    }

    private string this[string key] =>
        _map.TryGetValue(key, out var v) ? v
        : Defaults.TryGetValue(key, out var d) ? d
        : key;

    private string Format(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, this[key], args);

    public string ReadMore => this["readMore"];
    public string LoadMore => this["loadMore"];
    public string PagerNewer => this["pagerNewer"];
    public string PagerOlder => this["pagerOlder"];
    public string PagerStatus(int current, int total) => Format("pagerStatus", current, total);
    public string PagerAria => this["pagerAria"];
    public string PostNavPrevious => this["postNavPrevious"];
    public string PostNavNext => this["postNavNext"];
    public string PostNavAria => this["postNavAria"];
    public string PageNavAria => this["pageNavAria"];
    public string MinRead => this["minRead"];
    public string Share => this["share"];
    public string ShareTitle => this["shareTitle"];
    public string ShareCopy => this["shareCopy"];
    public string ShareCopied => this["shareCopied"];
    public string EmptyNoPosts => this["emptyNoPosts"];
    public string HomeEmpty => this["homeEmpty"];
    public string AuthorEmpty => this["authorEmpty"];
    public string HomeHeadingLatest => this["homeHeadingLatest"];
    public string HomeHeadingPaged(int page) => Format("homeHeadingPaged", page);
    public string PageTitlePaged(int page) => Format("pageTitlePaged", page);
    public string TaggedHeading(string tag) => Format("taggedHeading", tag);
    public string TaggedHeadingPaged(string tag, int page) => Format("taggedHeadingPaged", tag, page);
    public string TaggedTitle(string tag) => Format("taggedTitle", tag);
    public string TaggedTitlePaged(string tag, int page) => Format("taggedTitlePaged", tag, page);
    public string AuthorPagedTitle(string name, int page) => Format("authorPagedTitle", name, page);
    public string TagsHeading => this["tagsHeading"];
    public string TagsIntro => this["tagsIntro"];
    public string TagsEmpty => this["tagsEmpty"];
    public string ArchiveHeading => this["archiveHeading"];
    public string ArchiveIntro => this["archiveIntro"];
    public string AuthorsHeading => this["authorsHeading"];
    public string AuthorsIntro => this["authorsIntro"];
    public string AuthorsEmpty => this["authorsEmpty"];
    public string LastUpdated => this["lastUpdated"];
    public string SkipToContent => this["skipToContent"];
    public string SearchAria => this["searchAria"];
    public string SearchHeading => this["searchHeading"];
    public string SearchPlaceholder => this["searchPlaceholder"];
    public string SearchResultsAria => this["searchResultsAria"];
    public string SearchClose => this["searchClose"];
    public string SearchNavigate => this["searchNavigate"];
    public string SearchSelect => this["searchSelect"];
    public string SearchEsc => this["searchEsc"];
    public string SearchSearching => this["searchSearching"];
    public string SearchNoResults => this["searchNoResults"];
    public string SearchError => this["searchError"];
    public string SearchFailed => this["searchFailed"];
    public string SearchGroupAuthors => this["searchGroupAuthors"];
    public string SearchGroupTags => this["searchGroupTags"];
    public string SearchGroupPosts => this["searchGroupPosts"];
    public string SearchResultSingular => this["searchResultSingular"];
    public string SearchResultPlural => this["searchResultPlural"];
    public string RssFeed => this["rssFeed"];
    public string ThemeToggle => this["themeToggle"];
    public string NotFoundTitle => this["notFoundTitle"];
    public string NotFoundMessage => this["notFoundMessage"];
    public string NotFoundHome => this["notFoundHome"];

    // Overlays content/locale/{code}.json on the defaults. Missing file: silent. Corrupt/unknown keys: warn.
    public static Localization From(string docsPath, Config? config, ILogger logger)
    {
        var code = ResolveCode(config);
        var path = Path.Combine(docsPath, "locale", $"{code}.json");
        if (!File.Exists(path))
            return Default;

        var filename = Path.GetFileName(path);

        Dictionary<string, string?>? raw;
        try
        {
            var json = File.ReadAllText(path);
            raw = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, LocaleJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Locale file {Filename} is invalid. Falling back to default strings. Reason: {Message}", filename, ex.Message);
            return Default;
        }

        if (raw is null || raw.Count == 0)
            return Default;

        var map = new Dictionary<string, string>(Defaults, StringComparer.Ordinal);
        var deadKeys = new List<string>();
        foreach (var (key, value) in raw)
        {
            if (!Defaults.ContainsKey(key))
            {
                deadKeys.Add(key);
                continue;
            }
            if (!string.IsNullOrEmpty(value))
                map[key] = value;
        }

        if (deadKeys.Count > 0)
            logger.LogWarning("Locale file {Filename} has unknown keys (no such string, ignored): {Keys}",
                filename, string.Join(", ", deadKeys.Order()));

        return new Localization(map);
    }

    private static string ResolveCode(Config? config)
    {
        var raw = config?.Locale?.Code ?? config?.Lang ?? "en";
        return IsValidCode(raw) ? raw.ToLowerInvariant() : "en";
    }

    // Guard the filename: locale codes are short tokens, never paths.
    private static bool IsValidCode(string s)
    {
        if (s.Length is < 2 or > 12) return false;
        foreach (var c in s)
            if (!char.IsAsciiLetterOrDigit(c) && c != '-') return false;
        return true;
    }

    // Escape for inlining inside a quoted JS string literal.
    public static string JsEncode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '<': sb.Append("\\u003C"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
