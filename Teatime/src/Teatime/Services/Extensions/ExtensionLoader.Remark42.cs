using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static Remark42Provider? BuildRemark42(Remark42Options? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out var origin))
        {
            rejects.Add("remark42", "\"url\" must be an absolute http(s) URL");
            return null;
        }

        var siteId = Coalesce(options.SiteId) ?? "remark";
        if (!siteId.All(IsBucketChar))
        {
            rejects.Add("remark42", "\"siteId\" must match the SITE value your Remark42 runs with");
            return null;
        }

        var theme = (Coalesce(options.Theme) ?? "auto").ToLowerInvariant();
        if (theme is not ("light" or "dark" or "auto"))
        {
            rejects.Add("remark42", "\"theme\" must be light, dark or auto");
            return null;
        }

        var locale = Coalesce(options.Locale)?.ToLowerInvariant();
        if (locale is not null && !locale.All(c => char.IsAsciiLetter(c) || c == '-'))
        {
            rejects.Add("remark42", "\"locale\" must be a language code such as nl");
            return null;
        }

        return new Remark42Provider(
            Origin: origin,
            BaseUrl: baseUrl,
            SiteId: siteId,
            Theme: theme,
            Locale: locale,
            MaxShownComments: Math.Clamp(options.MaxShownComments, 1, 500));
    }
}
