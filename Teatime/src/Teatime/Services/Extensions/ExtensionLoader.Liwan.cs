using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static ActiveExtension? BuildLiwan(LiwanOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out var origin))
            return Reject("liwan", "\"url\" must be an absolute http(s) URL", rejects);

        var entity = Coalesce(options.Entity);
        if (entity is null || !entity.All(IsEntityChar))
            return Reject("liwan", "\"entity\" must be the entity id configured in Liwan", rejects);

        // The tracker is served from the docs origin, so data-api names the Liwan instance explicitly
        // rather than falling back to the page's own domain.
        return new ActiveExtension("liwan",
            [
                new ExtensionScript(
                    Src: $"{baseUrl}/tracker.js",
                    Attributes:
                    [
                        new KeyValuePair<string, string>("type", "module"),
                        new KeyValuePair<string, string>("data-entity", entity),
                        new KeyValuePair<string, string>("data-api", $"{baseUrl}/api/event")
                    ])
            ],
            [origin]);
    }

    private static bool IsEntityChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.';
}
