using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private static ListmonkProvider? BuildListmonk(ListmonkOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out _))
        {
            rejects.Add("listmonk", "\"url\" must be an absolute http(s) URL");
            return null;
        }

        var uuids = (options.ListUuids ?? [])
            .Concat(options.ListUuid is null ? [] : new[] { options.ListUuid })
            .Select(u => u.Trim())
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (uuids.Length == 0 || !uuids.All(u => Guid.TryParse(u, out _)))
        {
            rejects.Add("listmonk", "\"listUuid\" must be a list UUID from Listmonk");
            return null;
        }

        return new ListmonkProvider(
            Endpoint: new Uri($"{baseUrl}/api/public/subscription"),
            ListUuids: uuids,
            CollectName: options.CollectName);
    }
}
