using Teatime.Models;
using Teatime.Services.Rendering;

namespace Teatime.Tests;

public sealed class ArchiveColumnTests
{
    private static IReadOnlyList<(int, IReadOnlyList<Post>)> OneYear() =>
    [
        (2026, new[]
        {
            new Post(
                Slug: "hello", Path: "posts/hello", Title: "Hello", Date: new DateTime(2026, 7, 15),
                Updated: null, Tags: [], Excerpt: "", HtmlContent: "", Description: null,
                Headings: [], ReadingMinutes: 1, Cover: null, AuthorId: "murdock"),
        })
    ];

    private static string Render(IReadOnlyList<string>? columns) =>
        ArchiveRenderer.BuildYears(OneYear(), "", columns);

    [Fact]
    public void NoColumns_RendersDateThenTitle()
    {
        var html = Render(null);

        Assert.Contains("<time datetime=\"2026-07-15\">", html, StringComparison.Ordinal);
        Assert.Contains("Hello</a>", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf("<time", StringComparison.Ordinal) < html.IndexOf("<a ", StringComparison.Ordinal));
    }

    [Fact]
    public void ColumnOrderIsHonoured()
    {
        var html = Render(["title", "date"]);

        Assert.True(html.IndexOf("<a ", StringComparison.Ordinal) < html.IndexOf("<time", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownNamesAreDroppedAndBlankListFallsBackToTheDefault()
    {
        var html = Render(["nope", "title"]);
        Assert.DoesNotContain("<time", html, StringComparison.Ordinal);
        Assert.Contains("Hello</a>", html, StringComparison.Ordinal);

        var allBad = Render(["nope", "alsonope"]);
        Assert.Contains("<time", allBad, StringComparison.Ordinal);
        Assert.Contains("Hello</a>", allBad, StringComparison.Ordinal);
    }

    /// <summary>An id that resolves to no author file still shows as written, same as a byline.</summary>
    [Fact]
    public void UnresolvedAuthor_FallsBackToTheIdAsWritten()
    {
        var html = Render(["date", "title", "authorImage"]);

        Assert.Contains("title=\"murdock\"", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"avatar\" aria-hidden=\"true\">M</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PostWithNoAuthor_RendersNoAuthorColumn()
    {
        var years = new[]
        {
            (2026, (IReadOnlyList<Post>)new[]
            {
                new Post(
                    Slug: "hello", Path: "posts/hello", Title: "Hello", Date: new DateTime(2026, 7, 15),
                    Updated: null, Tags: [], Excerpt: "", HtmlContent: "", Description: null,
                    Headings: [], ReadingMinutes: 1, Cover: null, AuthorId: null),
            })
        };
        var html = ArchiveRenderer.BuildYears(years, "", ["date", "title", "authorImage"]);

        Assert.DoesNotContain("archive-author-avatar", html, StringComparison.Ordinal);
        Assert.Contains("Hello</a>", html, StringComparison.Ordinal);
    }
}
