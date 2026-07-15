using System.Globalization;
using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class PostListRenderer
{
    public static string BuildList(IReadOnlyList<Post> posts, string basePath, string? heading = null, string? emptyMessage = null)
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
            AppendCard(sb, post, basePath);

        return sb.ToString();
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
        sb.Append("</div>");
        sb.Append("</header>");
        return sb.ToString();
    }

    public static string BuildCover(Post post, string basePath) =>
        post.Cover is { Length: > 0 } cover
            ? $"<img class=\"post-cover\" src=\"{LayoutProvider.HtmlEncode(Asset(basePath, cover))}\" alt=\"\" loading=\"lazy\">"
            : string.Empty;

    public static string BuildFeaturedLead(Post post, string basePath)
    {
        var href = UrlPaths.Href(basePath, post.Url);
        var cover = post.Cover is { Length: > 0 } c
            ? $"<a class=\"lead-cover\" href=\"{href}\" tabindex=\"-1\" aria-hidden=\"true\"><img src=\"{LayoutProvider.HtmlEncode(Asset(basePath, c))}\" alt=\"\" loading=\"lazy\"></a>"
            : $"<a class=\"lead-cover lead-cover-empty\" href=\"{href}\" tabindex=\"-1\" aria-hidden=\"true\"></a>";

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

    private static string Avatar(string author, string? authorImage, string basePath)
    {
        if (authorImage is { Length: > 0 } img)
            return $"<img class=\"avatar\" src=\"{LayoutProvider.HtmlEncode(Asset(basePath, img))}\" alt=\"\">";
        var initial = LayoutProvider.HtmlEncode(char.ToUpperInvariant(author.TrimStart()[0]).ToString());
        return $"<span class=\"avatar\" aria-hidden=\"true\">{initial}</span>";
    }

    private static string Asset(string basePath, string url) =>
        url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)
            ? $"{basePath}{url}"
            : url;

    private static void AppendCard(StringBuilder sb, Post post, string basePath)
    {
        var href = UrlPaths.Href(basePath, post.Url);
        sb.Append("<article class=\"post-card\">");
        sb.Append("<h2 class=\"post-card-title\"><a href=\"").Append(href).Append("\">")
          .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a></h2>");
        sb.Append("<div class=\"post-meta\">").Append(MetaLine(post, basePath)).Append("</div>");
        if (post.Excerpt is { Length: > 0 })
            sb.Append("<p class=\"post-excerpt\">").Append(LayoutProvider.HtmlEncode(post.Excerpt)).Append("</p>");
        sb.Append("</article>");
    }

    private static string MetaLine(Post post, string basePath)
    {
        var sb = new StringBuilder();
        sb.Append("<time datetime=\"").Append(post.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\">")
          .Append(post.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)).Append("</time>");
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

    public static string BuildPostNav(Post? older, Post? newer, string basePath)
    {
        if (older is null && newer is null) return string.Empty;

        var olderHtml = older is not null
            ? $"<a class=\"post-nav-older\" rel=\"prev\" href=\"{UrlPaths.Href(basePath, older.Url)}\">← {LayoutProvider.HtmlEncode(older.Title)}</a>"
            : "<span></span>";
        var newerHtml = newer is not null
            ? $"<a class=\"post-nav-newer\" rel=\"next\" href=\"{UrlPaths.Href(basePath, newer.Url)}\">{LayoutProvider.HtmlEncode(newer.Title)} →</a>"
            : "<span></span>";

        return $"<nav class=\"post-nav\" aria-label=\"Adjacent posts\">{olderHtml}{newerHtml}</nav>";
    }

    private static string PageHref(int page, string basePath) =>
        page <= 1 ? UrlPaths.Href(basePath, "") : UrlPaths.Href(basePath, $"page/{page}");
}
