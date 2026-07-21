using System.Text.Json;
using Teatime.Models;

namespace Teatime.Services.Extensions;

/// <summary>
/// Reads <c>content/extensions.json</c> and verifies every enabled extension. An extension only
/// becomes active when its settings pass validation; anything else is dropped with a warning, so a
/// half-filled config can never emit a broken tracker. Each extension's own verification lives in a
/// sibling <c>ExtensionLoader.&lt;Name&gt;.cs</c> partial.
/// </summary>
public static partial class ExtensionLoader
{
    public const string FileName = "extensions.json";

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
        AddIfVerified(active, BuildLiwan(section.Liwan, rejects));

        var newsletter = ResolveNewsletter(section, rejects);
        var comments = BuildRemark42(section.Remark42, rejects);

        return new ExtensionSet(active, newsletter, rejects.Names, comments);
    }

    /// <summary>
    /// Picks the one newsletter back end. Enabling several is treated as a config error rather than silently favouring one, so every candidate is dropped and named in the log.
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

    private static bool IsBucketChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';
}
