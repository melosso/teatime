using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
    private const string DefaultBeaconPermission = "newsletter";
    private const string DefaultBeaconKeyHeader = "X-Api-Key";

    // Mirrors the language codes Beacon accepts on /api/tokens/generate.
    private static readonly string[] BeaconLanguages = ["en", "de", "fr", "nl", "pl", "es"];

    private static BeaconProvider? BuildBeacon(BeaconOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        if (!TryBaseUrl(options.Url, out var baseUrl, out _))
        {
            rejects.Add("beacon", "\"url\" must be an absolute http(s) URL");
            return null;
        }

        var bucket = Coalesce(options.Bucket);
        if (bucket is null || bucket.Length > 100 || !bucket.All(IsBucketChar))
        {
            rejects.Add("beacon", "\"bucket\" must be a Beacon bucket name such as newsletter_en");
            return null;
        }

        var permission = Coalesce(options.Permission) ?? DefaultBeaconPermission;
        if (permission.Length > 50 || !permission.All(IsBucketChar))
        {
            rejects.Add("beacon", "\"permission\" must be a Beacon permission name such as newsletter");
            return null;
        }

        var apiKey = ResolveSecret(Coalesce(options.ApiKey));
        if (apiKey is null)
        {
            rejects.Add("beacon", "\"apiKey\" is empty, or the environment variable it points at is not set");
            return null;
        }

        var header = Coalesce(options.ApiKeyHeader) ?? DefaultBeaconKeyHeader;
        if (!header.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            rejects.Add("beacon", "\"apiKeyHeader\" must be a plain HTTP header name");
            return null;
        }

        var language = (Coalesce(options.Language) ?? "en").ToLowerInvariant();
        if (!BeaconLanguages.Contains(language))
        {
            rejects.Add("beacon", $"\"language\" must be one of: {string.Join(", ", BeaconLanguages)}");
            return null;
        }

        return new BeaconProvider(
            Endpoint: new Uri($"{baseUrl}/api/tokens/generate"),
            Bucket: bucket,
            Permission: permission,
            ApiKeyHeader: header,
            ApiKey: apiKey,
            Language: language,
            ExpiryDays: options.ExpiryDays,
            CollectName: options.CollectName,
            SkipPermissionUpdate: options.SkipPermissionUpdate,
            CustomFields: options.CustomFields is { Count: > 0 } fields ? fields : null);
    }
}
