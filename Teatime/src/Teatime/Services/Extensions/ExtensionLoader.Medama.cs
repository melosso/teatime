using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static ActiveExtension? BuildMedama(MedamaOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out var origin))
            return Reject("medama", "\"url\" must be an absolute http(s) URL", rejects);

        return new ActiveExtension("medama",
            [new ExtensionScript(Src: $"{baseUrl}/script.js", Defer: true)],
            [origin]);
    }
}
