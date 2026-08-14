using Teatime.Models;
using Teatime.Services.Rendering;

namespace Teatime.Tests;

public sealed class FooterMenuRendererTests
{
    [Fact]
    public void NullMenu_ReturnsNull_SoDefaultsRender()
    {
        Assert.Null(FooterMenuRenderer.Build(null, basePath: ""));
    }

    [Fact]
    public void DeclaredEmptyMenu_ReturnsEmpty_NotDefaults()
    {
        var html = FooterMenuRenderer.Build([], basePath: "");

        Assert.Equal("", html);
    }

    [Fact]
    public void PopulatedMenu_RendersLinks()
    {
        var html = FooterMenuRenderer.Build(
            [new MenuLink { Title = "RSS", Path = "/feed.xml" }],
            basePath: "");

        Assert.Equal("<a href=\"/feed.xml/\">RSS</a>", html);
    }
}
