namespace Teatime.Models;

/// <summary>Opt-in bookmark cards. Bound from <c>config.json</c> under <c>bookmarks</c></summary>
public sealed class BookmarkOptions
{
    public bool Enabled { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
    public List<string>? AllowHosts { get; set; }
    public List<string>? DenyHosts { get; set; }
}
