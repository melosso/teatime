using System.Globalization;
using Teatime.Services.Rendering;
using Xunit;

namespace Teatime.Tests;

public class SlugColorTests
{
    [Fact]
    public void HueFor_IsDeterministic()
    {
        Assert.Equal(SlugColor.HueFor("hello-world"), SlugColor.HueFor("hello-world"));
    }

    [Fact]
    public void HueFor_SharedFirstLetter_StillDiffers()
    {
        var slugs = new[] { "why-teatime", "writing-well", "what-i-learned", "weeknotes-1", "weeknotes-2" };
        Assert.Equal(slugs.Length, slugs.Select(SlugColor.HueFor).Distinct().Count());
    }

    [Fact]
    public void HueFor_RepoSlugs_AreAllDistinct()
    {
        var slugs = new[] { "draft-example", "eupl-license", "grow", "hello-teatime", "on-markdown", "style-test" };
        Assert.Equal(slugs.Length, slugs.Select(SlugColor.HueFor).Distinct().Count());
    }

    [Fact]
    public void HueFor_SpreadsAcrossManySlugs()
    {
        var slugs = Enumerable.Range(0, 200).Select(i => $"post-number-{i}").ToArray();
        var distinct = slugs.Select(SlugColor.HueFor).Distinct().Count();

        Assert.True(distinct > 120, $"slug hues bunched up: only {distinct} distinct across {slugs.Length} slugs");
    }

    [Fact]
    public void HueFor_EmptyOrNullSlug_ReturnsStableFallback()
    {
        Assert.Equal(SlugColor.HueFor("teatime"), SlugColor.HueFor(null));
        Assert.Equal(SlugColor.HueFor("teatime"), SlugColor.HueFor(""));
    }

    [Fact]
    public void HueFor_IsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var turkish = SlugColor.HueFor("INDEX-I");
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(SlugColor.HueFor("INDEX-I"), turkish);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void HueFor_StaysWithinHueRange()
    {
        foreach (var slug in new[] { "a", "post-one", "another-post", "x-y-z", "2026-review" })
        {
            var hue = SlugColor.HueFor(slug);
            Assert.InRange(hue, 0, 359);
        }
    }
}
