using System.Globalization;
using System.Text;
using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static partial class PostListRenderer
{
    private const string ShareIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><circle cx=\"18\" cy=\"5\" r=\"3\"/><circle cx=\"6\" cy=\"12\" r=\"3\"/><circle cx=\"18\" cy=\"19\" r=\"3\"/><path d=\"M8.6 13.5l6.8 4M15.4 6.5l-6.8 4\"/></svg>";

    public static string BuildList(
        IReadOnlyList<Post> posts,
        string basePath,
        string? heading = null,
        string? emptyMessage = null,
        bool showPreview = false)
    {
        var sb = new StringBuilder();
        if (heading is { Length: > 0 })
            sb.Append("<h1 class=\"list-heading\">").Append(LayoutProvider.HtmlEncode(heading)).Append("</h1>");

        if (posts.Count == 0)
        {
            sb.Append("<p class=\"list-empty\">")
              .Append(LayoutProvider.HtmlEncode(emptyMessage ?? "No posts yet."))
              .Append("</p>");
            return sb.ToString();
        }

        foreach (var post in posts)
            AppendCard(sb, post, basePath, showPreview);

        return sb.ToString();
    }

    public static string BuildLoadMore(string? nextUrl)
    {
        if (string.IsNullOrEmpty(nextUrl)) return string.Empty;
        return $"<div class=\"load-more-wrap\"><button type=\"button\" class=\"load-more\" data-next=\"{nextUrl}\">Load more posts</button></div>";
    }

    public static string BuildPager(int currentPage, int totalPages, string basePath)
    {
        if (totalPages <= 1) return string.Empty;

        var newer = currentPage > 1
            ? $"<a class=\"pager-newer\" rel=\"prev\" href=\"{PageHref(currentPage - 1, basePath)}\">← Newer</a>"
            : "<span></span>";
        var older = currentPage < totalPages
            ? $"<a class=\"pager-older\" rel=\"next\" href=\"{PageHref(currentPage + 1, basePath)}\">Older →</a>"
            : "<span></span>";

        return $"<nav class=\"pager\" aria-label=\"Pagination\">{newer}<span class=\"pager-status\">Page {currentPage} of {totalPages}</span>{older}</nav>";
    }

    public static string BuildPostHeader(Post post, string basePath, string? author, string? authorImage, string? authorUrl)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"post-header\">");
        sb.Append("<h1 class=\"post-title\">").Append(LayoutProvider.HtmlEncode(post.Title)).Append("</h1>");
        sb.Append("<div class=\"post-meta byline\">");
        if (author is { Length: > 0 })
        {
            var inner = Avatar(author, authorImage, basePath)
                + $"<span class=\"byline-author\">{LayoutProvider.HtmlEncode(author)}</span>";
            if (authorUrl is { Length: > 0 })
                sb.Append("<a class=\"byline-link\" href=\"").Append(UrlPaths.Href(basePath, authorUrl)).Append("\">").Append(inner).Append("</a>");
            else
                sb.Append(inner);
            sb.Append(" · ");
        }
        sb.Append(MetaLine(post, basePath));
        sb.Append("<button type=\"button\" class=\"share-trigger\" data-share aria-haspopup=\"dialog\" aria-controls=\"share-overlay\">")
          .Append(ShareIcon).Append("<span>Share</span></button>");
        sb.Append("</div>");
        sb.Append("</header>");
        return sb.ToString();
    }

    public static string BuildCover(Post post, string basePath)
    {
        if (post.Cover is not { Length: > 0 } cover) return string.Empty;
        var css = post.CoverWidth is { Length: > 0 } w ? $"post-cover {w}" : "post-cover";
        return $"<img class=\"{css}\" src=\"{LayoutProvider.HtmlEncode(Asset(basePath, cover))}\" alt=\"\" loading=\"eager\" fetchpriority=\"high\" decoding=\"async\">";
    }

    public static string BuildCover(string? rawCover, string basePath)
    {
        if (string.IsNullOrWhiteSpace(rawCover)) return string.Empty;
        var url = rawCover.Trim();
        string? width = null;
        var m = CoverAttrRegex().Match(rawCover);
        if (m.Success && AllowedCoverClasses.Contains(m.Groups[2].Value))
        {
            url = m.Groups[1].Value.Trim();
            width = m.Groups[2].Value.ToLowerInvariant();
        }
        var css = width is not null ? $"post-cover {width}" : "post-cover";
        return $"<img class=\"{css}\" src=\"{LayoutProvider.HtmlEncode(Asset(basePath, url))}\" alt=\"\" loading=\"eager\" fetchpriority=\"high\" decoding=\"async\">";
    }

    private static readonly System.Collections.Generic.HashSet<string> AllowedCoverClasses =
        new(System.StringComparer.OrdinalIgnoreCase) { "natural", "plain", "wide", "full" };

    [System.Text.RegularExpressions.GeneratedRegex(@"^(.*?)\s*\{\.([a-z-]+)\}\s*$")]
    private static partial System.Text.RegularExpressions.Regex CoverAttrRegex();

    public static string BuildFeaturedLead(Post post, string basePath)
    {
        var href = UrlPaths.Href(basePath, post.Url);
        var cover = Preview(post, basePath, href, "lead-cover", "lead-cover-mark", eager: true);

        var sb = new StringBuilder();
        sb.Append("<article class=\"lead\">").Append(cover);
        sb.Append("<div class=\"lead-body\">");
        sb.Append("<h2 class=\"lead-title\"><a href=\"").Append(href).Append("\">")
          .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a></h2>");
        sb.Append("<div class=\"post-meta\">").Append(MetaLine(post, basePath)).Append("</div>");
        if (post.Excerpt is { Length: > 0 })
            sb.Append("<p class=\"lead-excerpt\">").Append(LayoutProvider.HtmlEncode(post.Excerpt)).Append("</p>");
        sb.Append("<a class=\"readmore\" href=\"").Append(href).Append("\">Keep reading <span>→</span></a>");
        sb.Append("</div></article>");
        return sb.ToString();
    }

    private static string Preview(Post post, string basePath, string href, string coverClass, string markClass, bool eager)
    {
        if (post.Cover is { Length: > 0 } c)
        {
            var loading = eager ? "loading=\"eager\" fetchpriority=\"high\"" : "loading=\"lazy\"";
            return $"<a class=\"{coverClass}\" href=\"{href}\" tabindex=\"-1\" aria-hidden=\"true\">"
                 + $"<img src=\"{LayoutProvider.HtmlEncode(Asset(basePath, c))}\" alt=\"\" {loading} decoding=\"async\"></a>";
        }

        var hue = SlugColor.HueFor(post.Slug);
        return $"<a class=\"{coverClass} slug-tint\" href=\"{href}\" tabindex=\"-1\" aria-hidden=\"true\" style=\"--slug-hue:{hue.ToString(CultureInfo.InvariantCulture)}\">"
             + $"<span class=\"{markClass}\">{Monogram(post.Title)}</span></a>";
    }

    private static string Monogram(string title) =>
        LayoutProvider.HtmlEncode(TitleMonogram.Current.Letter(title));

    private static string Avatar(string author, string? authorImage, string basePath)
    {
        if (authorImage is { Length: > 0 } img)
            return $"<img class=\"avatar\" src=\"{LayoutProvider.HtmlEncode(Asset(basePath, img))}\" alt=\"\">";
        var initial = LayoutProvider.HtmlEncode(char.ToUpperInvariant(author.TrimStart()[0]).ToString());
        return $"<span class=\"avatar\" aria-hidden=\"true\">{initial}</span>";
    }

    private static string Asset(string basePath, string url)
    {
        var resolved = url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)
            ? $"{basePath}{url}"
            : url;
        return AssetVersioning.Current.Apply(resolved);
    }

    private static void AppendCard(StringBuilder sb, Post post, string basePath, bool showPreview)
    {
        var href = UrlPaths.Href(basePath, post.Url);
        sb.Append(showPreview ? "<article class=\"post-card\">" : "<article class=\"post-card post-card-plain\">");
        sb.Append("<div class=\"post-card-body\">");
        sb.Append("<h2 class=\"post-card-title\"><a href=\"").Append(href).Append("\">")
          .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a></h2>");
        sb.Append("<div class=\"post-meta\">").Append(MetaLine(post, basePath)).Append("</div>");
        if (post.Excerpt is { Length: > 0 })
            sb.Append("<p class=\"post-excerpt\">").Append(LayoutProvider.HtmlEncode(post.Excerpt)).Append("</p>");
        sb.Append("</div>");
        if (showPreview)
            sb.Append(Preview(post, basePath, href, "card-cover", "card-cover-mark", eager: false));
        sb.Append("</article>");
    }

    private static string MetaLine(Post post, string basePath)
    {
        var sb = new StringBuilder();
        sb.Append("<time datetime=\"").Append(DateFormatter.Iso(post.Date)).Append("\">")
          .Append(DateFormatter.Current.Medium(post.Date)).Append("</time>");
        sb.Append(" · ").Append(post.ReadingMinutes).Append(" min read");
        if (post.Tags.Count > 0)
            sb.Append("<span class=\"post-tags\">").Append(BuildTagChips(post.Tags, basePath)).Append("</span>");
        return sb.ToString();
    }

    public static string BuildTagChips(IReadOnlyList<string> tags, string basePath)
    {
        var sb = new StringBuilder();
        foreach (var tag in tags)
        {
            var slug = PagePath.SlugifySegment(tag);
            if (slug.Length == 0) continue;
            sb.Append("<a class=\"tag-chip\" href=\"").Append(UrlPaths.Href(basePath, $"tags/{slug}"))
              .Append("\">").Append(LayoutProvider.HtmlEncode(tag)).Append("</a>");
        }
        return sb.ToString();
    }

    public static string BuildPostNav(Post? older, Post? newer, string basePath) =>
        BuildAdjacentNav(
            older is not null ? UrlPaths.Href(basePath, older.Url) : null, older?.Title,
            newer is not null ? UrlPaths.Href(basePath, newer.Url) : null, newer?.Title,
            "Adjacent posts");

    public static string BuildAdjacentNav(string? prevHref, string? prevTitle, string? nextHref, string? nextTitle, string ariaLabel)
    {
        if (prevHref is null && nextHref is null) return string.Empty;

        var olderHtml = prevHref is not null
            ? $"<a class=\"post-nav-link post-nav-older\" rel=\"prev\" href=\"{prevHref}\"><span class=\"post-nav-label\">← Previous</span><span class=\"post-nav-title\">{LayoutProvider.HtmlEncode(prevTitle)}</span></a>"
            : "<span></span>";
        var newerHtml = nextHref is not null
            ? $"<a class=\"post-nav-link post-nav-newer\" rel=\"next\" href=\"{nextHref}\"><span class=\"post-nav-label\">Next →</span><span class=\"post-nav-title\">{LayoutProvider.HtmlEncode(nextTitle)}</span></a>"
            : "<span></span>";

        return $"<nav class=\"post-nav\" aria-label=\"{ariaLabel}\">{olderHtml}{newerHtml}</nav>";
    }

    private static string PageHref(int page, string basePath) =>
        page <= 1 ? UrlPaths.Href(basePath, "") : UrlPaths.Href(basePath, $"page/{page}");
}
