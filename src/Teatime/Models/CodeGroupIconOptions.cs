namespace Teatime.Models;

/// <summary>Bound from `Docs:CodeGroupIcons` (appsettings.json), not teatime.json -- pipeline is built once at startup.</summary>
public sealed record CodeGroupIconOptions
{
    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "/icons";

    public string Format { get; init; } = "svg";

    public Dictionary<string, string>? Overrides { get; init; }
}
