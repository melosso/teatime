using System.Text.Json.Serialization;

namespace Teatime.Models;

/// <summary>Root of the optional <c>content/extensions.json</c>, sitting next to <c>config.json</c>.</summary>
public sealed class ExtensionsFile
{
    public ExtensionsSection? Extensions { get; set; }
}

/// <summary>Every built-in extension, keyed by name. Each one stays off until it is enabled and its settings verify.</summary>
public sealed class ExtensionsSection
{
    public MatomoOptions? Matomo { get; set; }
    public PlausibleOptions? Plausible { get; set; }
    public MedamaOptions? Medama { get; set; }
    public GoatCounterOptions? GoatCounter { get; set; }
    public LiwanOptions? Liwan { get; set; }
    public Remark42Options? Remark42 { get; set; }
    public BeaconOptions? Beacon { get; set; }
    public ListmonkOptions? Listmonk { get; set; }
    public MailchimpOptions? Mailchimp { get; set; }
}

/// <summary>Self-hosted Matomo analytics.</summary>
public sealed class MatomoOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of the Matomo install, e.g. <c>https://analytics.example.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Numeric Matomo site id.</summary>
    public string? SiteId { get; set; }

    /// <summary>Snake-case alias for <see cref="SiteId"/>.</summary>
    [JsonPropertyName("site_id")]
    public string? SiteIdAlias { get; set; }

    /// <summary>Cookieless tracking. On by default, so the tracker needs no consent banner.</summary>
    public bool DisableCookies { get; set; } = true;
}

/// <summary>Plausible analytics, cloud or self-hosted.</summary>
public sealed class PlausibleOptions
{
    public bool Enabled { get; set; }

    /// <summary>Site domain registered in Plausible. Comma-separated for shared scripts.</summary>
    public string? Domain { get; set; }

    /// <summary>Base URL of the Plausible install. Defaults to <c>https://plausible.io</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Script variant under <c>/js/</c>, e.g. <c>script.outbound-links.js</c>. Defaults to <c>script.js</c>.</summary>
    public string? Script { get; set; }
}

/// <summary>
/// Beacon consent management, used here for newsletter sign-up. The API key stays server side:
/// the browser only ever talks to Teatime's own <c>/api/subscribe</c>.
/// </summary>
public sealed class BeaconOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of the Beacon API, e.g. <c>https://beacon-api.melosso.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Beacon bucket the sign-up writes to, e.g. <c>newsletter_en</c>.</summary>
    public string? Bucket { get; set; }

    /// <summary>Permission recorded inside the bucket. Defaults to <c>newsletter</c>.</summary>
    public string? Permission { get; set; }

    /// <summary>API key, or <c>${ENV_VAR}</c> to read one from the environment.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Header carrying the key. Defaults to <c>X-Api-Key</c>.</summary>
    public string? ApiKeyHeader { get; set; }

    /// <summary>Language for Beacon's own confirmation mail and preference page. Defaults to <c>en</c>.</summary>
    public string? Language { get; set; }

    /// <summary>Lifetime of the preference-management token Beacon issues.</summary>
    public int? ExpiryDays { get; set; }

    /// <summary>Adds an optional name field to the form.</summary>
    public bool CollectName { get; set; }

    /// <summary>Extra values stored alongside each consent record, e.g. <c>{"source": "blog"}</c>.</summary>
    public Dictionary<string, string>? CustomFields { get; set; }

    /// <summary>
    /// Leaves an existing consent record untouched, so re-submitting an address cannot flip
    /// someone's earlier opt-out back to opted-in. On by default, and worth keeping that way.
    /// </summary>
    public bool SkipPermissionUpdate { get; set; } = true;
}

/// <summary>
/// Self-hosted Listmonk. Sign-ups go to the public subscription endpoint, which needs no credential,
/// so there is nothing here to keep secret.
/// </summary>
public sealed class ListmonkOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of your Listmonk install, e.g. <c>https://lists.example.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>UUID of the list to subscribe to. Use <see cref="ListUuids"/> for several at once.</summary>
    public string? ListUuid { get; set; }

    /// <summary>UUIDs of every list to subscribe to.</summary>
    public List<string>? ListUuids { get; set; }

    /// <summary>Adds an optional name field to the form.</summary>
    public bool CollectName { get; set; }
}

/// <summary>
/// Mailchimp. Its API key grants full account access and has no scoped variant, so <see cref="ApiKey"/>
/// is only accepted as a <c>${ENV_VAR}</c> reference and never as a literal in your content folder.
/// </summary>
public sealed class MailchimpOptions
{
    public bool Enabled { get; set; }

    /// <summary>Audience (list) id from Mailchimp.</summary>
    public string? ListId { get; set; }

    /// <summary>Always <c>${ENV_VAR}</c>. The data centre is read from the key's own suffix.</summary>
    public string? ApiKey { get; set; }

    /// <summary><c>pending</c> asks Mailchimp to send its confirmation email, <c>subscribed</c> skips it.</summary>
    public string? Status { get; set; }

    /// <summary>Adds an optional name field, sent as the FNAME merge field.</summary>
    public bool CollectName { get; set; }
}

/// <summary>Self-hosted Remark42 comments, mounted under each post.</summary>
public sealed class Remark42Options
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of your Remark42 install, e.g. <c>https://comments.example.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Site id configured in Remark42's own SITE setting. Defaults to <c>remark</c>.</summary>
    public string? SiteId { get; set; }

    /// <summary><c>light</c>, <c>dark</c>, or <c>auto</c> to follow the reader's theme. Defaults to <c>auto</c>.</summary>
    public string? Theme { get; set; }

    /// <summary>Locale for Remark42's own interface, e.g. <c>nl</c>. Defaults to the site language.</summary>
    public string? Locale { get; set; }

    /// <summary>How many comments load before the reader asks for more.</summary>
    public int MaxShownComments { get; set; } = 15;
}

/// <summary>GoatCounter analytics, self-hosted or on goatcounter.com.</summary>
public sealed class GoatCounterOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of your GoatCounter site, e.g. <c>https://you.goatcounter.com</c>.</summary>
    public string? Url { get; set; }
}

/// <summary>Self-hosted Medama analytics.</summary>
public sealed class MedamaOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of the Medama install, e.g. <c>https://medama.example.com</c>.</summary>
    public string? Url { get; set; }
}

/// <summary>Self-hosted Liwan analytics.</summary>
public sealed class LiwanOptions
{
    public bool Enabled { get; set; }

    /// <summary>Base URL of the Liwan instance, e.g. <c>https://liwan.example.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Entity id configured in Liwan for this site.</summary>
    public string? Entity { get; set; }
}
