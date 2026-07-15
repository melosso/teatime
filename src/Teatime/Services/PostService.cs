using Teatime.Models;

namespace Teatime.Services;

public sealed record TagInfo(string Name, string Slug, int Count);

public sealed record PostsView(
    IReadOnlyList<Post> Posts,
    IReadOnlyDictionary<string, IReadOnlyList<Post>> ByTagSlug,
    IReadOnlyList<TagInfo> Tags);

public sealed class PostService
{
    private const string PostsPrefix = "posts/";

    private readonly ContentService _content;
    private readonly bool _includeDrafts;
    private readonly Lock _gate = new();
    private long _cachedVersion = -1;
    private PostsView _cached = Empty;

    private static readonly PostsView Empty = new(
        [], new Dictionary<string, IReadOnlyList<Post>>(), []);

    public PostService(ContentService content, IHostEnvironment environment)
    {
        _content = content;
        _includeDrafts = environment.IsDevelopment();
    }

    public async Task<PostsView> GetViewAsync(CancellationToken cancellationToken = default)
    {
        var version = _content.BuildVersion;
        lock (_gate)
        {
            if (version == _cachedVersion) return _cached;
        }

        var pages = await _content.GetAllPagesAsync(cancellationToken);
        var view = Build(pages, _includeDrafts);

        lock (_gate)
        {
            _cachedVersion = version;
            _cached = view;
        }
        return view;
    }

    public async Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = PagePath.SlugifySegment(slug);
        var view = await GetViewAsync(cancellationToken);
        return view.Posts.FirstOrDefault(p => p.Slug == normalized);
    }

    public async Task<(IReadOnlyList<Post> Posts, int TotalPages)> GetPageAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var view = await GetViewAsync(cancellationToken);
        var total = Math.Max(1, (int)Math.Ceiling(view.Posts.Count / (double)pageSize));
        var slice = view.Posts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (slice, total);
    }

    public async Task<IReadOnlyList<Post>> GetByTagAsync(string tagSlug, CancellationToken cancellationToken = default)
    {
        var view = await GetViewAsync(cancellationToken);
        return view.ByTagSlug.TryGetValue(PagePath.SlugifySegment(tagSlug), out var posts) ? posts : [];
    }

    public async Task<IReadOnlyList<Post>> GetByAuthorAsync(string authorId, CancellationToken cancellationToken = default)
    {
        var view = await GetViewAsync(cancellationToken);
        return view.Posts
            .Where(p => string.Equals(p.AuthorId, authorId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<(Post? Older, Post? Newer)> GetPrevNextAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = PagePath.SlugifySegment(slug);
        var view = await GetViewAsync(cancellationToken);
        var i = 0;
        for (; i < view.Posts.Count; i++)
            if (view.Posts[i].Slug == normalized) break;
        if (i >= view.Posts.Count) return (null, null);

        var newer = i > 0 ? view.Posts[i - 1] : null;
        var older = i < view.Posts.Count - 1 ? view.Posts[i + 1] : null;
        return (older, newer);
    }

    public async Task<IReadOnlyList<(int Year, IReadOnlyList<Post> Posts)>> GetArchiveAsync(CancellationToken cancellationToken = default)
    {
        var view = await GetViewAsync(cancellationToken);
        return view.Posts
            .GroupBy(p => p.Date.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => (g.Key, (IReadOnlyList<Post>)g.ToList()))
            .ToList();
    }

    internal static PostsView Build(IReadOnlyList<DocumentationPage> pages, bool includeDrafts)
    {
        var posts = pages
            .Where(p => p.Path.StartsWith(PostsPrefix, StringComparison.Ordinal) && p.Path != "posts")
            .Where(p => includeDrafts || !p.Draft)
            .Select(Post.FromPage)
            .OrderByDescending(p => p.Date)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byTag = new Dictionary<string, List<Post>>();
        var display = new Dictionary<string, string>();
        foreach (var post in posts)
            foreach (var tag in post.Tags)
            {
                var tagSlug = PagePath.SlugifySegment(tag);
                if (tagSlug.Length == 0) continue;
                if (!byTag.TryGetValue(tagSlug, out var list))
                {
                    byTag[tagSlug] = list = [];
                    display[tagSlug] = tag;
                }
                list.Add(post);
            }

        var byTagSlug = byTag.ToDictionary(
            kv => kv.Key, kv => (IReadOnlyList<Post>)kv.Value);
        var tags = byTag
            .Select(kv => new TagInfo(display[kv.Key], kv.Key, kv.Value.Count))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PostsView(posts, byTagSlug, tags);
    }
}
