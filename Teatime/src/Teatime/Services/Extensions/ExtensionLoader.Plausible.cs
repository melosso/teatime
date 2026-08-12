using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private const string PlausibleDefaultUrl = "https://plausible.io";
    private const string PlausibleDefaultScript = "script.js";

    private static ActiveExtension? BuildPlausible(PlausibleOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        var domain = Coalesce(options.Domain);
        if (domain is null || !domain.All(IsDomainChar))
            return Reject("plausible", "\"domain\" must be the site domain registered in Plausible", rejects);

        if (!TryBaseUrl(Coalesce(options.Url) ?? PlausibleDefaultUrl, out var baseUrl, out var origin))
            return Reject("plausible", "\"url\" must be an absolute http(s) URL", rejects);

        var script = Coalesce(options.Script) ?? PlausibleDefaultScript;
        if (!script.EndsWith(".js", StringComparison.Ordinal) || !script.All(IsScriptNameChar))
            return Reject("plausible", "\"script\" must be a script file name such as script.outbound-links.js", rejects);

        return new ActiveExtension("plausible",
            [
                new ExtensionScript(
                    Src: $"{baseUrl}/js/{script}",
                    Defer: true,
                    Attributes: [new KeyValuePair<string, string>("data-domain", domain)])
            ],
            [origin]);
    }

    private static bool IsDomainChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or ',';

    private static bool IsScriptNameChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_';
}
