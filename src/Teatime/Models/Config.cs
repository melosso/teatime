namespace Teatime.Models;

public class Config
{
    public string? Title { get; set; }
    public string? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public string? Lang { get; set; }
    public List<HeadTag>? Head { get; set; }

    public string? Brand { get; set; }
    public string? BrandImage { get; set; }
    public string? Footer { get; set; }
    public string? Favicon { get; set; }

    /// <summary>Author name shown in the post byline (single-author blog).</summary>
    public string? Author { get; set; }

    /// <summary>Optional avatar image URL for the byline; falls back to the author's initial.</summary>
    public string? AuthorImage { get; set; }

    /// <summary>Header nav items. When present, replaces the default Posts/Tags/Archive/About menu.</summary>
    public List<MenuLink>? Menu { get; set; }

    /// <summary>Top reading-progress bar. Defaults to on; set false to hide it.</summary>
    public bool? ScrollIndicator { get; set; }

    public List<SocialLink>? SocialLinks { get; set; }
}
