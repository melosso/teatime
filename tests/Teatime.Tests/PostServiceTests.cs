using Teatime.Models;
using Teatime.Services;

namespace Teatime.Tests;

public class PostServiceTests
{
    private static DocumentationPage PostPage(
        string slug, DateTime date, bool draft = false,
        IReadOnlyList<string>? tags = null, string html = "<p>Body text here.</p>")
        => new(
            Path: $"posts/{slug}",
            Title: slug,
            HtmlContent: html,
            Date: date,
            Draft: draft,
            Tags: tags);

    [Fact]
    public void Build_OrdersPostsByDateDescending()
    {
        var pages = new List<DocumentationPage>
        {
            PostPage("old", new DateTime(2024, 1, 1)),
            PostPage("new", new DateTime(2026, 1, 1)),
            PostPage("mid", new DateTime(2025, 1, 1)),
        };

        var view = PostService.Build(pages, includeDrafts: false);

        Assert.Equal(["new", "mid", "old"], view.Posts.Select(p => p.Slug));
    }

    [Fact]
    public void Build_ExcludesDraftsWhenNotIncludingDrafts()
    {
        var pages = new List<DocumentationPage>
        {
            PostPage("live", new DateTime(2026, 1, 1)),
            PostPage("wip", new DateTime(2026, 2, 1), draft: true),
        };

        Assert.Single(PostService.Build(pages, includeDrafts: false).Posts);
        Assert.Equal(2, PostService.Build(pages, includeDrafts: true).Posts.Count);
    }

    [Fact]
    public void Build_IgnoresNonPostPages()
    {
        var pages = new List<DocumentationPage>
        {
            PostPage("real", new DateTime(2026, 1, 1)),
            new(Path: "about", Title: "About", HtmlContent: "<p>x</p>"),
            new(Path: "index", Title: "Home", HtmlContent: "<p>x</p>"),
        };

        var view = PostService.Build(pages, includeDrafts: false);

        Assert.Single(view.Posts);
        Assert.Equal("real", view.Posts[0].Slug);
    }

    [Fact]
    public void Build_GroupsAndCountsTagsBySlug()
    {
        var pages = new List<DocumentationPage>
        {
            PostPage("a", new DateTime(2026, 1, 1), tags: ["Dot Net", "Web"]),
            PostPage("b", new DateTime(2026, 2, 1), tags: ["dot-net"]),
        };

        var view = PostService.Build(pages, includeDrafts: false);

        Assert.Equal(2, view.ByTagSlug["dot-net"].Count);
        Assert.Single(view.ByTagSlug["web"]);
        Assert.Contains(view.Tags, t => t is { Slug: "dot-net", Count: 2 });
    }

    [Fact]
    public void FromPage_UsesSlugOverrideAndComputesUrl()
    {
        var page = PostPage("original", new DateTime(2026, 1, 1)) with { Slug = "Custom Slug!" };

        var post = Post.FromPage(page);

        Assert.Equal("custom-slug", post.Slug);
        Assert.Equal("posts/custom-slug", post.Url);
    }

    [Fact]
    public void FromPage_FallsBackToFirstParagraphExcerpt()
    {
        var page = PostPage("p", new DateTime(2026, 1, 1), html: "<h1>T</h1><p>First para.</p><p>Second.</p>");

        var post = Post.FromPage(page);

        Assert.Equal("First para.", post.Excerpt);
    }

    [Fact]
    public void FromPage_ReadingMinutesAtLeastOne()
    {
        var post = Post.FromPage(PostPage("p", new DateTime(2026, 1, 1), html: "<p>one two three</p>"));
        Assert.Equal(1, post.ReadingMinutes);
    }
}
