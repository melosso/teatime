using Teatime.Models;
using Teatime.Services.Rendering;

namespace Teatime.Tests;

public sealed class SiteNavRendererTests
{
    private static Config DropdownConfig() => new()
    {
        Menu =
        [
            new MenuLink
            {
                Title = "Hulp en bronnen",
                Items =
                [
                    new MenuLink { Title = "Hulp en bronnen", Path = "/hulp/" },
                    new MenuLink { Title = "Praatgroepen", Path = "/hulp/praatgroepen/" },
                    new MenuLink { Title = "Zorgverlening", Path = "/hulp/zorg/" }
                ]
            }
        ]
    };

    [Fact]
    public void Dropdown_ChildPage_OnlyExactChildIsHere()
    {
        var html = SiteNavRenderer.Build(DropdownConfig(), basePath: "", currentPath: "hulp/praatgroepen");

        // The section-root "/hulp/" is a prefix of the current page but must NOT light up.
        Assert.DoesNotContain("<a class=\"top-nav-dropdown-link here\" href=\"/hulp/\"", html);
        Assert.Contains("<a class=\"top-nav-dropdown-link here\" href=\"/hulp/praatgroepen/\"", html);
        // Exactly one dropdown link is marked active.
        Assert.Equal(1, CountOccurrences(html, "top-nav-dropdown-link here"));
    }

    [Fact]
    public void Dropdown_SectionRoot_RootIsHere()
    {
        var html = SiteNavRenderer.Build(DropdownConfig(), basePath: "", currentPath: "hulp");

        Assert.Contains("<a class=\"top-nav-dropdown-link here\" href=\"/hulp/\"", html);
        Assert.Equal(1, CountOccurrences(html, "top-nav-dropdown-link here"));
    }

    [Fact]
    public void Dropdown_ActiveChild_MarksButtonActive()
    {
        var html = SiteNavRenderer.Build(DropdownConfig(), basePath: "", currentPath: "hulp/zorg");

        Assert.Contains("class=\"top-nav-link active\"", html);
        Assert.Contains("<a class=\"top-nav-dropdown-link here\" href=\"/hulp/zorg/\"", html);
    }

    [Fact]
    public void Dropdown_UnrelatedPage_NothingHere()
    {
        var html = SiteNavRenderer.Build(DropdownConfig(), basePath: "", currentPath: "over-ons");

        Assert.DoesNotContain(" here", html);
        Assert.DoesNotContain("top-nav-link active", html);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }
}
