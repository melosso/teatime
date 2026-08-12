using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

/// <summary>Renders BookmarkCacheEntry as a card.</summary>
public static class BookmarkCardRenderer
{
    public static string Render(BookmarkCacheEntry entry, string basePath)
    {
        var sb = new StringBuilder();
        sb.Append("<figure class=\"kg-bookmark-card\">")
          .Append("<a class=\"kg-bookmark-container\" href=\"").Append(LayoutProvider.HtmlEncode(entry.Url))
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");

        sb.Append("<div class=\"kg-bookmark-content\">");
        sb.Append("<div class=\"kg-bookmark-title\">").Append(LayoutProvider.HtmlEncode(entry.Title)).Append("</div>");

        if (entry.Description is { Length: > 0 } description)
            sb.Append("<div class=\"kg-bookmark-description\">").Append(LayoutProvider.HtmlEncode(description)).Append("</div>");

        sb.Append("<div class=\"kg-bookmark-metadata\">");
        sb.Append(Icon(entry, basePath));
        if (entry.Author is { Length: > 0 } author)
            sb.Append("<span class=\"kg-bookmark-author\">").Append(LayoutProvider.HtmlEncode(author)).Append("</span>");
        sb.Append("<span class=\"kg-bookmark-publisher\">").Append(LayoutProvider.HtmlEncode(entry.Publisher)).Append("</span>");
        sb.Append("</div>");
        sb.Append("</div>");

        if (entry.ThumbnailPath is { Length: > 0 } thumbnail)
            sb.Append("<div class=\"kg-bookmark-thumbnail\"><img src=\"").Append(LayoutProvider.HtmlEncode(Prefix(thumbnail, basePath)))
              .Append("\" alt=\"\" loading=\"lazy\" decoding=\"async\"></div>");

        sb.Append("</a></figure>");
        return sb.ToString();
    }

    private static string Icon(BookmarkCacheEntry entry, string basePath)
    {
        if (entry.IconPath is { Length: > 0 } icon)
            return $"<img class=\"kg-bookmark-icon\" src=\"{LayoutProvider.HtmlEncode(Prefix(icon, basePath))}\" alt=\"\" loading=\"lazy\" decoding=\"async\">";

        var initial = entry.Publisher.TrimStart() is { Length: > 0 } p ? char.ToUpperInvariant(p[0]) : '?';
        return $"<span class=\"kg-bookmark-icon kg-bookmark-icon-fallback\" aria-hidden=\"true\">{LayoutProvider.HtmlEncode(initial.ToString())}</span>";
    }

    private static string Prefix(string path, string basePath) =>
        path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) ? $"{basePath}{path}" : path;
}
