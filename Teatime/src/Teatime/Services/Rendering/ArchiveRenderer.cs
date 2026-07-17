using System.Globalization;
using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class ArchiveRenderer
{
    public static string Build(IReadOnlyList<(int Year, IReadOnlyList<Post> Posts)> years, string basePath)
    {
        var sb = new StringBuilder();
        var l = Localization.Current;
        sb.Append("<h1 class=\"list-heading\">").Append(LayoutProvider.HtmlEncode(l.ArchiveHeading)).Append("</h1>");
        sb.Append("<p class=\"list-intro\">").Append(LayoutProvider.HtmlEncode(l.ArchiveIntro)).Append("</p>");
        sb.Append(BuildYears(years, basePath));
        return sb.ToString();
    }

    public static string BuildYears(IReadOnlyList<(int Year, IReadOnlyList<Post> Posts)> years, string basePath)
    {
        if (years.Count == 0)
            return $"<p class=\"list-empty\">{LayoutProvider.HtmlEncode(Localization.Current.EmptyNoPosts)}</p>";

        var sb = new StringBuilder();
        foreach (var (year, posts) in years)
        {
            sb.Append("<section class=\"archive-year\"><h2>").Append(year).Append("</h2><ul class=\"archive-list\">");
            foreach (var post in posts)
            {
                sb.Append("<li><time datetime=\"").Append(DateFormatter.Iso(post.Date)).Append("\">")
                  .Append(DateFormatter.Current.MonthDay(post.Date)).Append("</time>")
                  .Append("<a href=\"").Append(UrlPaths.Href(basePath, post.Url)).Append("\">")
                  .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a></li>");
            }
            sb.Append("</ul></section>");
        }
        return sb.ToString();
    }
}
