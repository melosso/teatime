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
        sb.Append("<h1 class=\"list-heading\">Archive</h1>");
        sb.Append("<p class=\"list-intro\">Every post, newest first.</p>");

        if (years.Count == 0)
        {
            sb.Append("<p class=\"list-empty\">No posts yet.</p>");
            return sb.ToString();
        }

        foreach (var (year, posts) in years)
        {
            sb.Append("<section class=\"archive-year\"><h2>").Append(year).Append("</h2><ul class=\"archive-list\">");
            foreach (var post in posts)
            {
                sb.Append("<li><time datetime=\"").Append(post.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\">")
                  .Append(post.Date.ToString("MMM d", CultureInfo.InvariantCulture)).Append("</time>")
                  .Append("<a href=\"").Append(UrlPaths.Href(basePath, post.Url)).Append("\">")
                  .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a></li>");
            }
            sb.Append("</ul></section>");
        }
        return sb.ToString();
    }
}
