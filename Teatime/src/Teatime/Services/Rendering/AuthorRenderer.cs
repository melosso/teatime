using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class AuthorRenderer
{
    public static string BuildIndex(IReadOnlyList<Author> authors, string basePath)
    {
        var sb = new StringBuilder();
        var l = Localization.Current;
        sb.Append("<h1 class=\"list-heading\">").Append(LayoutProvider.HtmlEncode(l.AuthorsHeading)).Append("</h1>");
        sb.Append("<p class=\"list-intro\">").Append(LayoutProvider.HtmlEncode(l.AuthorsIntro)).Append("</p>");
        sb.Append(BuildGrid(authors, basePath));
        return sb.ToString();
    }

    public static string BuildGrid(IReadOnlyList<Author> authors, string basePath)
    {
        if (authors.Count == 0)
            return $"<p class=\"list-empty\">{LayoutProvider.HtmlEncode(Localization.Current.AuthorsEmpty)}</p>";

        var sb = new StringBuilder();
        sb.Append("<ul class=\"author-grid\">");
        foreach (var author in authors)
        {
            sb.Append("<li><a class=\"author-card\" href=\"").Append(UrlPaths.Href(basePath, author.Url)).Append("\">")
              .Append(Avatar(author, basePath, "author-card-avatar"))
              .Append("<span class=\"author-card-name\">").Append(LayoutProvider.HtmlEncode(author.Name)).Append("</span>")
              .Append("</a></li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }

    public static string BuildHeader(Author author, string basePath)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"author-header\">");
        sb.Append(Avatar(author, basePath, "author-header-avatar"));
        sb.Append("<h1 class=\"author-name\">").Append(LayoutProvider.HtmlEncode(author.Name)).Append("</h1>");
        if (author.BioHtml is { Length: > 0 })
            sb.Append("<div class=\"author-bio prose\">").Append(author.BioHtml).Append("</div>");
        sb.Append("</header>");
        return sb.ToString();
    }

    private static string Avatar(Author author, string basePath, string cssClass)
    {
        if (author.Image is { Length: > 0 } img)
        {
            var src = img.StartsWith('/') && !img.StartsWith("//", StringComparison.Ordinal) ? $"{basePath}{img}" : img;
            return $"<img class=\"{cssClass}\" src=\"{LayoutProvider.HtmlEncode(src)}\" alt=\"\" loading=\"lazy\">";
        }
        var initial = LayoutProvider.HtmlEncode(char.ToUpperInvariant(author.Name.TrimStart() is { Length: > 0 } n ? n[0] : '?').ToString());
        return $"<span class=\"{cssClass} avatar-initial\" aria-hidden=\"true\">{initial}</span>";
    }
}
