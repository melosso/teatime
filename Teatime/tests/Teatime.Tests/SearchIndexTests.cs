using System.Text.Json;
using Teatime.Models;
using Teatime.Serialization;
using Teatime.Services;

namespace Teatime.Tests;

public sealed class SearchIndexTests
{
    private static DocumentationPage MakePage(string path, string title, string? description = null, IEnumerable<HeadingInfo>? headings = null, string body = "")
    {
        var html = $"<p>{body}</p>";
        return new DocumentationPage(path, title, html, description, null, headings ?? []);
    }

    [Fact]
    public void Search_EmptyIndex_ReturnsEmpty()
    {
        var index = new SearchIndex();
        index.Build([]);
        var results = index.Search("anything");
        Assert.Empty(results);
    }

    [Fact]
    public void Search_TitleMatch_ReturnsResult()
    {
        var index = new SearchIndex();
        index.Build([MakePage("install", "Installation Guide")]);
        var results = index.Search("installation");
        Assert.Single(results);
        Assert.Equal("install", results[0].Path);
    }

    [Fact]
    public void Search_TitleMatch_HighestScore()
    {
        var index = new SearchIndex();
        index.Build([
            MakePage("install", "Installation Guide"),
            MakePage("intro", "Introduction", "about installation")
        ]);
        var results = index.Search("installation");
        Assert.Equal(2, results.Count);
        Assert.Equal("install", results[0].Path);
    }

    [Fact]
    public void Search_DescriptionMatch_ReturnsResult()
    {
        var index = new SearchIndex();
        index.Build([MakePage("page", "Title", "This is about configuration")]);
        var results = index.Search("configuration");
        Assert.Single(results);
    }

    [Fact]
    public void Search_HeadingMatch_ReturnsResult()
    {
        var index = new SearchIndex();
        index.Build([MakePage("page", "Title", null, [new HeadingInfo("Getting Started", "getting-started")])]);
        var results = index.Search("started");
        Assert.Single(results);
    }

    [Fact]
    public void Search_BodyMatch_ReturnsResult()
    {
        var index = new SearchIndex();
        index.Build([MakePage("page", "Title", body: "This content mentions deployment")]);
        var results = index.Search("deployment");
        Assert.Single(results);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var index = new SearchIndex();
        index.Build([MakePage("page", "Title")]);
        var results = index.Search("nonexistent");
        Assert.Empty(results);
    }

    [Fact]
    public void Search_MultipleTerms_ReturnsUnion()
    {
        var index = new SearchIndex();
        index.Build([
            MakePage("install", "Installation Guide"),
            MakePage("config", "Configuration Guide"),
            MakePage("deploy", "Deployment Guide")
        ]);
        var results = index.Search("installation deployment");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        var index = new SearchIndex();
        index.Build([MakePage("install", "INSTALLATION GUIDE")]);
        var results = index.Search("installation");
        Assert.Single(results);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        var index = new SearchIndex();
        index.Build([MakePage("page", "Title")]);
        Assert.Empty(index.Search(""));
        Assert.Empty(index.Search("   "));
    }

    [Fact]
    public void Search_WithExcerpt_IncludesContext()
    {
        var index = new SearchIndex();
        var body = "This is a long paragraph about deployment strategies and best practices.";
        var page = MakePage("deploy", "Deployment", body: body);
        index.Build([page]);
        var results = index.Search("deployment");
        var result = Assert.Single(results);
        Assert.NotNull(result.Excerpt);
        Assert.Contains("deployment", result.Excerpt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ReplacesPreviousIndex()
    {
        var index = new SearchIndex();
        index.Build([MakePage("old", "Old Page")]);
        index.Build([MakePage("new", "New Page")]);
        var results = index.Search("new");
        Assert.Single(results);
        Assert.Empty(index.Search("old"));
    }

    [Fact]
    public void ExportSnapshot_ContainsDocsTermsAndTrigrams()
    {
        var index = new SearchIndex();
        index.Build([MakePage("install", "Installation Guide", body: "deployment strategies")]);
        var export = index.ExportSnapshot();

        var doc = Assert.Single(export.Docs);
        Assert.Equal("install", doc.Path);
        Assert.Equal("Installation Guide", doc.Title);
        Assert.Contains("deployment", doc.Text);
        Assert.True(export.Terms.ContainsKey("installation"));
        Assert.True(export.Trigrams.ContainsKey("ins"));
    }

    [Fact]
    public void ExportSnapshot_PostingsReferenceDocsByIndex()
    {
        var index = new SearchIndex();
        index.Build([MakePage("a", "Alpha"), MakePage("b", "Beta")]);
        var export = index.ExportSnapshot();

        var postings = export.Terms["alpha"];
        var posting = Assert.Single(postings);
        Assert.Equal("a", export.Docs[posting.Doc].Path);
        Assert.True(posting.Score > 0);
    }

    [Fact]
    public void ExportSnapshot_RoundTripsThroughTeatimeJsonContext()
    {
        var index = new SearchIndex();
        index.Build([MakePage("guide", "Guide", "intro", body: "content here")]);
        var export = index.ExportSnapshot();

        var json = JsonSerializer.Serialize(export, TeatimeJsonContext.Default.SearchIndexExport);
        var restored = JsonSerializer.Deserialize(json, TeatimeJsonContext.Default.SearchIndexExport);

        Assert.NotNull(restored);
        Assert.Equal(export.Docs.Count, restored!.Docs.Count);
        Assert.Equal("guide", restored.Docs[0].Path);
        Assert.True(restored.Terms.ContainsKey("guide"));
        Assert.Contains("\"docs\"", json);
    }
}
