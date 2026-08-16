using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Rendering;

namespace Teatime.Tests;

public sealed class FooterRendererTests
{
    private static readonly MarkdownService Markdown = new();

    [Fact]
    public void NoConfig_RendersDefaultFooter()
    {
        var html = FooterRenderer.Build(null, basePath: "", brandText: "Teatime", socialLinksHtml: null, Markdown);

        Assert.Contains("site-footer", html);
        Assert.Contains("RSS", html);
        Assert.Contains("Archive", html);
    }

    [Fact]
    public void DeclaredEmptyMenu_NoFooterText_NoSocialLinks_OmitsFooterEntirely()
    {
        var config = new Config { FooterMenu = [] };

        var html = FooterRenderer.Build(config, basePath: "", brandText: "Teatime", socialLinksHtml: null, Markdown);

        Assert.Equal("", html);
    }

    [Fact]
    public void DeclaredEmptyMenu_WithCustomFooterText_StillRenders()
    {
        var config = new Config { FooterMenu = [], Footer = "All rights reserved." };

        var html = FooterRenderer.Build(config, basePath: "", brandText: "Teatime", socialLinksHtml: null, Markdown);

        Assert.Contains("site-footer", html);
        Assert.Contains("All rights reserved.", html);
    }

    [Fact]
    public void DeclaredEmptyMenu_WithSocialLinks_StillRenders()
    {
        var config = new Config { FooterMenu = [] };

        var html = FooterRenderer.Build(config, basePath: "", brandText: "Teatime", socialLinksHtml: "<a href=\"https://example.com\">Mastodon</a>", Markdown);

        Assert.Contains("site-footer", html);
        Assert.Contains("Mastodon", html);
    }
}
