using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services;

public static class GroupedSearch
{
    private const int MaxAuthors = 5;
    private const int MaxTags = 8;

    public static GroupedSearchResponse Build(
        string query,
        IReadOnlyList<SearchResult> posts,
        IReadOnlyList<Author> authors,
        IReadOnlyList<TagInfo> tags,
        string basePath)
    {
        var q = query.Trim();

        var authorHits = authors
            .Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAuthors)
            .Select(a => new AuthorSearchHit(a.Name, a.Url, LayoutProvider.ResolveAssetUrl(a.Image, basePath)))
            .ToList();

        var tagHits = tags
            .Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(t => t.Count)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxTags)
            .Select(t => new TagSearchHit(t.Name, $"tags/{t.Slug}", t.Count))
            .ToList();

        var postHits = posts
            .Where(p => !p.Path.StartsWith("authors/", StringComparison.Ordinal))
            .ToList();

        return new GroupedSearchResponse(authorHits, tagHits, postHits);
    }
}
