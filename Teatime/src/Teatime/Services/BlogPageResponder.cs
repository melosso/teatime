using System.Security.Cryptography;
using System.Text;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services.Extensions;
using Teatime.Services.Layout;
using Teatime.Services.Rendering;

namespace Teatime.Services;

public sealed record BlogPageView(
    string Title,
    string ContentHtml,
    string? Description = null,
    string CanonicalPath = "",
    bool IsArticle = false,
    bool ShowComments = false,
    string? Image = null,
    DateTime? Modified = null);

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

    private static string? ResolveSocialImage(string? image, string origin, string basePath)
    {
        if (string.IsNullOrWhiteSpace(image))
            return null;
        if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return image;
        var path = image.StartsWith('/') ? image : "/" + image;
        return $"{origin}{basePath}{path}";
    }

    /// <summary>Fresh per response, so it cannot be read off the public ETag. Rules out answering 304.</summary>
    private static string NewNonce() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public async Task WriteAsync(HttpContext context, BlogPageView view)
    {
        var basePath = _settings.BasePath;
        var config = _content.SiteConfig;

        var etagInput = Encoding.UTF8.GetBytes(_content.BuildVersion + ":")
            .Concat(Encoding.UTF8.GetBytes(view.ContentHtml)).ToArray();
        var etag = Convert.ToBase64String(SHA256.HashData(etagInput)).TrimEnd('=');
        context.Response.Headers.ETag = $"\"{etag}\"";
        context.Response.Headers.CacheControl = "no-cache";

        var nonce = NewNonce();
        var extensions = _content.Extensions;
        var baseCsp = SecurityHeaders.WithExtraSources(
            _settings.CustomCsp ?? SecurityHeaders.DefaultCsp, extensions.CspSources);
        context.Response.Headers.ContentSecurityPolicy = SecurityHeaders.BuildNonceCsp(baseCsp, nonce);

        var themeCss = ThemeProvider.BuildThemeCss(_theme);
        var customCssLink = ThemeProvider.BuildCustomCssLink(_theme, _settings.AutoCustomCssUrl, basePath);
        var customJsScript = ThemeProvider.BuildCustomJsScript(_theme, _settings.AutoCustomJsUrl, basePath);
        var brandText = config?.Brand ?? config?.Title ?? ThemeProvider.GetBrandText(_theme);
        var socialLinksHtml = await SocialLinksHtmlRenderer.BuildSocialLinksHtmlAsync(config?.SocialLinks, _iconsDir, _fallbackIconsDir);
        var feedUrl = $"{context.Request.Scheme}://{context.Request.Host}{basePath}/feed.xml";
        var rssDiscoveryHtml = $"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"{LayoutProvider.HtmlEncode(config?.Brand ?? config?.Title ?? "RSS Feed")}\" href=\"{LayoutProvider.HtmlEncode(feedUrl)}\">";

        var seg = view.CanonicalPath.Trim('/');
        var siteNavHtml = SiteNavRenderer.Build(config, basePath, seg);
        var footerHtml = FooterRenderer.Build(config, basePath, brandText, socialLinksHtml, _markdown);
        var pageSegment = seg.Length == 0 ? string.Empty : $"{seg}/";
        var rawPath = $"{basePath}/{pageSegment}".TrimStart('/');
        var canonicalUrl = $"{context.Request.Scheme}://{context.Request.Host}/{rawPath}";

        var contentHtml = view.ShowComments
            ? view.ContentHtml + CommentEmbedRenderer.Build(extensions.Comments, nonce, canonicalUrl, config?.Lang)
            : view.ContentHtml;

        var metaDescription = string.IsNullOrEmpty(view.Description) ? config?.Description : view.Description;
        var origin = $"{context.Request.Scheme}://{context.Request.Host}";
        var socialImageUrl = ResolveSocialImage(view.Image ?? config?.Image ?? config?.BrandImage, origin, basePath);
        var siteName = config?.Brand ?? config?.Title;
        var locale = config?.Lang ?? "en";
        var modified = view.IsArticle ? view.Modified : null;

        var socialMetaHtml = SocialMetaRenderer.BuildSocialMeta(
            canonicalUrl, view.Title, metaDescription, !view.IsArticle, socialImageUrl, siteName, locale, modified);
        var structuredDataHtml = StructuredDataRenderer.BuildJsonLd(
            canonicalUrl, view.Title, metaDescription, !view.IsArticle, socialImageUrl, siteName, modified, nonce);

        var fullHtml = LayoutProvider.GetLayout(
            title: PageTitleRenderer.ComputeTitle(view.Title, config),
            content: contentHtml,
            themeCss: themeCss,
            brandText: brandText,
            brandImage: config?.BrandImage,
            themeMode: ThemeProvider.ResolveMode(_theme),
            enableLiveReload: _docsOptions.EnableHotReload,
            staticSearch: _docsOptions.IsStaticExport,
            buildVersion: _content.BuildVersion,
            favicon: config?.Favicon,
            description: string.IsNullOrEmpty(view.Description) ? config?.Description : view.Description,
            isHomePage: false,
            showScrollIndicator: config?.ScrollIndicator ?? ThemeProvider.ShowScrollIndicator(_theme),
            basePath: basePath,
            lang: config?.Lang ?? "en",
            headTagsHtml: HeadTagHtmlRenderer.BuildHeadTagsHtml(config?.Head)
                + ExtensionHeadRenderer.Build(extensions, nonce),
            canonicalUrl: canonicalUrl,
            nonce: nonce,
            hasMath: view.ContentHtml.Contains("class=\"katex\"", StringComparison.Ordinal),
            hasMermaid: view.ContentHtml.Contains("class=\"mermaid\"", StringComparison.Ordinal),
            hasMap: view.ContentHtml.Contains("class=\"teatime-map\"", StringComparison.Ordinal),
            hasNewsletter: view.ContentHtml.Contains("class=\"teatime-newsletter\"", StringComparison.Ordinal),
            rssDiscoveryHtml: rssDiscoveryHtml,
            isArticle: view.IsArticle,
            siteNavHtml: siteNavHtml,
            footerHtml: footerHtml,
            pageId: seg.Length == 0 ? "home" : seg.Replace('/', '-'),
            // User theme assets load last so custom.css overrides engine styles at equal specificity.
            customAssetsHtml: customCssLink + customJsScript,
            socialMetaHtml: socialMetaHtml,
            structuredDataHtml: structuredDataHtml);

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
