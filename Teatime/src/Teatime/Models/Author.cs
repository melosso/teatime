namespace Teatime.Models;

public sealed record Author(string Id, string Slug, string Name, string? Image, string BioHtml, bool Hidden = false)
{
    public string Url => $"authors/{Slug}";
}
