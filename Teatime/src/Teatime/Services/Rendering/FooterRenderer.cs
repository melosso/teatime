using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class FooterRenderer
{
    public static string Build(Config? config, string basePath, string brandText, string? socialLinksHtml, MarkdownService markdown)
    {
        var homeHref = basePath.Length == 0 ? "/" : $"{basePath}/";

        var note = config?.Footer?
            .Replace("{year}", DateTime.UtcNow.Year.ToString())
            .Replace("{author}", config.Author ?? string.Empty)
            .Replace("{title}", config.Title ?? string.Empty);
        note = !string.IsNullOrEmpty(note)
            ? markdown.ToHtml(note).Replace("<p>", "").Replace("</p>", "").Trim()
            : $"© {DateTime.UtcNow.Year} {LayoutProvider.HtmlEncode(brandText)}";

        var links = FooterMenuRenderer.Build(config?.FooterMenu, basePath)
            ?? $"<a href=\"{basePath}/feed.xml\">RSS</a>\n        <a href=\"{homeHref}archive/\">Archive</a>";

        return $@"<footer class=""site-footer"">
        <span class=""site-footer-note"">{note}</span>
        {links}
        {socialLinksHtml}
    </footer>";
    }
}
