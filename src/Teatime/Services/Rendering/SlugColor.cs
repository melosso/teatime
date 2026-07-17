using System.Text;

namespace Teatime.Services.Rendering;

/// <summary>Deterministic tint for posts without a cover image. FNV-1a, not
/// <c>string.GetHashCode</c>, which is randomized per process and would desync the
/// static export from the live server.</summary>
public static class SlugColor
{
    private const uint HueCount = 360;

    public static int HueFor(string? slug)
    {
        var key = slug is { Length: > 0 } ? slug : "teatime";
        return (int)(Fnv1a(key) % HueCount);
    }

    private static uint Fnv1a(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }
        return hash;
    }
}
