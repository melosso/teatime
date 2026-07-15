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

        var tocHtml = page.ShowToc && page.Headings.Count > 0
            ? TocHtmlRenderer.BuildTocHtml(page.Headings)
            : null;

        var content = $"<h1 class=\"page-title\">{Layout.LayoutProvider.HtmlEncode(page.Title)}</h1>{page.HtmlContent}";

        await _responder.WriteAsync(context, new BlogPageView(
            Title: page.Title,
            ContentHtml: content,
            Description: page.Description,
            CanonicalPath: normalized,
            TocHtml: tocHtml));
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
