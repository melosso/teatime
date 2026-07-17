using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class FooterMenuRenderer
{
    public static string? Build(IReadOnlyList<MenuLink>? menu, string basePath)
    {
        if (menu is not { Count: > 0 }) return null;

        var sb = new StringBuilder();
        foreach (var item in menu)
        {
            if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Path)) continue;

            var external = IsExternal(item.Path!);
            var href = external ? item.Path! : UrlPaths.Href(basePath, item.Path!.Trim('/').ToLowerInvariant());
            sb.Append("<a href=\"").Append(LayoutProvider.HtmlEncode(href)).Append('"');
            if (external || item.External) sb.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
            sb.Append('>').Append(LayoutProvider.HtmlEncode(item.Title!)).Append("</a>");
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static bool IsExternal(string link) =>
        link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || link.StartsWith("//", StringComparison.Ordinal)
        || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
}
