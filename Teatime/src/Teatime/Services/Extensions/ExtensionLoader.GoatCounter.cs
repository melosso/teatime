using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static ActiveExtension? BuildGoatCounter(GoatCounterOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out var origin))
            return Reject("goatcounter", "\"url\" must be an absolute http(s) URL", rejects);

        return new ActiveExtension("goatcounter",
            [
                new ExtensionScript(
                    Src: $"{baseUrl}/count.js",
                    Async: true,
                    Attributes: [new KeyValuePair<string, string>("data-goatcounter", $"{baseUrl}/count")])
            ],
            [origin]);
    }
}
