using System.Text;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class TagListRenderer
{
    public static string BuildIndex(IReadOnlyList<TagInfo> tags, string basePath)
    {
        var sb = new StringBuilder();
        var l = Localization.Current;
        sb.Append("<h1 class=\"list-heading\">").Append(LayoutProvider.HtmlEncode(l.TagsHeading)).Append("</h1>");
        sb.Append("<p class=\"list-intro\">").Append(LayoutProvider.HtmlEncode(l.TagsIntro)).Append("</p>");
        sb.Append(BuildCloud(tags, basePath));
        return sb.ToString();
    }

    public static string BuildCloud(IReadOnlyList<TagInfo> tags, string basePath)
    {
        if (tags.Count == 0)
            return $"<p class=\"list-empty\">{LayoutProvider.HtmlEncode(Localization.Current.TagsEmpty)}</p>";

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
