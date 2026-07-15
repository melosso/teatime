using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Serialization;

namespace Teatime.Services;

public static class StaticSiteExporter
{
    public static async Task RunAsync(WebApplication app, string outputDir, string? baseUrl, CancellationToken cancellationToken)
    {
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var docs = app.Services.GetRequiredService<ContentService>();
        var postService = app.Services.GetRequiredService<PostService>();
        var options = app.Services.GetRequiredService<DocsOptions>();
        var pages = await docs.GetAllPagesAsync(cancellationToken);
        var view = await postService.GetViewAsync(cancellationToken);

        Directory.CreateDirectory(outputDir);

        var originPrefix = address.TrimEnd('/');
        var publicPrefix = string.IsNullOrEmpty(baseUrl) ? null : baseUrl.TrimEnd('/');

        // (requestPath, output-relative directory). Empty dir => outputDir/index.html
        var routes = new List<(string Request, string Dir)> { ("/", "") };

        var totalPages = Math.Max(1, (int)Math.Ceiling(view.Posts.Count / (double)options.PageSize));
        for (var n = 2; n <= totalPages; n++)
            routes.Add(($"/page/{n}", $"page/{n}"));

        foreach (var post in view.Posts)
            routes.Add(($"/{post.Url}", post.Url));

        foreach (var page in pages.Where(p => p.Path.StartsWith("pages/", StringComparison.Ordinal)))
        {
            var slug = page.Path["pages/".Length..];
            if (slug.Length > 0) routes.Add(($"/{slug}", slug));
        }

        var config = docs.SiteConfig;
        if (config?.Tags != false)
        {
            routes.Add(("/tags", "tags"));
            foreach (var tag in view.Tags)
            {
                routes.Add(($"/tags/{tag.Slug}", $"tags/{tag.Slug}"));
                var tagPages = Math.Max(1, (int)Math.Ceiling(tag.Count / (double)options.PageSize));
                for (var n = 2; n <= tagPages; n++)
                    routes.Add(($"/tags/{tag.Slug}/page/{n}", $"tags/{tag.Slug}/page/{n}"));
            }
        }
        if (config?.Archive != false)
            routes.Add(("/archive", "archive"));

        var authorService = app.Services.GetRequiredService<AuthorService>();
        var authors = authorService.GetAll();
        if (authors.Count > 0)
        {
            routes.Add(("/authors", "authors"));
            foreach (var author in authors)
            {
                routes.Add(($"/{author.Url}", author.Url));
                var authorPosts = await postService.GetByAuthorAsync(author.Id, cancellationToken);
                var authorPages = Math.Max(1, (int)Math.Ceiling(authorPosts.Count / (double)options.PageSize));
                for (var n = 2; n <= authorPages; n++)
                    routes.Add(($"/{author.Url}/page/{n}", $"{author.Url}/page/{n}"));
            }
        }

        foreach (var (request, dir) in routes)
        {
            var html = await client.GetStringAsync(request, cancellationToken);
            if (publicPrefix is not null)
                html = html.Replace(originPrefix, publicPrefix);
            var targetFile = dir.Length == 0
                ? Path.Combine(outputDir, "index.html")
                : Path.Combine(outputDir, Path.Combine(dir.Split('/')), "index.html");
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            await File.WriteAllTextAsync(targetFile, html, cancellationToken);
        }

        foreach (var extra in new[] { "robots.txt", "llms.txt", "sitemap.xml", "feed.xml" })
        {
            var content = await client.GetStringAsync($"/{extra}", cancellationToken);
            if (publicPrefix is not null)
                content = content.Replace(originPrefix, publicPrefix);
            await File.WriteAllTextAsync(Path.Combine(outputDir, extra), content, cancellationToken);
        }

        var notFoundResponse = await client.GetAsync("/__teatime_export_404__", cancellationToken);
        var notFoundHtml = await notFoundResponse.Content.ReadAsStringAsync(cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "404.html"), notFoundHtml, cancellationToken);

        // Search has no server on a static host, so ship the prebuilt index the client queries directly.
        var basePath = app.Services.GetRequiredService<PageRequestSettings>().BasePath;
        var authorHits = authors
            .Select(a => new AuthorSearchHit(a.Name, a.Url, Layout.LayoutProvider.ResolveAssetUrl(a.Image, basePath)))
            .ToList();
        var tagHits = view.Tags
            .Select(t => new TagSearchHit(t.Name, $"tags/{t.Slug}", t.Count))
            .ToList();
        var searchIndex = docs.GetSearchIndexExport() with { Authors = authorHits, Tags = tagHits };
        var searchJson = JsonSerializer.Serialize(searchIndex, TeatimeJsonContext.Default.SearchIndexExport);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "search-index.json"), searchJson, cancellationToken);
        app.Logger.LogInformation(
            "Static search index written: {Docs} docs, {Bytes:N0} bytes",
            searchIndex.Docs.Count, System.Text.Encoding.UTF8.GetByteCount(searchJson));

        CopyStaticAssets(app.Environment.WebRootPath, outputDir);

        var assetsSrc = Path.Combine(Path.GetFullPath(options.RootPath), "assets");
        if (Directory.Exists(assetsSrc))
            CopyStaticAssets(assetsSrc, Path.Combine(outputDir, "assets"));

        await app.StopAsync(cancellationToken);
    }

    private static void CopyStaticAssets(string sourceRoot, string outputDir)
    {
        if (!Directory.Exists(sourceRoot)) return;

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(outputDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
