using Teatime.Services;
using Teatime.Services.MarkdownExtensions;

namespace Teatime.Tests;

public sealed class NewsletterBlockTests
{
    private readonly MarkdownService _service = new();

    private string Render(string block) => _service.Parse(block).Html;

    [Fact]
    public void EmptyBlock_RendersFormWithDefaults()
    {
        var html = Render("```newsletter\n```\n");

        Assert.Contains("class=\"teatime-newsletter\"", html);
        Assert.Contains("name=\"email\"", html);
        Assert.Contains("Subscribe", html);
        Assert.Contains("name=\"website\"", html);
    }

    [Fact]
    public void ConsentAndNameAreOptIn()
    {
        var plain = Render("```newsletter\n```\n");
        Assert.DoesNotContain("name=\"consent\"", plain);
        Assert.DoesNotContain("name=\"name\"", plain);

        var full = Render("```newsletter\nconsent: true\nname: true\n```\n");
        Assert.Contains("name=\"consent\"", full);
        Assert.Contains("name=\"name\"", full);
    }

    [Fact]
    public void CustomTextIsHtmlEncoded()
    {
        var html = Render("```newsletter\nheading: \"Tea & <script>alert(1)</script>\"\n```\n");

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("Tea &amp; &lt;script&gt;", html);
    }

    [Fact]
    public void ConsentTakesReplacementTextAsWellAsAFlag()
    {
        var html = Render("```newsletter\nconsent: I agree to the terms.\n```\n");

        Assert.Contains("name=\"consent\"", html);
        Assert.Contains("I agree to the terms.", html);
    }

    [Fact]
    public void LocaleMapRendersTheDefaultSiteLocale()
    {
        var html = Render("```newsletter\nheading:\n  en: Subscribe\n  nl: Aanmelden\n```\n");

        Assert.Contains("Subscribe", html);
        Assert.DoesNotContain("Aanmelden", html);
    }

    [Fact]
    public void MalformedBlock_ReportsInsteadOfRenderingForm()
    {
        var html = Render("```newsletter\nheading: [unclosed\n```\n");

        Assert.DoesNotContain("class=\"teatime-newsletter\"", html);
        Assert.Contains("newsletter-error", html);
    }
}

public sealed class NewsletterBlockLocaleTests
{
    private static Dictionary<object, object?> Spec(params (string Key, object? Value)[] entries) =>
        entries.ToDictionary(e => (object)e.Key, e => e.Value);

    private static Dictionary<object, object?> ByLocale(params (string Code, string Text)[] entries) =>
        entries.ToDictionary(e => (object)e.Code, e => (object?)e.Text);

    [Fact]
    public void PlainStringAppliesToEveryLocale()
    {
        var spec = Spec(("heading", "Subscribe"));

        Assert.Equal("Subscribe", NewsletterBlock.Text(spec, "heading", "nl"));
        Assert.Equal("Subscribe", NewsletterBlock.Text(spec, "heading", "en"));
    }

    [Fact]
    public void LocaleMapPicksTheActiveCode()
    {
        var spec = Spec(("heading", ByLocale(("en", "Subscribe"), ("nl", "Aanmelden"))));

        Assert.Equal("Aanmelden", NewsletterBlock.Text(spec, "heading", "nl"));
        Assert.Equal("Subscribe", NewsletterBlock.Text(spec, "heading", "en"));
    }

    [Fact]
    public void RegionalCodeFallsBackToItsBaseLanguage()
    {
        var spec = Spec(("heading", ByLocale(("nl", "Aanmelden"))));

        Assert.Equal("Aanmelden", NewsletterBlock.Text(spec, "heading", "nl-BE"));
    }

    [Fact]
    public void UnknownLocaleFallsBackToEnglishThenToAnything()
    {
        var withEnglish = Spec(("heading", ByLocale(("en", "Subscribe"), ("nl", "Aanmelden"))));
        Assert.Equal("Subscribe", NewsletterBlock.Text(withEnglish, "heading", "pl"));

        var withoutEnglish = Spec(("heading", ByLocale(("nl", "Aanmelden"))));
        Assert.Equal("Aanmelden", NewsletterBlock.Text(withoutEnglish, "heading", "pl"));
    }

    [Fact]
    public void MissingOrBlankFieldDefersToTheBuiltInString()
    {
        Assert.Null(NewsletterBlock.Text(Spec(), "heading", "en"));
        Assert.Null(NewsletterBlock.Text(Spec(("heading", "   ")), "heading", "en"));
    }
}
