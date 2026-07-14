using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Teatime.Services.Rendering;

public static partial class IconProvider
{
    private static readonly ConcurrentDictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static async ValueTask<string> InlineSvgAsync(string iconName, string primaryDir, string? fallbackDir = null)
    {
        if (_cache.TryGetValue(iconName, out var cached))
            return cached ?? string.Empty;

        var slug = Slugify(iconName);
        var filePath = Path.Combine(primaryDir, $"{slug}.svg");

        if (!File.Exists(filePath) && fallbackDir != null)
            filePath = Path.Combine(fallbackDir, $"{slug}.svg");

        if (!File.Exists(filePath))
        {
            _cache.TryAdd(iconName, null);
            return string.Empty;
        }

        var svg = await File.ReadAllTextAsync(filePath);
        svg = StripFillAttr().Replace(svg, "");
        svg = svg.Replace("<svg", "<svg fill=\"currentColor\" aria-hidden=\"true\"");
        _cache.TryAdd(iconName, svg);
        return svg;
    }

    public static void ClearCache() => _cache.Clear();

    private static string Slugify(string name) =>
        SlugRegex().Replace(name.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex(@"\s+fill=""[^""]*""")]
    private static partial Regex StripFillAttr();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
