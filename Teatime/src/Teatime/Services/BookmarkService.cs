using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Serialization;
using Teatime.Services.Rendering;

namespace Teatime.Services;

/// <summary>Background URL-to-card resolver with SSRF protection and local asset caching.</summary>
public sealed partial class BookmarkService : IDisposable
{
    private const long MaxHtmlBytes = 2 * 1024 * 1024;
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly string _bookmarksDir;
    private readonly string _indexPath;
    private readonly string _basePathSegment;
    private readonly ILogger<BookmarkService> _logger;

    private readonly ConcurrentDictionary<string, BookmarkCacheEntry> _cache = new(StringComparer.Ordinal);
    // Tracks every attempted URL so a failed fetch is not retried in a loop.
    private readonly ConcurrentDictionary<string, byte> _attempted = new(StringComparer.Ordinal);
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _shutdownCts = new();

    private volatile BookmarkOptions _options = new();
    private bool _disposed;

    /// <summary>Invoked after a URL resolves so the content layer can re-render pages with the new card.</summary>
    public Action? RebuildRequested { get; set; }

    public BookmarkService(DocsOptions docsOptions, IWebHostEnvironment env, ILogger<BookmarkService> logger)
    {
        _logger = logger;
        _basePathSegment = docsOptions.BasePath?.TrimEnd('/') ?? "";
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _bookmarksDir = Path.Combine(webRoot, "bookmarks");
        _indexPath = Path.Combine(_bookmarksDir, "index.json");

        LoadCache();
        if (!docsOptions.IsStaticExport)
            _ = ConsumeAsync(_shutdownCts.Token);
    }

    /// <summary>Applied on every content rebuild from <c>config.json</c>.</summary>
    public void Configure(BookmarkOptions? options) => _options = options ?? new BookmarkOptions();

    /// <summary>Swaps resolved cards into placeholders; queues unresolved URLs and keeps their fallback link.</summary>
    public string Render(string html)
    {
        if (!_options.Enabled || !html.Contains("bookmark-embed", StringComparison.Ordinal))
            return html;

        return PlaceholderRegex().Replace(html, match =>
        {
            var url = WebUtility.HtmlDecode(match.Groups[1].Value);

            if (_cache.TryGetValue(url, out var entry))
                return BookmarkCardRenderer.Render(entry, _basePathSegment);

            QueueResolve(url);
            return match.Value;
        });
    }

    private void QueueResolve(string url)
    {
        if (_attempted.TryAdd(url, 0))
            _queue.Writer.TryWrite(url);
    }

