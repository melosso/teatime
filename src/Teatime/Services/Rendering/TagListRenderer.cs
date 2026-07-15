using System.Text;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class TagListRenderer
{
    public static string BuildIndex(IReadOnlyList<TagInfo> tags, string basePath)
    {
        var sb = new StringBuilder();
        sb.Append("<h1 class=\"list-heading\">Tags</h1>");
        sb.Append("<p class=\"list-intro\">Browse writing by topic.</p>");
        sb.Append(BuildCloud(tags, basePath));
        return sb.ToString();
    }

    public static string BuildCloud(IReadOnlyList<TagInfo> tags, string basePath)
    {
        if (tags.Count == 0)
            return "<p class=\"list-empty\">No tags yet.</p>";

        var sb = new StringBuilder();
        sb.Append("<ul class=\"tag-cloud\">");
        foreach (var tag in tags)
        {
            sb.Append("<li><a class=\"tag-chip\" href=\"").Append(UrlPaths.Href(basePath, $"tags/{tag.Slug}"))
              .Append("\">").Append(LayoutProvider.HtmlEncode(tag.Name))
              .Append("<span class=\"tag-count\">").Append(tag.Count).Append("</span></a></li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }
}
