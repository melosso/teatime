namespace Teatime.Models;

/// <summary>A blog surface the router owns: a literal endpoint that outranks the content catch-all,
/// so a content page can never claim its slug.</summary>
public sealed record ReservedRoute(string Slug, string Title, Func<Config?, bool> Enabled)
{
    public bool IsEnabled(Config? config) => Enabled(config);
}

public static class ReservedRoutes
{
    public static readonly ReservedRoute Tags = new("tags", "Tags", c => c?.Tags != false);
    public static readonly ReservedRoute Archive = new("archive", "Archive", c => c?.Archive != false);
    public static readonly ReservedRoute Authors = new("authors", "Authors", _ => true);

    public static readonly ReservedRoute[] Indexes = [Tags, Archive, Authors];

    private static readonly string[] Files = ["feed.xml", "sitemap.xml", "robots.txt", "llms.txt"];

    private static readonly string[] Prefixes = ["tags/", "authors/", "page/"];

    /// <summary>Routes the app serves that no content file backs. Dead-link checking treats these as live.</summary>
    public static bool IsKnown(string resolved) =>
        Indexes.Any(r => resolved == r.Slug)
        || Files.Contains(resolved)
        || Prefixes.Any(p => resolved.StartsWith(p, StringComparison.Ordinal));

    /// <summary>Content roots the catch-all must never serve as standalone pages; posts and authors
    /// have their own endpoints and URL shapes.</summary>
    public static bool IsContentPrefixed(string path) =>
        path.StartsWith("posts/", StringComparison.Ordinal)
        || path.StartsWith("authors/", StringComparison.Ordinal);
}
