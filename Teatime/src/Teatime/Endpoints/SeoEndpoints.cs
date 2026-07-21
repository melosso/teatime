using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Rendering;

namespace Teatime.Endpoints;

internal static class SeoEndpoints
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", GetRobots);
        app.MapGet("/llms.txt", GetLlms);
        app.MapGet("/sitemap.xml", GetSitemap);
        app.MapGet("/feed.xml", GetFeed).RequireRateLimiting(RateLimitPolicies.Search);
        return app;
    }

    internal static ContentHttpResult GetRobots(HttpContext context, PageRequestSettings settings)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var body = $"User-agent: *\nAllow: /\nSitemap: {baseUrl}{settings.BasePath}/sitemap.xml\n";
        return TypedResults.Text(body, "text/plain", Encoding.UTF8);
    }

    internal static async Task<ContentHttpResult> GetLlms(PostService posts, ContentService content, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var config = content.SiteConfig;
        var view = await posts.GetViewAsync(context.RequestAborted);

        var sb = new StringBuilder();
        sb.AppendLine($"# {config?.Brand ?? config?.Title ?? "Teatime"}");
        sb.AppendLine();
        foreach (var post in view.Posts)
        {
            var line = $"- [{post.Title}]({baseUrl}{basePath}/{post.Url}/)";
            if (post.Description is { Length: > 0 } d) line += $": {d}";
            else if (post.Excerpt is { Length: > 0 } e) line += $": {e}";
            sb.AppendLine(line);
        }

        return TypedResults.Text(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    internal static async Task<ContentHttpResult> GetSitemap(PostService posts, ContentService content, AuthorService authorService, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var config = content.SiteConfig;
        var view = await posts.GetViewAsync(context.RequestAborted);
        var pages = await content.GetAllPagesAsync(context.RequestAborted);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, "")}</loc><priority>1.0</priority></url>");

        foreach (var post in view.Posts.Where(p => p.InSitemap))
        {
            var lastMod = (post.Updated ?? post.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, post.Url)}</loc><lastmod>{lastMod}</lastmod><priority>0.8</priority></url>");
        }

        foreach (var page in pages.Where(p => p.InSitemap && p.Path.StartsWith("pages/", StringComparison.Ordinal)))
        {
            var slug = page.Path["pages/".Length..];
            if (slug.Length == 0) continue;
            var lastMod = (page.LastModified ?? DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, slug)}</loc><lastmod>{lastMod}</lastmod><priority>0.5</priority></url>");
        }

        var authors = authorService.GetListed();
        if (authors.Count > 0)
        {
            sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, ReservedRoutes.Authors.Slug)}</loc><priority>0.3</priority></url>");
            foreach (var author in authors)
                sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, author.Url)}</loc><priority>0.3</priority></url>");
        }

        foreach (var route in new[] { ReservedRoutes.Tags, ReservedRoutes.Archive }.Where(r => r.IsEnabled(config)))
            sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, route.Slug)}</loc><priority>0.3</priority></url>");
        sb.AppendLine("</urlset>");
        return TypedResults.Text(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    internal static async Task<ContentHttpResult> GetFeed(PostService posts, ContentService content, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var config = content.SiteConfig;
        var view = await posts.GetViewAsync(context.RequestAborted);

        var feedTitle = WebUtility.HtmlEncode(config?.Brand ?? config?.Title ?? "Teatime");
        var feedDesc = WebUtility.HtmlEncode(config?.Description ?? feedTitle);
        var feedLink = $"{baseUrl}{basePath}/";
        var feedLang = WebUtility.HtmlEncode(config?.Lang ?? "en");

        var recent = view.Posts.Take(20).ToList();
        var lastBuildDate = recent.Count > 0
            ? AsUtc(recent[0].Date).ToString("R", CultureInfo.InvariantCulture)
            : DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
        sb.AppendLine("  <channel>");
        sb.AppendLine($"    <title>{feedTitle}</title>");
        sb.AppendLine($"    <link>{feedLink}</link>");
        sb.AppendLine($"    <description>{feedDesc}</description>");
        sb.AppendLine($"    <language>{feedLang}</language>");
        sb.AppendLine($"    <lastBuildDate>{lastBuildDate}</lastBuildDate>");
        sb.AppendLine($"    <atom:link href=\"{WebUtility.HtmlEncode($"{baseUrl}{basePath}/feed.xml")}\" rel=\"self\" type=\"application/rss+xml\"/>");

        foreach (var post in recent)
        {
            var postUrl = $"{baseUrl}{basePath}/{post.Url}/";
            var title = WebUtility.HtmlEncode(post.Title);
            var description = WebUtility.HtmlEncode(post.Description is { Length: > 0 } d ? d : post.Excerpt);

            sb.AppendLine("    <item>");
            sb.AppendLine($"      <title>{title}</title>");
            sb.AppendLine($"      <link>{WebUtility.HtmlEncode(postUrl)}</link>");
            sb.AppendLine($"      <guid isPermaLink=\"true\">{WebUtility.HtmlEncode(postUrl)}</guid>");
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine($"      <description>{description}</description>");
            foreach (var tag in post.Tags)
                sb.AppendLine($"      <category>{WebUtility.HtmlEncode(tag)}</category>");
            sb.AppendLine($"      <pubDate>{AsUtc(post.Date).ToString("R", CultureInfo.InvariantCulture)}</pubDate>");
            sb.AppendLine("    </item>");
        }

        sb.AppendLine("  </channel>");
        sb.AppendLine("</rss>");
        return TypedResults.Text(sb.ToString(), "application/rss+xml", Encoding.UTF8);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
}
