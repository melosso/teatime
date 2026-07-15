using Teatime.Configuration;
using Teatime.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Teatime.Services;

public sealed class AuthorService
{
    private sealed record AuthorFrontMatter
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Image { get; init; }
    }

    private readonly DocsOptions _options;
    private readonly MarkdownService _markdown;
    private readonly ContentService _content;
    private readonly IDeserializer _yaml;
    private readonly Lock _gate = new();

    private long _cachedVersion = -1;
    private IReadOnlyDictionary<string, Author> _byId = new Dictionary<string, Author>();
    private IReadOnlyList<Author> _all = [];

    public AuthorService(DocsOptions options, MarkdownService markdown, ContentService content)
    {
        _options = options;
        _markdown = markdown;
        _content = content;
        _yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public IReadOnlyList<Author> GetAll()
    {
        Ensure();
        return _all;
    }

    public Author? GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        Ensure();
        return _byId.TryGetValue(id.Trim(), out var a) ? a : null;
    }

    public Author? GetBySlug(string slug)
    {
        Ensure();
        var s = PagePath.SlugifySegment(slug);
        return _all.FirstOrDefault(a => a.Slug == s);
    }

    private void Ensure()
    {
        var version = _content.BuildVersion;
        lock (_gate)
        {
            if (version == _cachedVersion) return;
        }

        var authors = Load();
        var byId = authors.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            _cachedVersion = version;
            _all = authors;
            _byId = byId;
        }
    }

    private List<Author> Load()
    {
        var dir = Path.Combine(Path.GetFullPath(_options.RootPath), "authors");
        var authors = new List<Author>();
        if (!Directory.Exists(dir)) return authors;

        foreach (var file in Directory.GetFiles(dir, "*.md").Order())
        {
            var text = File.ReadAllText(file);
            var fm = ParseFrontMatter(text);
            var id = (fm.Id ?? Path.GetFileNameWithoutExtension(file)).Trim().ToLowerInvariant();
            if (id.Length == 0) continue;
            var name = fm.Name ?? id;
            var bio = _markdown.ToHtml(text);
            authors.Add(new Author(id, PagePath.SlugifySegment(id), name, fm.Image, bio));
        }

        return authors
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AuthorFrontMatter ParseFrontMatter(string text)
    {
        if (!text.StartsWith("---", StringComparison.Ordinal)) return new AuthorFrontMatter();
        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0) return new AuthorFrontMatter();
        var yaml = text[3..end];
        try { return _yaml.Deserialize<AuthorFrontMatter>(yaml) ?? new AuthorFrontMatter(); }
        catch (YamlDotNet.Core.YamlException) { return new AuthorFrontMatter(); }
    }
}
