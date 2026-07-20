using System.Text;
using System.Text.Json;
using Teatime.Models;

namespace Teatime.Services.Extensions;

/// <summary>
/// Reads <c>content/extensions.json</c> and verifies every enabled extension. An extension only
/// becomes active when its settings pass validation; anything else is dropped with a warning, so a
/// half-filled config can never emit a broken tracker.
/// </summary>
public static class ExtensionLoader
{
    public const string FileName = "extensions.json";

    private const string PlausibleDefaultUrl = "https://plausible.io";
    private const string PlausibleDefaultScript = "script.js";

    private const string DefaultBeaconPermission = "newsletter";
    private const string DefaultBeaconKeyHeader = "X-Api-Key";

    // Mirrors the language codes Beacon accepts on /api/tokens/generate.
    private static readonly string[] BeaconLanguages = ["en", "de", "fr", "nl", "pl", "es"];

    // Characters that would break out of the HTML attribute or JS string literal the URL lands in.
    private static readonly char[] UnsafeUrlChars = ['\'', '"', '<', '>', '\\', ' '];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static ExtensionSet Load(string docsPath, ILogger logger)
    {
        var path = Path.Combine(docsPath, FileName);
        if (!File.Exists(path))
            return ExtensionSet.Empty;

        ExtensionsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ExtensionsFile>(File.ReadAllText(path), JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "{File} could not be read; all extensions stay disabled", FileName);
            return ExtensionSet.Empty;
        }

        if (file?.Extensions is not { } section)
            return ExtensionSet.Empty;

        var rejects = new Rejections(logger);

        var active = new List<ActiveExtension>();
        AddIfVerified(active, BuildMatomo(section.Matomo, rejects));
        AddIfVerified(active, BuildPlausible(section.Plausible, rejects));
        AddIfVerified(active, BuildMedama(section.Medama, rejects));
        AddIfVerified(active, BuildGoatCounter(section.GoatCounter, rejects));

        var newsletter = ResolveNewsletter(section, rejects);
        var comments = BuildRemark42(section.Remark42, rejects);

        return new ExtensionSet(active, newsletter, rejects.Names, comments);
    }

    /// <summary>
    /// Picks the one newsletter back end. Enabling several is treated as a config error rather than
    /// silently favouring one, so every candidate is dropped and named in the log.
    /// </summary>
    private static INewsletterProvider? ResolveNewsletter(ExtensionsSection section, Rejections rejects)
    {
        var enabled = new List<string>(3);
        if (section.Beacon is { Enabled: true }) enabled.Add("beacon");
        if (section.Listmonk is { Enabled: true }) enabled.Add("listmonk");
        if (section.Mailchimp is { Enabled: true }) enabled.Add("mailchimp");

        if (enabled.Count > 1)
        {
            foreach (var name in enabled)
                rejects.Add(name, $"only one newsletter extension can be enabled, found {string.Join(", ", enabled)}");
            return null;
        }

        return BuildBeacon(section.Beacon, rejects)
            ?? BuildListmonk(section.Listmonk, rejects)
            ?? (INewsletterProvider?)BuildMailchimp(section.Mailchimp, rejects);
    }

    private static void AddIfVerified(List<ActiveExtension> active, ActiveExtension? extension)
    {
        if (extension is not null)
            active.Add(extension);
    }

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

    private static MailchimpProvider? BuildMailchimp(MailchimpOptions? options, Rejections rejects)
    {
        if (options is not { Enabled: true })
            return null;

        var listId = Coalesce(options.ListId);
        if (listId is null || !listId.All(char.IsAsciiLetterOrDigit))
        {
            rejects.Add("mailchimp", "\"listId\" must be a Mailchimp audience id");
            return null;
        }

        var raw = Coalesce(options.ApiKey);
        if (raw is null || !raw.StartsWith("${", StringComparison.Ordinal))
        {
            // The key is account-wide with no scoped variant, so a literal in the content folder is refused outright.
            rejects.Add("mailchimp", "\"apiKey\" has to be a ${ENV_VAR} reference, never a literal key");
            return null;
        }

        var apiKey = ResolveSecret(raw);
        if (apiKey is null)
        {
            rejects.Add("mailchimp", "the environment variable \"apiKey\" points at is not set");
            return null;
        }

        // Mailchimp keys end in -us21 and that suffix names the data centre the account lives in.
        var dataCentre = apiKey.Split('-')[^1];
        if (dataCentre == apiKey || dataCentre.Length == 0 || !dataCentre.All(char.IsAsciiLetterOrDigit))
        {
            rejects.Add("mailchimp", "the API key is missing its trailing data centre suffix, such as -us21");
            return null;
        }

        var status = (Coalesce(options.Status) ?? "pending").ToLowerInvariant();
        if (status is not ("pending" or "subscribed"))
        {
            rejects.Add("mailchimp", "\"status\" must be either pending or subscribed");
            return null;
        }

        return new MailchimpProvider(
            Endpoint: new Uri($"https://{dataCentre}.api.mailchimp.com/3.0/lists/{listId}/members"),
            ApiKey: apiKey,
            Status: status,
            CollectName: options.CollectName);
    }

    /// <summary>Expands a <c>${ENV_VAR}</c> placeholder so the key can live outside the content folder.</summary>
    private static string? ResolveSecret(string? raw)
    {
        if (raw is null)
            return null;

        if (!raw.StartsWith("${", StringComparison.Ordinal))
            return raw;

        // A half-written placeholder is a typo, never a key.
        if (!raw.EndsWith('}'))
            return null;

        var name = raw[2..^1].Trim();
        var value = name.Length == 0 ? null : Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Collects every enabled-but-rejected extension so startup can report them in one line.</summary>
    private sealed class Rejections(ILogger logger)
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Names => _names;

        public void Add(string name, string reason)
        {
            logger.LogWarning("Extension {Extension} is enabled but was not activated: {Reason}", name, reason);
            _names.Add(name);
        }
    }

    private static ActiveExtension? Reject(string name, string reason, Rejections rejects)
    {
        rejects.Add(name, reason);
        return null;
    }

    /// <summary>Splits a configured URL into its origin (for CSP) and its trailing-slash-free base (for script URLs).</summary>
    internal static bool TryBaseUrl(string? raw, out string baseUrl, out string origin)
    {
        baseUrl = origin = string.Empty;

        var trimmed = Coalesce(raw);
        if (trimmed is null || trimmed.IndexOfAny(UnsafeUrlChars) >= 0)
            return false;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https"))
            return false;

        if (uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            return false;

        origin = uri.GetLeftPart(UriPartial.Authority);
        baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return baseUrl.Length > 0;
    }

    private static string? Coalesce(params string?[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        return null;
    }

    private static bool IsDomainChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or ',';

    private static bool IsScriptNameChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_';

    private static bool IsBucketChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';
}
