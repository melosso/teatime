namespace Teatime.Models;

public sealed record FrontMatter
{
    public string? Title { get; init; }
    public string? Description { get; init; }

    public string? Layout { get; init; }

    public List<string>? Keywords { get; init; }

    /// <summary>Per-page override for <c>Config.LastUpdated</c>. <c>false</c> hides the
    /// "Last updated" stamp on this page even when the site-wide setting is on.</summary>
    public bool? LastUpdated { get; init; }

    /// <summary>Set to <c>false</c> to hide prev/next pagination links on this page.</summary>
    public bool? Pagination { get; init; }

    /// <summary>Set to <c>false</c> to hide the table of contents on this page.</summary>
    public bool? Toc { get; init; }

    /// <summary>When set, the page issues a 307 redirect to this URL instead of rendering.
    /// Root-relative paths (starting with <c>/</c>) are prefixed with the configured base path.
    /// Absolute URLs are used as-is.</summary>
    public string? Redirect { get; init; }

    /// <summary>Content creation date (ISO 8601). Used as the "Last updated" display value
    /// when <see cref="Updated"/> is absent. Overrides file system mtime.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Last-modified date (ISO 8601). Takes priority over <see cref="Date"/> and
    /// file system mtime for the "Last updated" display.</summary>
    public DateTime? Updated { get; init; }

    /// <summary>Blog tags for a post. Drives <c>/tags</c> and <c>/tags/{tag}</c> listings.</summary>
    public List<string>? Tags { get; init; }

    /// <summary>When <c>true</c>, the post is excluded from listings, feeds, and the sitemap
    /// outside the Development environment.</summary>
    public bool? Draft { get; init; }

    /// <summary>Overrides the URL slug (last path segment). Defaults to the file name.</summary>
    public string? Slug { get; init; }

    /// <summary>Excerpt shown on index/list cards. Falls back to the first paragraph when absent.</summary>
    public string? Summary { get; init; }

    /// <summary>Feature image URL for the post (byline area + home lead card).</summary>
    public string? Cover { get; init; }

    /// <summary>Author id referencing a file in <c>content/authors/</c>. Falls back to the site author.</summary>
    public string? Author { get; init; }
}
