using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services.Rendering;

namespace Teatime.Services;

public sealed partial class ContentService : IHostedService, IDisposable
{
    private readonly DocsOptions _options;
    private readonly MarkdownService _markdown;
    private readonly ILogger<ContentService> _logger;
    private readonly string _basePathSegment;
    private FileSystemWatcher? _watcher;
    private FileSystemWatcher? _configWatcher;
    private readonly CancellationTokenSource _shutdownCts = new();

    // All read state lives in one immutable snapshot swapped atomically after a full build; readers never see half-built state
    private sealed record ContentSnapshot(
        IReadOnlyDictionary<string, DocumentationPage> Pages,
        Config? Config,
        SearchIndex SearchIndex);

    private static readonly ContentSnapshot EmptySnapshot = new(
        ImmutableDictionary<string, DocumentationPage>.Empty,
        null,
        new SearchIndex());

    private volatile ContentSnapshot _snapshot = EmptySnapshot;
    private string? _lastContentHash;
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private readonly Channel<FileSystemEventArgs> _fileChannel =
        Channel.CreateBounded<FileSystemEventArgs>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    private bool _disposed;

    public ContentService(
        DocsOptions options,
        MarkdownService markdown,
        ILogger<ContentService> logger)
    {
        _options = options;
        _markdown = markdown;
        _logger = logger;
        _basePathSegment = _options.BasePath?.Trim('/').ToLowerInvariant() ?? "";
    }

