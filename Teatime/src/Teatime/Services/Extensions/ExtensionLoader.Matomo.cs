using System.Text;
using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static ActiveExtension? BuildMatomo(MatomoOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out var origin))
            return Reject("matomo", "\"url\" must be an absolute http(s) URL", rejects);

        var siteId = Coalesce(options.SiteId, options.SiteIdAlias);
        if (siteId is null || !siteId.All(char.IsAsciiDigit))
            return Reject("matomo", "\"siteId\" must be the numeric Matomo site id", rejects);

        var init = new StringBuilder("var _paq=window._paq=window._paq||[];");
        if (options.DisableCookies)
            init.Append("_paq.push(['disableCookies']);");
        init.Append("_paq.push(['trackPageView']);_paq.push(['enableLinkTracking']);")
            .Append($"_paq.push(['setTrackerUrl','{baseUrl}/matomo.php']);")
            .Append($"_paq.push(['setSiteId','{siteId}']);");

        // The queue must exist before matomo.js runs, so the inline tag is emitted first.
        return new ActiveExtension("matomo",
            [
                new ExtensionScript(Inline: init.ToString()),
                new ExtensionScript(Src: $"{baseUrl}/matomo.js", Async: true, Defer: true)
            ],
            [origin]);
    }
}
