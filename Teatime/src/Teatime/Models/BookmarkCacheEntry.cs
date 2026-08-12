namespace Teatime.Models;

/// <summary>Resolved bookmark; paths are local site-relative</summary>
public sealed record BookmarkCacheEntry(
    string Url,
    string Title,
    string? Description,
    string? IconPath,
    string? ThumbnailPath,
    string? Author,
    string Publisher);

/// <summary>On-disk JSON cache</summary>
public sealed record BookmarkCache(IReadOnlyList<BookmarkCacheEntry> Entries);