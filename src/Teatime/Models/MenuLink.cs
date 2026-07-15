namespace Teatime.Models;

public sealed record MenuLink
{
    public string? Title { get; init; }
    public string? Path { get; init; }

    /// <summary>Nested items. When present, this entry renders as a hover/focus dropdown instead of a link.</summary>
    public List<MenuLink>? Items { get; init; }
}
