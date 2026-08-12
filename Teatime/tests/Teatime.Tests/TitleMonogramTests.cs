using Teatime.Models;
using Teatime.Services.Rendering;
using Xunit;

namespace Teatime.Tests;

public class TitleMonogramTests
{
    private static TitleMonogram For(string? culture) =>
        TitleMonogram.From(new LocaleOptions { Culture = culture });

    [Theory]
    [InlineData("The Garden", "G")]
    [InlineData("A Garden", "G")]
    [InlineData("An Orchard", "O")]
    [InlineData("Garden", "G")]
    public void English_SkipsLeadingArticle(string title, string expected) =>
        Assert.Equal(expected, For("en-GB").Letter(title));

    [Theory]
    [InlineData("nl-NL", "De Tuin", "T")]
    [InlineData("nl-NL", "Het Huis", "H")]
    [InlineData("nl-NL", "Een Tuin", "T")]
    [InlineData("de-DE", "Die Verwandlung", "V")]
    [InlineData("de-DE", "Das Kapital", "K")]
    [InlineData("es-ES", "La Casa", "C")]
    [InlineData("es-ES", "Los Jardines", "J")]
    [InlineData("fr-FR", "Les Fleurs", "F")]
    public void OtherLanguages_SkipTheirOwnArticles(string culture, string title, string expected) =>
        Assert.Equal(expected, For(culture).Letter(title));

    [Theory]
    [InlineData("L'été Dernier", "É")]
    [InlineData("L’Étranger", "É")]
    public void French_HandlesElision(string title, string expected) =>
        Assert.Equal(expected, For("fr-FR").Letter(title));

    [Fact]
    public void Polish_HasNoArticlesToSkip()
    {
        Assert.Equal("O", For("pl-PL").Letter("Ogród"));
        Assert.Equal("A", For("pl-PL").Letter("A Potem"));
    }

    [Fact]
    public void DutchArticle_IsNotStrippedForEnglishSite()
    {
        Assert.Equal("D", For("en-GB").Letter("De Stijl"));
    }

    [Fact]
    public void ArticleOnlyTitle_KeepsTheArticle()
    {
        Assert.Equal("T", For("en-GB").Letter("The"));
        Assert.Equal("A", For("en-GB").Letter("A"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("!!!")]
    public void EmptyOrPunctuationOnly_FallsBackToT(string? title) =>
        Assert.Equal("T", For("en-GB").Letter(title));

    [Fact]
    public void LeadingPunctuation_IsIgnored()
    {
        Assert.Equal("G", For("en-GB").Letter("“The Garden”"));
        Assert.Equal("R", For("en-GB").Letter("...Rain"));
    }

    [Fact]
    public void UnknownCulture_FallsBackToEnglishArticles() =>
        Assert.Equal("G", For("zz-ZZ").Letter("The Garden"));

    [Fact]
    public void Casing_FollowsConfiguredCulture() =>
        Assert.Equal("İ", For("tr-TR").Letter("istanbul"));
}
