using Teatime.Services.Rendering;

namespace Teatime.Services;

public sealed record PageRequestSettings(
    string BasePath,
    string? CustomCsp,
    string ThemeDir,
    string WebRootPath,
    string DocsRootAbsolute,
    string? PublicBaseUrl,
    string? CliTheme = null,
    string? CliStructure = null)
{
    /// <summary>Blank means absent. An empty setting must not count as "configured" and mask a later source.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');

    /// <summary>Precedence: <c>--base-url</c>, then <c>Docs:PublicBaseUrl</c>, then the bare <c>PublicBaseUrl</c> alias.</summary>
    public static string? ResolvePublicBaseUrl(string? cliBaseUrl, string? docsOption, string? alias) =>
        Normalize(cliBaseUrl) ?? Normalize(docsOption) ?? Normalize(alias);

    /// <summary>
    /// Absolute origin for canonical URLs, feeds and sitemaps; <c>PublicBaseUrl</c> wins, as the Host header is caller-supplied.
    /// </summary>
    public string Origin(HttpContext context) =>
        Normalize(PublicBaseUrl) ?? $"{context.Request.Scheme}://{context.Request.Host}";
}

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
        if (normalized.Length == 0 || Models.ReservedRoutes.IsContentPrefixed(normalized))
        {
            await _responder.Write404Async(context);
            return;
        }

        var page = await _content.GetPageAsync($"pages/{normalized}", context.RequestAborted)
                   ?? await _content.GetPageAsync(normalized, context.RequestAborted);

        if (page is null || Models.ReservedRoutes.IsContentPrefixed(page.Path))
        {
            await _responder.Write404Async(context);
            return;
        }

        if (page.Redirect is { Length: > 0 } target
            && TryResolveRedirect(target, _responder.BasePath, context, _content.SiteConfig, out var resolved))
        {
            context.Response.Redirect(resolved, permanent: false);
            return;
        }

        var header = $"<header class=\"page-header\"><h1 class=\"page-title\">{Layout.LayoutProvider.HtmlEncode(page.Title)}</h1></header>";
        var cover = PostListRenderer.BuildCover(page.Cover, _responder.BasePath);
        var updated = BuildUpdatedStamp(page);
        var pageNav = await BuildPageNav(page, context.RequestAborted);

        await _responder.WriteAsync(context, new BlogPageView(
            Title: page.Title,
            ContentHtml: header + cover + page.HtmlContent + updated + pageNav,
            Description: page.Description,
            CanonicalPath: normalized,
            IsArticle: true,
            Image: page.Cover,
            Modified: page.Updated ?? page.LastModified,
            NoIndex: page.NoIndex || (_content.SiteConfig?.NoIndex?.Pages ?? false)));
    }

    // Opt-in: only pages with an explicit updated:/date: front matter show the stamp (never file mtime).
    private static string BuildUpdatedStamp(Models.DocumentationPage page)
    {
        if (!page.ShowLastUpdated || page.Updated is not { } when)
            return string.Empty;
        var human = DateFormatter.Current.Medium(when);
        var label = Layout.LayoutProvider.HtmlEncode(Localization.Current.LastUpdated);
        return $"<p class=\"page-updated\">{label} <time datetime=\"{DateFormatter.Iso(when)}\">{human}</time></p>";
    }

    private async Task<string> BuildPageNav(Models.DocumentationPage page, CancellationToken ct)
    {
        if (!page.ShowPagination || (page.PagePrev is null && page.PageNext is null))
            return string.Empty;

        var (prevHref, prevTitle) = await ResolvePageLink(page.PagePrev, ct);
        var (nextHref, nextTitle) = await ResolvePageLink(page.PageNext, ct);
        return PostListRenderer.BuildAdjacentNav(prevHref, prevTitle, nextHref, nextTitle, Localization.Current.PageNavAria);
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

    /// <summary>False when an absolute target's host is neither this site's nor listed in <c>config.json</c>'s <c>redirectHosts</c>; the caller then renders the page instead of forwarding.</summary>
    internal static bool TryResolveRedirect(
        string target, string basePath, HttpContext context, Models.Config? config, out string resolved)
    {
        resolved = string.Empty;
        var isAbsolute = target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isAbsolute && !IsAllowedRedirectHost(target, context.Request.Host.Host, config?.RedirectHosts))
        {
            Serilog.Log.Warning(
                "Redirect to {Target} is not allowed: its host is not listed in config.json redirectHosts; the page rendered instead",
                target);
            return false;
        }

        resolved = ResolveRedirect(target, basePath);
        return true;
    }

    internal static bool IsAllowedRedirectHost(string target, string requestHost, IReadOnlyList<string>? allowedHosts)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            return false;

        if (uri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase))
            return true;

        return allowedHosts is { Count: > 0 }
            && allowedHosts.Any(h => uri.Host.Equals(h.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    internal static string ResolveRedirect(string target, string basePath)
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