    /// <summary>Drains the queue synchronously. Used by the static exporter so every card is resolved before crawling.</summary>
    public async Task ResolveAllPendingAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        while (_queue.Reader.TryRead(out var url))
            await ResolveAsync(url, cancellationToken);
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var url in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                var resolved = await ResolveAsync(url, cancellationToken);
                if (resolved)
                    RebuildRequested?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bookmark resolver stopped unexpectedly");
        }
    }

    private async Task<bool> ResolveAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowedHost(uri.Host))
                return false;

            using var client = CreateGuardedClient();

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var finalUri = response.RequestMessage?.RequestUri ?? uri;
            var html = await ReadCappedStringAsync(response, MaxHtmlBytes, cancellationToken);

            var entry = await BuildEntryAsync(client, finalUri, html, cancellationToken);
            _cache[url] = entry;
            await SaveCacheAsync(cancellationToken);
            _logger.LogInformation("Resolved bookmark {Url}", url);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not resolve bookmark {Url}; leaving a plain link", url);
            return false;
        }
    }

    private async Task<BookmarkCacheEntry> BuildEntryAsync(
        HttpClient client, Uri baseUri, string html, CancellationToken cancellationToken)
    {
        var title = FindMeta(html, "og:title") ?? FindTitleTag(html) ?? baseUri.Host;
        var description = FindMeta(html, "og:description") ?? FindMeta(html, "description");
        var publisher = FindMeta(html, "og:site_name") ?? baseUri.Host;
        var author = FindMeta(html, "author") ?? FindMeta(html, "article:author");

        var iconSource = FindIconHref(html) ?? "/favicon.ico";
        var iconPath = await DownloadImageAsync(client, baseUri, iconSource, "icon", cancellationToken);
        var thumbnailPath = await DownloadImageAsync(client, baseUri, FindMeta(html, "og:image"), "thumb", cancellationToken);

        return new BookmarkCacheEntry(
            Url: baseUri.ToString(),
            Title: Truncate(title, 200) ?? baseUri.Host,
            Description: Truncate(description, 300),
            IconPath: iconPath,
            ThumbnailPath: thumbnailPath,
            Author: Truncate(author, 100),
            Publisher: publisher);
    }

    private async Task<string?> DownloadImageAsync(
        HttpClient client, Uri baseUri, string? source, string kind, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (!Uri.TryCreate(baseUri, source, out var imageUri)
            || (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps)
            || !IsAllowedHost(imageUri.Host))
            return null;

        try
        {
            using var response = await client.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var extension = ImageExtension(contentType);
            if (extension is null)
                return null;

            var bytes = await ReadCappedBytesAsync(response, MaxImageBytes, cancellationToken);
            if (bytes.Length == 0)
                return null;

            var name = $"{kind}-{ShortHash(imageUri.ToString())}{extension}";
            Directory.CreateDirectory(_bookmarksDir);
            await File.WriteAllBytesAsync(Path.Combine(_bookmarksDir, name), bytes, cancellationToken);
            return $"/bookmarks/{name}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not download bookmark image {Url}", imageUri);
            return null;
        }
    }

    // SSRF guard: validates the IP of every connection (and redirect hop), so we never open a socket
    // to an internal address even under DNS rebinding.
    private HttpClient CreateGuardedClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30)),
            ConnectCallback = ConnectSafelyAsync
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30)),
            DefaultRequestHeaders =
            {
                { "User-Agent", "TeatimeBookmarks/1.0 (+https://github.com/melosso/teatime)" },
                { "Accept", "text/html,application/xhtml+xml,image/*" }
            }
        };
    }

    private static async ValueTask<Stream> ConnectSafelyAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

        var target = Array.Find(addresses, a => !IsBlockedAddress(a))
            ?? throw new IOException($"Refusing to connect to {host}: no allowed address");

        if (IsBlockedAddress(target))
            throw new IOException($"Refusing to connect to a private or loopback address for {host}");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(target, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsBlockedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6Multicast)
                return true;
            var v6 = address.GetAddressBytes();
            return (v6[0] & 0xfe) == 0xfc;
        }

        var b = address.GetAddressBytes();
        return b[0] switch
        {
            0 or 10 or 127 => true,
            169 when b[1] == 254 => true,
            172 when b[1] is >= 16 and <= 31 => true,
            192 when b[1] == 168 => true,
            100 when b[1] is >= 64 and <= 127 => true,
            >= 224 => true,
            _ => false
        };
    }

    private bool IsAllowedHost(string host)
    {
        if (Matches(_options.DenyHosts, host))
            return false;
        return _options.AllowHosts is not { Count: > 0 } allow || Matches(allow, host);

        static bool Matches(List<string>? hosts, string host) =>
            hosts is not null && hosts.Any(h =>
                host.Equals(h, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith($".{h}", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindMeta(string html, string key)
    {
        foreach (Match tag in MetaTagRegex().Matches(html))
        {
            var name = Attribute(tag.Value, "property") ?? Attribute(tag.Value, "name");
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                continue;

            var content = Attribute(tag.Value, "content");
            if (!string.IsNullOrWhiteSpace(content))
                return WebUtility.HtmlDecode(content).Trim();
        }
        return null;
    }

    private static string? FindTitleTag(string html)
    {
        var match = TitleTagRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    private static string? FindIconHref(string html)
    {
        foreach (Match tag in LinkTagRegex().Matches(html))
        {
            var rel = Attribute(tag.Value, "rel");
            if (rel is null || !rel.Contains("icon", StringComparison.OrdinalIgnoreCase))
                continue;

            var href = Attribute(tag.Value, "href");
            if (!string.IsNullOrWhiteSpace(href))
                return WebUtility.HtmlDecode(href).Trim();
        }
        return null;
    }

    private static string? Attribute(string tag, string name)
    {
        var match = Regex.Match(
            tag,
            $"\\b{name}\\s*=\\s*(\"([^\"]*)\"|'([^']*)')",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
            return null;
        return match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
    }

    private void LoadCache()
    {
        try
        {
            if (!File.Exists(_indexPath))
                return;
            var json = File.ReadAllText(_indexPath);
            var doc = JsonSerializer.Deserialize(json, TeatimeJsonContext.Default.BookmarkCache);
            if (doc is null)
                return;
            foreach (var entry in doc.Entries)
            {
                _cache[entry.Url] = entry;
                _attempted.TryAdd(entry.Url, 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read bookmark cache; starting empty");
        }
    }

    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private async Task SaveCacheAsync(CancellationToken cancellationToken)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_bookmarksDir);
            var doc = new BookmarkCache(_cache.Values.OrderBy(e => e.Url, StringComparer.Ordinal).ToList());
            var json = JsonSerializer.Serialize(doc, TeatimeJsonContext.Default.BookmarkCache);
            await File.WriteAllTextAsync(_indexPath, json, cancellationToken);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static async Task<string> ReadCappedStringAsync(HttpResponseMessage response, long cap, CancellationToken ct)
    {
        var bytes = await ReadCappedBytesAsync(response, cap, ct);
        return Encoding.UTF8.GetString(bytes);
    }

    private static async Task<byte[]> ReadCappedBytesAsync(HttpResponseMessage response, long cap, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > cap)
                break;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static string? ImageExtension(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/svg+xml" => ".svg",
        "image/x-icon" or "image/vnd.microsoft.icon" => ".ico",
        _ => null
    };

    private static string ShortHash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max].TrimEnd() + "…";
    }

    [GeneratedRegex(@"<div class=""bookmark-embed"" data-bookmark-url=""([^""]+)"">.*?</div>", RegexOptions.Singleline)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MetaTagRegex();

    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkTagRegex();

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleTagRegex();

    public void Dispose()
    {
        if (_disposed)
            return;
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _saveLock.Dispose();
        _disposed = true;
    }
}
