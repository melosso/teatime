using System.Text.RegularExpressions;

namespace Teatime.Models;

public sealed partial record Post(
    string Slug,
    string Path,
    string Title,
    DateTime Date,
    DateTime? Updated,
    IReadOnlyList<string> Tags,
    string Excerpt,
    string HtmlContent,
    string? Description,
    IReadOnlyList<HeadingInfo> Headings,
    int ReadingMinutes,
    string? Cover,
    string? AuthorId,
    string? CoverWidth = null)
{
    private static readonly HashSet<string> AllowedCoverClasses =
        new(StringComparer.OrdinalIgnoreCase) { "natural", "plain", "wide", "full" };

    public static Post FromPage(DocumentationPage page)
    {
        var slug = page.Slug is { Length: > 0 } s ? Models.PagePath.SlugifySegment(s) : LastSegment(page.Path);
        var date = page.Date ?? page.LastModified ?? DateTime.MinValue;
        var excerpt = page.Summary is { Length: > 0 } summary ? summary : FirstParagraph(page.HtmlContent);
        var words = WordCount(page.HtmlContent);
        var (cover, coverWidth) = SplitCover(page.Cover);

        return new Post(
            Slug: slug,
            Path: page.Path,
            Title: page.Title,
            Date: date,
            Updated: page.LastModified is { } u && u.Date != date.Date ? u : null,
            Tags: page.Tags ?? [],
            Excerpt: excerpt,
            HtmlContent: page.HtmlContent,
            Description: page.Description,
            Headings: page.Headings ?? [],
            ReadingMinutes: Math.Max(1, (int)Math.Ceiling(words / 200.0)),
            Cover: cover,
            AuthorId: page.Author,
            CoverWidth: coverWidth);
    }

    public string Url => $"posts/{Slug}";

    private static (string? Cover, string? Width) SplitCover(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (raw, null);
        var m = CoverAttrRegex().Match(raw);
        if (!m.Success) return (raw.Trim(), null);
        var cls = m.Groups[2].Value;
        return (m.Groups[1].Value.Trim(), AllowedCoverClasses.Contains(cls) ? cls.ToLowerInvariant() : null);
    }

    private static string LastSegment(string path)
    {
        var i = path.LastIndexOf('/');
        return i >= 0 ? path[(i + 1)..] : path;
    }

    private static string FirstParagraph(string html)
    {
        var match = ParagraphRegex().Match(html);
        if (!match.Success) return string.Empty;
        var text = TagRegex().Replace(match.Groups[1].Value, string.Empty).Trim();
        return System.Net.WebUtility.HtmlDecode(text);
    }

    private static int WordCount(string html)
    {
        var text = TagRegex().Replace(html, " ");
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [GeneratedRegex(@"^(.*?)\s*\{\.([a-z-]+)\}\s*$")]
    private static partial Regex CoverAttrRegex();

    [GeneratedRegex(@"<p>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();
}
