using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Rendering;

namespace Teatime.Tests;

public sealed class BookmarkPlaceholderTests
{
    private readonly MarkdownService _service = new();

    [Fact]
    public void BareUrlOnOwnLine_BecomesPlaceholder()
    {
        var (html, _, _, _) = _service.Parse("https://example.com\n");
        Assert.Contains("class=\"bookmark-embed\"", html);
        Assert.Contains("data-bookmark-url=\"https://example.com\"", html);
    }

    [Fact]
    public void LabelledLink_StaysPlainLink()
    {
        var (html, _, _, _) = _service.Parse("[Example](https://example.com)\n");
        Assert.DoesNotContain("bookmark-embed", html);
    }

    [Fact]
    public void UrlInsideSentence_StaysPlainLink()
    {
        var (html, _, _, _) = _service.Parse("See https://example.com today.\n");
        Assert.DoesNotContain("bookmark-embed", html);
    }
}

public sealed class BookmarkCardRendererTests
{
    private static BookmarkCacheEntry Entry(string? icon, string? thumb, string title = "Fosstodon") =>
        new("https://fosstodon.org/@example", title, "A Mastodon instance", icon, thumb, "example", "fosstodon.org");

    [Fact]
    public void Render_WithImages_EmitsThumbnailAndIcon()
    {
        var html = BookmarkCardRenderer.Render(Entry("/bookmarks/icon.png", "/bookmarks/thumb.jpg"), "");
        Assert.Contains("kg-bookmark-thumbnail", html);
        Assert.Contains("/bookmarks/icon.png", html);
        Assert.Contains("/bookmarks/thumb.jpg", html);
    }

    [Fact]
    public void Render_WithoutIcon_UsesLetterChip()
    {
        var html = BookmarkCardRenderer.Render(Entry(null, null), "");
        Assert.Contains("kg-bookmark-icon-fallback", html);
        Assert.Contains(">F<", html);
    }

    [Fact]
    public void Render_WithoutThumbnail_OmitsThumbnail()
    {
        var html = BookmarkCardRenderer.Render(Entry("/bookmarks/icon.png", null), "");
        Assert.DoesNotContain("kg-bookmark-thumbnail", html);
    }

    [Fact]
    public void Render_EncodesTitle()
    {
        var html = BookmarkCardRenderer.Render(Entry(null, null, "<script>x</script>"), "");
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_AppliesBasePathToImages()
    {
        var html = BookmarkCardRenderer.Render(Entry("/bookmarks/icon.png", null), "/blog");
        Assert.Contains("/blog/bookmarks/icon.png", html);
    }
}

public sealed class BookmarkSsrfTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("172.16.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    public void IsBlockedAddress_RejectsInternal(string ip) =>
        Assert.True(BookmarkService.IsBlockedAddress(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946")]
    public void IsBlockedAddress_AllowsPublic(string ip) =>
        Assert.False(BookmarkService.IsBlockedAddress(IPAddress.Parse(ip)));
}

public sealed class BookmarkServiceDisabledTests
{
    private sealed class FakeEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Test";
        public string EnvironmentName { get; set; } = "Test";
    }

    [Fact]
    public void Render_WhenDisabled_LeavesPlaceholderUntouched()
    {
        using var service = new BookmarkService(new DocsOptions { IsStaticExport = true }, new FakeEnv(), NullLogger<BookmarkService>.Instance);
        const string html = "<div class=\"bookmark-embed\" data-bookmark-url=\"https://example.com\"><a href=\"https://example.com\">https://example.com</a></div>";
        Assert.Equal(html, service.Render(html));
    }
}
