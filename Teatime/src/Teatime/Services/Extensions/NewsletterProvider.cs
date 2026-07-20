namespace Teatime.Services.Extensions;

/// <summary>
/// A verified newsletter back end. Only one can be active at a time, and nothing about it reaches the
/// browser: the endpoint and any credential stay server side behind Teatime's own /api/subscribe.
/// </summary>
public interface INewsletterProvider
{
    /// <summary>Name as written in <c>extensions.json</c>.</summary>
    string Name { get; }

    Uri Endpoint { get; }

    /// <summary>Whether the form should offer a name field.</summary>
    bool CollectName { get; }
}

/// <summary>Beacon consent management. Records consent and issues a preference-management token.</summary>
public sealed record BeaconProvider(
    Uri Endpoint,
    string ApiKeyHeader,
    string ApiKey,
    string Bucket,
    string Permission,
    string Language,
    int? ExpiryDays,
    bool CollectName,
    bool SkipPermissionUpdate,
    IReadOnlyDictionary<string, string>? CustomFields) : INewsletterProvider
{
    public string Name => "beacon";
}

/// <summary>Self-hosted Listmonk. The public subscription endpoint takes no credential at all.</summary>
public sealed record ListmonkProvider(
    Uri Endpoint,
    IReadOnlyList<string> ListUuids,
    bool CollectName) : INewsletterProvider
{
    public string Name => "listmonk";
}

/// <summary>Mailchimp. The key carries full account access, so it is only ever read from the environment.</summary>
public sealed record MailchimpProvider(
    Uri Endpoint,
    string ApiKey,
    string Status,
    bool CollectName) : INewsletterProvider
{
    public string Name => "mailchimp";

    /// <summary>True when Mailchimp is set to send its own confirmation email.</summary>
    public bool DoubleOptIn => Status == "pending";
}
