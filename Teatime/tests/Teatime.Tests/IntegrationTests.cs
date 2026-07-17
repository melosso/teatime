using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Teatime.Tests;

/// <summary>Boots the real app in-memory against a temp content dir; real routing, middleware, rate limiter, CSP and ETag flow</summary>
public class TeatimeWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ContentDir { get; } =
        Path.Combine(Path.GetTempPath(), "teatime-integration-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(ContentDir, "posts"));
        Directory.CreateDirectory(Path.Combine(ContentDir, "pages"));
        File.WriteAllText(Path.Combine(ContentDir, "posts", "hello-world.md"),
            "---\ntitle: Hello World\ndate: 2026-01-02\ntags: [guide]\n---\n\n# Hello World\n\nInstallation instructions here.\n");
        File.WriteAllText(Path.Combine(ContentDir, "posts", "second-post.md"),
            "---\ntitle: Second Post\ndate: 2026-01-05\ntags: [guide, meta]\n---\n\nMore words to read.\n");
        File.WriteAllText(Path.Combine(ContentDir, "pages", "about.md"),
            "---\ntitle: About\ndescription: About this blog\n---\n\n# About\n\nColophon here.\n");
        File.WriteAllText(Path.Combine(ContentDir, "config.json"),
            """{"title": "Test Blog"}""");

        // Port 0 keeps the pre-bind port probe from colliding with a running dev server
        builder.UseSetting("urls", "http://127.0.0.1:0");
        builder.UseSetting("Docs:RootPath", ContentDir);
        builder.UseSetting("Docs:EnableHotReload", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(ContentDir))
            Directory.Delete(ContentDir, true);
    }
}

public sealed class IntegrationTests : IClassFixture<TeatimeWebApplicationFactory>
{
    private readonly TeatimeWebApplicationFactory _factory;

    public IntegrationTests(TeatimeWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Root_ListsPosts_WithHtmlAndSecurityHeaders()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("nonce-", csp);
        Assert.NotNull(response.Headers.ETag);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Hello World", html);
        Assert.Contains("/posts/hello-world/", html);
    }

    [Fact]
    public async Task Post_SecondRequestWithETag_Returns304()
    {
        var client = _factory.CreateClient();
        var first = await client.GetAsync("/posts/hello-world");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var etag = first.Headers.ETag;
        Assert.NotNull(etag);

        var request = new HttpRequestMessage(HttpMethod.Get, "/posts/hello-world");
        request.Headers.IfNoneMatch.Add(etag!);
        var second = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task StandalonePage_ServedAtSlug()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/about");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Colophon here.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TagPage_ListsTaggedPosts()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/tags/guide");
        Assert.Contains("Hello World", html);
        Assert.Contains("Second Post", html);
    }

    [Fact]
    public async Task Archive_Responds()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/archive");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("2026", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownPage_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/no/such/page");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiPages_ReturnsSummaries_WithoutServerFilePaths()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/pages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"path\":\"posts/hello-world\"", json);
        Assert.Contains("\"title\":\"Hello World\"", json);
        // Contract; the response must never leak OriginalRelativePath or any server file path
        Assert.DoesNotContain(".md", json);
        Assert.DoesNotContain("originalRelativePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiSearch_FindsPost_AndShortQueryReturnsEmpty()
    {
        var client = _factory.CreateClient();

        var hit = await client.GetStringAsync("/api/search?q=installation");
        Assert.Contains("posts/hello-world", hit);
        Assert.Contains("\"posts\":", hit);

        var shortQuery = await client.GetStringAsync("/api/search?q=a");
        Assert.Contains("\"authors\":[]", shortQuery);
        Assert.Contains("\"tags\":[]", shortQuery);
        Assert.Contains("\"posts\":[]", shortQuery);
    }

    [Fact]
    public async Task ApiBuildVersion_IsNotCached()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/build-version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"version\":", json);
    }

    [Fact]
    public async Task Raw_ValidPost_ReturnsMarkdownAttachment()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/raw/posts/hello-world");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Installation instructions here.", body);
    }

    [Theory]
    [InlineData("/raw/..%2Fappsettings.json")]
    [InlineData("/raw/..%2f..%2fappsettings.json")]
    [InlineData("/raw/%2e%2e/appsettings.json")]
    [InlineData("/raw/nonexistent")]
    public async Task Raw_TraversalOrUnknownPath_Returns404(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);

        // 400 is also acceptable; the framework may reject encoded dot-segments before routing
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Expected 404/400 for {url}, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Seo_RobotsSitemapLlmsFeed_AllRespond()
    {
        var client = _factory.CreateClient();

        var robots = await client.GetStringAsync("/robots.txt");
        Assert.Contains("Sitemap:", robots);

        var sitemap = await client.GetStringAsync("/sitemap.xml");
        Assert.Contains("<urlset", sitemap);
        Assert.Contains("posts/hello-world", sitemap);

        var llms = await client.GetStringAsync("/llms.txt");
        Assert.Contains("[Hello World]", llms);

        var feed = await client.GetAsync("/feed.xml");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        Assert.Contains("<rss", await feed.Content.ReadAsStringAsync());
    }
}

