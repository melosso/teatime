namespace Teatime.Models;

// Serializable SearchIndex projection for static export; postings reference docs by index.
public sealed record SearchIndexExport(
    IReadOnlyList<SearchDocEntry> Docs,
    IReadOnlyDictionary<string, IReadOnlyList<SearchPosting>> Terms,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Trigrams);

public sealed record SearchDocEntry(string Path, string Title, string? Description, string Text);

public readonly record struct SearchPosting(int Doc, int Score);
