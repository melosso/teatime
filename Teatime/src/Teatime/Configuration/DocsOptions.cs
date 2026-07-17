namespace Teatime.Configuration;

public sealed record DocsOptions
{
    public string RootPath { get; init; } = "content";
    public string? DefaultPage { get; init; } = "index";
    public bool EnableHotReload { get; init; } = true;
    public int PageSize { get; init; } = 10;
    public string? BasePath { get; init; }

    // Static export: pages load a prebuilt search index instead of /api/search.
    public bool IsStaticExport { get; init; }
}
