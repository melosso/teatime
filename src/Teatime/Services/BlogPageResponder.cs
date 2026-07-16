using System.Security.Cryptography;
using System.Text;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services.Layout;
using Teatime.Services.Rendering;

namespace Teatime.Services;

public sealed record BlogPageView(
    string Title,
    string ContentHtml,
    string? Description = null,
    string CanonicalPath = "",
    bool IsArticle = false);

public sealed class BlogPageResponder
{
    private readonly ContentService _content;
    private readonly MarkdownService _markdown;
    private readonly ThemeOptions _theme;
    private readonly DocsOptions _docsOptions;
    private readonly PageRequestSettings _settings;
    private readonly string _iconsDir;
    private readonly string? _fallbackIconsDir;

    public BlogPageResponder(
        ContentService content,
        MarkdownService markdown,
        ThemeOptions theme,
        DocsOptions docsOptions,
        PageRequestSettings settings)
    {
        _content = content;
        _markdown = markdown;
        _theme = theme;
        _docsOptions = docsOptions;
        _settings = settings;
        _iconsDir = Path.Combine(settings.WebRootPath, "icons");
        var defaultIconsDir = Path.Combine(AppContext.BaseDirectory, "wwwroot-default", "icons");
        _fallbackIconsDir = Directory.Exists(defaultIconsDir) ? defaultIconsDir : null;
    }

    public string BasePath => _settings.BasePath;
    public string HomeUrl => _settings.BasePath.Length == 0 ? "/" : $"{_settings.BasePath}/";

    private static string NonceFromETag(string etag) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(etag)), 0, 16);

    public async Task WriteAsync(HttpContext context, BlogPageView view)
    {
        var basePath = _settings.BasePath;
        var config = _content.SiteConfig;

        var etagInput = Encoding.UTF8.GetBytes(_content.BuildVersion + ":")
            .Concat(Encoding.UTF8.GetBytes(view.ContentHtml)).ToArray();
        var etag = Convert.ToBase64String(SHA256.HashData(etagInput)).TrimEnd('=');
        context.Response.Headers.ETag = $"\"{etag}\"";
        context.Response.Headers.CacheControl = "no-cache";

        var nonce = NonceFromETag(etag);
        context.Response.Headers.ContentSecurityPolicy =
            SecurityHeaders.BuildNonceCsp(_settings.CustomCsp ?? SecurityHeaders.DefaultCsp, nonce);

        if (context.Request.Headers.IfNoneMatch.ToString() == $"\"{etag}\"")
        {
            context.Response.StatusCode = 304;
            return;
        }

        var themeCss = ThemeProvider.BuildThemeCss(_theme);
        var customCssLink = ThemeProvider.BuildCustomCssLink(_theme, _settings.AutoCustomCssUrl, basePath);
        var customJsScript = ThemeProvider.BuildCustomJsScript(_theme, _settings.AutoCustomJsUrl, basePath);
        var brandText = config?.Brand ?? config?.Title ?? ThemeProvider.GetBrandText(_theme);
        var socialLinksHtml = await SocialLinksHtmlRenderer.BuildSocialLinksHtmlAsync(config?.SocialLinks, _iconsDir, _fallbackIconsDir);
        var feedUrl = $"{context.Request.Scheme}://{context.Request.Host}{basePath}/feed.xml";
        var rssDiscoveryHtml = $"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"{LayoutProvider.HtmlEncode(config?.Brand ?? config?.Title ?? "RSS Feed")}\" href=\"{LayoutProvider.HtmlEncode(feedUrl)}\">";

        var seg = view.CanonicalPath.Trim('/');
        var siteNavHtml = SiteNavRenderer.Build(config, basePath, seg);
        var footerLinksHtml = FooterMenuRenderer.Build(config?.FooterMenu, basePath);
        var pageSegment = seg.Length == 0 ? string.Empty : $"{seg}/";
        var rawPath = $"{basePath}/{pageSegment}".TrimStart('/');
        var canonicalUrl = $"{context.Request.Scheme}://{context.Request.Host}/{rawPath}";

        var footerText = config?.Footer?
            .Replace("{year}", DateTime.UtcNow.Year.ToString())
            .Replace("{author}", config.Author ?? string.Empty)
            .Replace("{title}", config.Title ?? string.Empty);
        if (!string.IsNullOrEmpty(footerText))
            footerText = _markdown.ToHtml(footerText).Replace("<p>", "").Replace("</p>", "").Trim();

        var fullHtml = LayoutProvider.GetLayout(
            title: PageTitleRenderer.ComputeTitle(view.Title, config),
            content: view.ContentHtml,
            navigationHtml: string.Empty,
            breadcrumbHtml: string.Empty,
            paginationHtml: string.Empty,
            themeCss: themeCss + customCssLink + customJsScript,
            brandText: brandText,
            brandImage: config?.BrandImage,
            enableDarkMode: ThemeProvider.UseDarkMode(_theme),
            footerText: footerText,
            socialLinksHtml: socialLinksHtml,
            enableLiveReload: _docsOptions.EnableHotReload,
            staticSearch: _docsOptions.IsStaticExport,
            buildVersion: _content.BuildVersion,
            favicon: config?.Favicon,
            description: string.IsNullOrEmpty(view.Description) ? config?.Description : view.Description,
            mobileTopNavHtml: string.Empty,
            isHomePage: false,
            showScrollIndicator: config?.ScrollIndicator ?? ThemeProvider.ShowScrollIndicator(_theme),
            basePath: basePath,
            lang: config?.Lang ?? "en",
            headTagsHtml: HeadTagHtmlRenderer.BuildHeadTagsHtml(config?.Head),
            canonicalUrl: canonicalUrl,
            nonce: nonce,
            hasMath: view.ContentHtml.Contains("class=\"katex\"", StringComparison.Ordinal),
            hasMermaid: view.ContentHtml.Contains("class=\"mermaid\"", StringComparison.Ordinal),
            rssDiscoveryHtml: rssDiscoveryHtml,
            isArticle: view.IsArticle,
            siteNavHtml: siteNavHtml,
            footerLinksHtml: footerLinksHtml);

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(fullHtml);
    }

    public Task Write404Async(HttpContext context)
    {
        var config = _content.SiteConfig;
        context.Response.StatusCode = 404;
        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(
            LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode, _settings.BasePath, config?.Lang ?? "en"));
    }
}