    public Config? SiteConfig => _snapshot.Config;
    public long BuildVersion { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RebuildAsync(cancellationToken);

        if (_options.EnableHotReload)
        {
            var docsPath = Path.GetFullPath(_options.RootPath);
            if (!Directory.Exists(docsPath))
                Directory.CreateDirectory(docsPath);

            _watcher = new FileSystemWatcher(docsPath)
            {
                IncludeSubdirectories = true,
                Filter = "*.md",
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _configWatcher = new FileSystemWatcher(docsPath)
            {
                Filter = "config.json",
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += OnFileChanged;
            _configWatcher.Created += OnFileChanged;
            _configWatcher.Deleted += OnFileChanged;
            _configWatcher.Renamed += OnFileRenamed;

            _ = FileWatcherConsumerAsync(_shutdownCts.Token);

            _logger.LogInformation("Hot reload enabled, watching {DocsPath}", docsPath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCts.Cancel();
        _watcher?.Dispose();
        _configWatcher?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _watcher?.Dispose();
        _configWatcher?.Dispose();
        _buildLock.Dispose();
        _disposed = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _fileChannel.Writer.TryWrite(e);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _fileChannel.Writer.TryWrite(e);
    }

    private async Task FileWatcherConsumerAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var __ in _fileChannel.Reader.ReadAllAsync(ct))
            {
                await Task.Delay(300, ct);

                while (_fileChannel.Reader.TryRead(out _)) { }

                try
                {
                    await RebuildAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rebuild documentation");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File watcher consumer failed");
        }
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await _buildLock.WaitAsync(cancellationToken);
        try
        {
            await BuildAsync(cancellationToken);
        }
        finally
        {
            _buildLock.Release();
        }
    }

    // Caller must hold _buildLock; builds a complete snapshot off to the side, then swaps it in
    private async Task BuildAsync(CancellationToken cancellationToken)
    {
        IconProvider.ClearCache();
        var docsPath = Path.GetFullPath(_options.RootPath);
        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("Docs directory does not exist: {Path}", docsPath);
            return;
        }

        var config = LoadConfig(docsPath);

        // Sorted for deterministic hashing, regardless of FS enumeration order.
        var allFiles = Directory.GetFiles(docsPath, "*.md", SearchOption.AllDirectories).Order().ToArray();
        var pages = new List<DocumentationPage>();
        var pageMap = new Dictionary<string, DocumentationPage>();
        var hashInput = new StringBuilder();

        foreach (var file in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(docsPath, file);
            var pagePath = PagePath.FromFile(relativePath);

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            hashInput.Append(relativePath).Append('\0').Append(content).Append('\0');

            var defaultTitle = Path.GetFileNameWithoutExtension(relativePath);
            if (defaultTitle.Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(relativePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var dirName = Path.GetFileName(dir)!;
                    var spaced = dirName.Replace('-', ' ').Replace('_', ' ');
                    defaultTitle = spaced.Length > 0 ? char.ToUpperInvariant(spaced[0]) + spaced[1..] : dirName;
                }
                else
                {
                    defaultTitle = "Home";
                }
            }

            var normalizedRelativePath = relativePath.Replace('\\', '/');
            var parsed = _markdown.Parse(content, defaultTitle, filePath: normalizedRelativePath);

            var html = WrapTables(parsed.Html);
            var lastModified = parsed.FrontmatterDate ?? File.GetLastWriteTimeUtc(file);

            var page = new DocumentationPage(
                Path: pagePath,
                Title: parsed.Title ?? defaultTitle,
                HtmlContent: html,
                Description: parsed.Description,
                LastModified: lastModified,
                Headings: parsed.Headings,
                Layout: parsed.Layout,
                ShowLastUpdated: parsed.ShowLastUpdated,
                OriginalRelativePath: normalizedRelativePath,
                Keywords: parsed.Keywords,
                ShowPagination: parsed.ShowPagination,
                Redirect: parsed.Redirect,
                ShowToc: parsed.ShowToc,
                Date: parsed.PublishDate,
                Tags: parsed.Tags,
                Draft: parsed.Draft,
                Slug: parsed.Slug,
                Summary: parsed.Summary,
                Cover: parsed.Cover,
                Author: parsed.Author,
                Enabled: parsed.Enabled
            );

            pageMap[pagePath] = page;
            pages.Add(page);
        }

        var configPath = Path.Combine(docsPath, "config.json");
        if (File.Exists(configPath))
            hashInput.Append(await File.ReadAllTextAsync(configPath, cancellationToken));

        var contentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput.ToString())));

        var searchIndex = new SearchIndex();
        searchIndex.Build(pages);

        var snapshot = new ContentSnapshot(
            pageMap,
            config,
            searchIndex);

        _snapshot = snapshot;

        // Prevent unnecessary client reloads from spurious file events by verifying content changes!
        if (contentHash == _lastContentHash)
        {
            _logger.LogDebug("Rebuilt documentation but content is unchanged, skipping version bump");
            return;
        }

        _lastContentHash = contentHash;
        BuildVersion++;
        _logger.LogInformation("Built documentation with {PageCount} pages", pages.Count);

        LogDeadLinks(pages, pageMap);
    }

    private void LogDeadLinks(List<DocumentationPage> pages, Dictionary<string, DocumentationPage> pageMap)
    {
        var deadSources = new HashSet<string>();
        foreach (var page in pages)
        {
            foreach (Match match in HrefRegex().Matches(page.HtmlContent))
            {
                var href = match.Groups[1].Value;
                if (ShouldSkipHref(href))
                    continue;

                var resolved = ResolveHref(page.Path, href, _basePathSegment);
                if (resolved.Length == 0
                    || pageMap.ContainsKey(resolved)
                    || pageMap.ContainsKey($"pages/{resolved}")
                    || IsKnownRoute(resolved))
                    continue;

                deadSources.Add(page.Path);
            }
        }

        if (deadSources.Count > 0)
        {
            var list = string.Join(", ", deadSources.Order());
            _logger.LogWarning("Dead internal links found in: {Sources}", list);
        }
    }

    private static string ResolveHref(string pagePath, string href, string basePathSegment)
    {
        var fragIdx = href.IndexOf('#');
        var pathOnly = fragIdx >= 0 ? href[..fragIdx] : href;

        if (pathOnly.StartsWith('/'))
        {
            var abs = pathOnly.Trim('/').ToLowerInvariant();
            if (basePathSegment.Length > 0)
            {
                if (abs == basePathSegment) return "";
                if (abs.StartsWith($"{basePathSegment}/", StringComparison.Ordinal))
                    abs = abs[(basePathSegment.Length + 1)..];
            }
            return abs;
        }

        var basePath = pagePath == "index" ? "" : pagePath;
        var combined = $"{basePath}/{pathOnly}";
        var segments = new List<string>();
        foreach (var seg in combined.Split('/'))
        {
            if (seg == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
            }
            else if (seg != "." && seg != "")
                segments.Add(seg);
        }
        return string.Join("/", segments).ToLowerInvariant();
    }

    private static bool IsKnownRoute(string resolved) =>
        resolved is "tags" or "archive" or "authors" or "feed.xml" or "sitemap.xml" or "robots.txt" or "llms.txt"
        || resolved.StartsWith("tags/", StringComparison.Ordinal)
        || resolved.StartsWith("authors/", StringComparison.Ordinal)
        || resolved.StartsWith("page/", StringComparison.Ordinal);

    private static bool ShouldSkipHref(string href)
    {
        return href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("//")
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("#")
            || href == "/";
    }

    [GeneratedRegex(@"<a\s[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HrefRegex();

    public ValueTask<DocumentationPage?> GetPageAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.Trim('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            path = _options.DefaultPage ?? "index";

        _snapshot.Pages.TryGetValue(path, out var page);
        return ValueTask.FromResult(page);
    }

    public Task<IReadOnlyList<DocumentationPage>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentationPage> pages = _snapshot.Pages.Values.ToImmutableList();
        return Task.FromResult(pages);
    }

    public IReadOnlyList<SearchResult> Search(string query)
    {
        return _snapshot.SearchIndex.Search(query);
    }

    public SearchIndexExport GetSearchIndexExport()
    {
        return _snapshot.SearchIndex.ExportSnapshot();
    }

    [GeneratedRegex(@"<table[^>]*>[\s\S]*?</table>", RegexOptions.IgnoreCase)]
    private static partial Regex TableRegex();

    private static string WrapTables(string html) =>
        TableRegex().Replace(html, m => $"<div class=\"table-wrapper\">{m.Value}</div>");

    private static Config? LoadConfig(string docsPath)
    {
        var configPath = Path.Combine(docsPath, "config.json");
        if (!File.Exists(configPath))
            return null;

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
