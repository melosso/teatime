namespace Teatime.Services.Extensions;

/// <summary>A verified comment back end. Its embed mounts under a post, not in the head.</summary>
public interface ICommentProvider
{
    string Name { get; }

    /// <summary>Origin for script-src, connect-src, img-src and frame-src.</summary>
    string Origin { get; }
}

/// <summary>Self-hosted Remark42. Stores threads in its own Bolt file, so it needs no database.</summary>
public sealed record Remark42Provider(
    string Origin,
    string BaseUrl,
    string SiteId,
    string Theme,
    string? Locale,
    int MaxShownComments) : ICommentProvider
{
    public string Name => "remark42";
}
