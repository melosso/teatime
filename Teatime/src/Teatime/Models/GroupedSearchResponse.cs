namespace Teatime.Models;

public sealed record GroupedSearchResponse(
    IReadOnlyList<AuthorSearchHit> Authors,
    IReadOnlyList<TagSearchHit> Tags,
    IReadOnlyList<SearchResult> Posts)
{
    public static readonly GroupedSearchResponse Empty = new([], [], []);
}

public sealed record AuthorSearchHit(string Name, string Url, string? Image);

public sealed record TagSearchHit(string Name, string Url, int Count);
