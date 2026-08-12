using System.Text;
using Teatime.Models;
using Teatime.Services.Layout;

namespace Teatime.Services.Rendering;

public static class ArchiveRenderer
{
    private static readonly string[] DefaultColumns = ["date", "title"];

    public static string Build(
        IReadOnlyList<(int Year, IReadOnlyList<Post> Posts)> years,
        string basePath,
        IReadOnlyList<string>? columns = null,
        AuthorService? authors = null)
    {
        var sb = new StringBuilder();
        var l = Localization.Current;
        sb.Append("<h1 class=\"list-heading\">").Append(LayoutProvider.HtmlEncode(l.ArchiveHeading)).Append("</h1>");
        sb.Append("<p class=\"list-intro\">").Append(LayoutProvider.HtmlEncode(l.ArchiveIntro)).Append("</p>");
        sb.Append(BuildYears(years, basePath, columns, authors));
        return sb.ToString();
    }

    public static string BuildYears(
        IReadOnlyList<(int Year, IReadOnlyList<Post> Posts)> years,
        string basePath,
        IReadOnlyList<string>? columns = null,
        AuthorService? authors = null)
    {
        if (years.Count == 0)
            return $"<p class=\"list-empty\">{LayoutProvider.HtmlEncode(Localization.Current.EmptyNoPosts)}</p>";

        var active = Resolve(columns);
        var sb = new StringBuilder();
        foreach (var (year, posts) in years)
        {
            sb.Append("<section class=\"archive-year\"><h2>").Append(year).Append("</h2><ul class=\"archive-list\">");
            foreach (var post in posts)
            {
                sb.Append("<li>");
                foreach (var column in active)
                    AppendColumn(sb, column, post, basePath, authors);
                sb.Append("</li>");
            }
            sb.Append("</ul></section>");
        }
        return sb.ToString();
    }

    private static IReadOnlyList<string> Resolve(IReadOnlyList<string>? columns)
    {
        if (columns is null || columns.Count == 0)
            return DefaultColumns;

        var kept = columns
            .Where(c => c is not null)
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => c is "date" or "title" or "author" or "authorimage")
            .ToArray();

        // A list naming nothing recognised must not render rows with no title.
        return kept.Length == 0 ? DefaultColumns : kept;
    }

    private static void AppendColumn(StringBuilder sb, string column, Post post, string basePath, AuthorService? authors)
    {
        switch (column)
        {
            case "date":
                sb.Append("<time datetime=\"").Append(DateFormatter.Iso(post.Date)).Append("\">")
                  .Append(DateFormatter.Current.MonthDay(post.Date)).Append("</time>");
                break;

            case "title":
                sb.Append("<a class=\"archive-title\" href=\"").Append(UrlPaths.Href(basePath, post.Url)).Append("\">")
                  .Append(LayoutProvider.HtmlEncode(post.Title)).Append("</a>");
                break;

            case "author":
                if (Lookup(post, authors) is { } named)
                    sb.Append("<span class=\"archive-author\">").Append(LayoutProvider.HtmlEncode(named.Name)).Append("</span>");
                break;

            case "authorimage":
                if (Lookup(post, authors) is { } pictured)
                    sb.Append("<span class=\"archive-author-avatar\" title=\"").Append(LayoutProvider.HtmlEncode(pictured.Name)).Append("\">")
                      .Append(PostListRenderer.Avatar(pictured.Name, pictured.Image, basePath))
                      .Append("</span>");
                break;
        }
    }

    /// <summary>An id with no author file shows as written, same as a byline, so the column never goes ragged.</summary>
    private static Author? Lookup(Post post, AuthorService? authors) =>
        authors?.GetById(post.AuthorId)
        ?? (post.AuthorId is { Length: > 0 } raw
            ? new Author(raw, string.Empty, raw.Trim(), null, null, string.Empty, Hidden: true)
            : null);
}
