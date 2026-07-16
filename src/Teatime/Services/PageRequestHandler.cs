using Teatime.Services.Rendering;

namespace Teatime.Services;

public sealed record PageRequestSettings(
    string BasePath,
    string? CustomCsp,
    string? AutoCustomCssUrl,
    string? AutoCustomJsUrl,
    string WebRootPath,
    string DocsRootAbsolute);

public sealed class PageRequestHandler
{
    private readonly ContentService _content;
    private readonly BlogPageResponder _responder;

    public PageRequestHandler(ContentService content, BlogPageResponder responder)
    {
        _content = content;
        _responder = responder;
    }

    public async Task HandleAsync(string? path, HttpContext context)
    {
        var normalized = (path ?? string.Empty).Trim('/').ToLowerInvariant();
        if (normalized.Length == 0
            || normalized.StartsWith("posts/", StringComparison.Ordinal)
            || normalized.StartsWith("authors/", StringComparison.Ordinal))
        {
            await _responder.Write404Async(context);
            return;
        }

        var page = await _content.GetPageAsync($"pages/{normalized}", context.RequestAborted)
                   ?? await _content.GetPageAsync(normalized, context.RequestAborted);

        if (page is null
            || page.Path.StartsWith("posts/", StringComparison.Ordinal)
            || page.Path.StartsWith("authors/", StringComparison.Ordinal))
        {
            await _responder.Write404Async(context);
            return;
        }

        if (page.Redirect is { Length: > 0 } target)
        {
            context.Response.Redirect(ResolveRedirect(target, _responder.BasePath), permanent: false);
            return;
        }

        var header = $"<header class=\"page-header\"><h1 class=\"page-title\">{Layout.LayoutProvider.HtmlEncode(page.Title)}</h1></header>";
        var cover = PostListRenderer.BuildCover(page.Cover, _responder.BasePath);
        var pageNav = await BuildPageNav(page, context.RequestAborted);

        await _responder.WriteAsync(context, new BlogPageView(
            Title: page.Title,
            ContentHtml: header + cover + page.HtmlContent + pageNav,
            Description: page.Description,
            CanonicalPath: normalized,
            IsArticle: true));
    }

    private async Task<string> BuildPageNav(Models.DocumentationPage page, CancellationToken ct)
    {
        if (!page.ShowPagination || (page.PagePrev is null && page.PageNext is null))
            return string.Empty;

        var (prevHref, prevTitle) = await ResolvePageLink(page.PagePrev, ct);
        var (nextHref, nextTitle) = await ResolvePageLink(page.PageNext, ct);
        return PostListRenderer.BuildAdjacentNav(prevHref, prevTitle, nextHref, nextTitle, "Adjacent pages");
    }

    private async ValueTask<(string? Href, string? Title)> ResolvePageLink(string? target, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target)) return (null, null);
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return (target, target);

        var norm = target.Trim('/').ToLowerInvariant();
        var targetPage = await _content.GetPageAsync($"pages/{norm}", ct) ?? await _content.GetPageAsync(norm, ct);
        return (UrlPaths.Href(_responder.BasePath, norm), targetPage?.Title ?? norm);
    }

    private static string ResolveRedirect(string target, string basePath)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return target;

        var trimmed = target.Trim('/');
        return trimmed.Length == 0
            ? (basePath.Length == 0 ? "/" : $"{basePath}/")
            : (basePath.Length == 0 ? $"/{trimmed}/" : $"{basePath}/{trimmed}/");
    }
}
