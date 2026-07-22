namespace Teatime.Models;

public class Config
{
    public string? Title { get; set; }
    public string? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public string? Lang { get; set; }
    public List<HeadTag>? Head { get; set; }

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

    public List<SocialLink>? SocialLinks { get; set; }

    /// <summary>Locale settings: date culture and the UI string table. See <see cref="LocaleOptions"/>.</summary>
    public LocaleOptions? Locale { get; set; }

    /// <summary>Bookmark card rendering for standalone links. Off unless enabled. See <see cref="BookmarkOptions"/>.</summary>
    public BookmarkOptions? Bookmarks { get; set; }
}
