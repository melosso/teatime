namespace Teatime.Models;

using System.Text.Json.Serialization;

public class Config
{
    public string? Title { get; set; }
    public string? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public string? Lang { get; set; }
    public List<HeadTag>? Head { get; set; }

    public static LocaleOptions? ResolveLocale(Config? config)
    {
        if (config is null || (config.Locale is null && config.Culture is null && config.Lang is null))
            return null;

        return new LocaleOptions
        {
            Culture = config.Locale?.Culture ?? config.Culture,
            Code = config.Locale?.Code ?? config.Locale?.Lang ?? config.Lang
        };
    }

    /// <summary>Theme name, e.g. <c>"ocean"</c>. Unknown names fall back to the default theme.</summary>
    public string? Theme { get; set; }

    /// <summary>Page structure name, e.g. <c>"editorial"</c>. Orthogonal to <see cref="Theme"/>; unknown names fall back to the default structure.</summary>
    public string? Structure { get; set; }

    public string? Brand { get; set; }
    public string? BrandImage { get; set; }
    public string? Image { get; set; }
    public string? Footer { get; set; }
    public string? Favicon { get; set; }

    /// <summary>Author name shown in the post byline (single-author blog).</summary>
    public string? Author { get; set; }

    /// <summary>Optional avatar image URL for the byline; falls back to the author's initial.</summary>
    public string? AuthorImage { get; set; }

    /// <summary>Header nav items. When present, replaces the default Posts/Tags/Archive menu.</summary>
    public List<MenuLink>? Menu { get; set; }

    /// <summary>Footer links. When present, replaces the default RSS/Archive links in the footer.</summary>
    public List<MenuLink>? FooterMenu { get; set; }

    /// <summary>Top reading-progress bar. Defaults to on; set false to hide it.</summary>
    public bool? ScrollIndicator { get; set; }

    /// <summary>Cap the total posts shown across the paginated home feed. Null/0 shows all.
    /// Archive and tag pages still list every post.</summary>
    public int? HomeLimit { get; set; }

    /// <summary>Set false to disable the tag index and tag pages entirely (they return 404).</summary>
    public bool? Tags { get; set; }

    /// <summary>Set false to disable the archive page entirely (it returns 404).</summary>
    public bool? Archive { get; set; }

    /// <summary>Noindex whole surfaces site-wide, without touching front matter per page.
    /// A page's own <c>noindex</c> front matter still applies when its surface here is off.</summary>
    public NoIndexOptions? NoIndex { get; set; }

    public List<SocialLink>? SocialLinks { get; set; }

    /// <summary>Root-level date culture (e.g. "en-GB"), merged with <c>locale.culture</c>.</summary>
    public string? Culture { get; set; }

    /// <summary>Locale settings: date culture and the UI string table. Accepts the object form
    /// <c>{ "culture": "en-GB", "code": "en" }</c> or a bare code string <c>"en"</c>.</summary>
    [JsonConverter(typeof(LocaleOptionsConverter))]
    public LocaleOptions? Locale { get; set; }

    /// <summary>Bookmark card rendering for standalone links. Off unless enabled. See <see cref="BookmarkOptions"/>.</summary>
    public BookmarkOptions? Bookmarks { get; set; }

    /// <summary>Hosts a front matter <c>redirect:</c> may send readers to off-site. Same-host redirects always work.</summary>
    public List<string>? RedirectHosts { get; set; }
}

/// <summary>Per-surface noindex switches for <see cref="Config.NoIndex"/>.</summary>
public sealed class NoIndexOptions
{
    /// <summary>Noindex every post (and drop them from <c>sitemap.xml</c>).</summary>
    public bool? Posts { get; set; }

    /// <summary>Noindex every standalone page under <c>content/pages/</c> (and drop them from <c>sitemap.xml</c>).</summary>
    public bool? Pages { get; set; }

    /// <summary>Noindex the tag index and every <c>/tags/{tag}</c> page.</summary>
    public bool? Tags { get; set; }

    /// <summary>Noindex the archive page.</summary>
    public bool? Archive { get; set; }
}