/// <summary>Own factory instance; the author files must not leak into the shared fixture's search and sitemap tests</summary>
public sealed class HiddenAuthorTests
{
    private sealed class AuthorFactory : TeatimeWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            Directory.CreateDirectory(Path.Combine(ContentDir, "authors"));
            File.WriteAllText(Path.Combine(ContentDir, "authors", "visible.md"),
                "---\nid: visible\nname: Visible Author\n---\n\nBio.\n");
            File.WriteAllText(Path.Combine(ContentDir, "authors", "ghost.md"),
                "---\nid: ghost\nname: Ghost Author\nhidden: true\n---\n\nBio.\n");
            File.WriteAllText(Path.Combine(ContentDir, "posts", "ghost-written.md"),
                "---\ntitle: Ghost Written\ndate: 2026-01-09\nauthor: ghost\n---\n\nWords.\n");
        }
    }

    [Fact]
    public async Task AuthorIndex_OmitsHiddenAuthor()
    {
        using var factory = new AuthorFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/authors");

        Assert.Contains("Visible Author", html);
        Assert.DoesNotContain("Ghost Author", html);
    }

    [Fact]
    public async Task HiddenAuthor_PageReturns404()
    {
        using var factory = new AuthorFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/authors/ghost");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HiddenAuthor_BylineNamesThemWithoutLinking()
    {
        using var factory = new AuthorFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/posts/ghost-written");

        Assert.Contains("Ghost Author", html);
        Assert.DoesNotContain("authors/ghost", html);
    }

    [Fact]
    public async Task Sitemap_OmitsHiddenAuthor()
    {
        using var factory = new AuthorFactory();
        var client = factory.CreateClient();

        var sitemap = await client.GetStringAsync("/sitemap.xml");

        Assert.Contains("authors/visible", sitemap);
        Assert.DoesNotContain("authors/ghost", sitemap);
    }
}

/// <summary>Own factory instance; the reserved-route pages must not leak into the shared fixture's tag/archive tests</summary>
public sealed class ReservedRouteRedirectTests
{
    private sealed class RedirectFactory : TeatimeWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            File.WriteAllText(Path.Combine(ContentDir, "pages", "tags.md"),
                "---\ntitle: Tags\nredirect: /\n---\n");
            File.WriteAllText(Path.Combine(ContentDir, "pages", "archive.md"),
                "---\ntitle: Archive\nredirect: /about\n---\n");
            File.WriteAllText(Path.Combine(ContentDir, "pages", "authors.md"),
                "---\ntitle: Authors\nredirect: /about\n---\n");
        }
    }

    [Theory]
    [InlineData("/tags", "/")]
    [InlineData("/archive", "/about/")]
    [InlineData("/authors", "/about/")]
    public async Task ReservedRoute_WithRedirectFrontMatter_Redirects(string route, string expected)
    {
        using var factory = new RedirectFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.ToString());
    }
}

/// <summary>Own factory instance; burning the rate-limit budget must not starve shared-fixture tests (no remote IP on TestServer, all requests share one partition)</summary>
public sealed class RateLimitIntegrationTests
{
    [Fact]
    public async Task ApiSearch_OverLimit_Returns429()
    {
        using var factory = new TeatimeWebApplicationFactory();
        var client = factory.CreateClient();

        var lastStatus = HttpStatusCode.OK;
        for (var i = 0; i < 35; i++)
        {
            var response = await client.GetAsync("/api/search?q=install");
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }
}
