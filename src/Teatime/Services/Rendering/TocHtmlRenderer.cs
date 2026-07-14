using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public sealed record TocNode(HeadingInfo Heading)
{
    private readonly List<TocNode> _children = [];
    public IReadOnlyList<TocNode> Children => _children;
    public void AddChild(TocNode child) => _children.Add(child);
}

public static class TocHtmlRenderer
{
    // Excludes the page's own H1; everything else collapses onto a 3-level scale (deeper headings
    // flatten onto level 3) so the TOC never grows a fourth visual indent.
    public static string BuildTocHtml(IReadOnlyList<HeadingInfo> headings)
    {
        var items = headings.Where(h => h.Level >= 2).ToList();
        if (items.Count == 0)
        {
            // No subheadings -- fall back to a single entry linking to the page's own H1 (if any)
            // so the TOC sidebar isn't just an empty "On This Page" box.
            var titleHeading = headings.FirstOrDefault(h => h.Level == 1);
            if (titleHeading is null)
                return string.Empty;

            var html = new StringBuilder();
            AppendTocNode(html, new TocNode(titleHeading));
            return html.ToString();
        }

        var minLevel = items.Min(h => h.Level);
        var roots = BuildTocTree(items, minLevel);

        var tocHtml = new StringBuilder();
        foreach (var root in roots)
            AppendTocNode(tocHtml, root);
        return tocHtml.ToString();
    }

    public static List<TocNode> BuildTocTree(IReadOnlyList<HeadingInfo> items, int minLevel)
    {
        var roots = new List<TocNode>();
        var stack = new List<(int Depth, TocNode Node)>();

        foreach (var heading in items)
        {
            var depth = Math.Min(heading.Level - minLevel + 1, 3);
            var node = new TocNode(heading);

            while (stack.Count > 0 && stack[^1].Depth >= depth)
                stack.RemoveAt(stack.Count - 1);

            if (stack.Count == 0)
                roots.Add(node);
            else
                stack[^1].Node.AddChild(node);

            stack.Add((depth, node));
        }

        return roots;
    }

    public static void AppendTocNode(StringBuilder html, TocNode node)
    {
        html.Append("<li class=\"toc-item\">");
        html.Append($"<a href=\"#{LayoutProvider.HtmlEncode(node.Heading.Id)}\">{LayoutProvider.HtmlEncode(node.Heading.Text)}</a>");

        if (node.Children.Count > 0)
        {
            html.Append("<ul class=\"toc-sublist\">");
            foreach (var child in node.Children)
                AppendTocNode(html, child);
            html.Append("</ul>");
        }

        html.Append("</li>");
    }
}
