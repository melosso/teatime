using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Teatime.Serialization;

namespace Teatime.Services.Extensions;

/// <summary>How a sign-up attempt ended, kept coarse so nothing about the upstream call leaks to the browser.</summary>
public enum SubscribeOutcome
{
    Subscribed,
    ConfirmationSent,
    AlreadySubscribed,
    InvalidEmail,
    Disabled,
    Unavailable
}

public readonly record struct SubscribeResult(SubscribeOutcome Outcome)
{
    public bool IsSuccess => Outcome
        is SubscribeOutcome.Subscribed
        or SubscribeOutcome.ConfirmationSent
        or SubscribeOutcome.AlreadySubscribed;
}

/// <summary>
/// Sends a sign-up to whichever newsletter back end is configured. Credentials never leave the server,
/// and nothing the provider returns is passed back to the reader: Beacon's token manages a subscriber's
/// preferences, and a provider's own error text can disclose whether an address is already on a list.
/// </summary>
public sealed class NewsletterService(HttpClient http, IExtensionSource extensions, ILogger<NewsletterService> logger)
{
    public bool IsEnabled => extensions.Extensions.Newsletter is not null;

    public bool CollectsName => extensions.Extensions.Newsletter?.CollectName ?? false;

    public async Task<SubscribeResult> SubscribeAsync(string? email, string? name, CancellationToken cancellationToken)
    {
        if (extensions.Extensions.Newsletter is not { } provider)
            return new SubscribeResult(SubscribeOutcome.Disabled);

        var address = Normalize(email);
        if (address is null)
            return new SubscribeResult(SubscribeOutcome.InvalidEmail);

        var subscriberName = provider.CollectName ? Trim(name, 200) : null;

        try
        {
            using var message = BuildRequest(provider, address, subscriberName);
            using var response = await http.SendAsync(message, cancellationToken);
            return await InterpretAsync(provider, response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "The {Provider} newsletter service could not be reached", provider.Name);
            return new SubscribeResult(SubscribeOutcome.Unavailable);
        }
    }

    private static HttpRequestMessage BuildRequest(INewsletterProvider provider, string email, string? name)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);

        switch (provider)
        {
            case BeaconProvider beacon:
                message.Content = JsonContent.Create(
                    new[]
                    {
                        new BeaconTokenRequest(
                            Bucket: beacon.Bucket,
                            Email: email,
                            Name: name,
                            Permissions: new Dictionary<string, bool>(StringComparer.Ordinal) { [beacon.Permission] = true },
                            Language: beacon.Language,
                            ExpiryDays: beacon.ExpiryDays,
                            SkipPermissionUpdate: beacon.SkipPermissionUpdate,
                            CustomFields: beacon.CustomFields)
                    },
                    TeatimeJsonContext.Default.BeaconTokenRequestArray);
                message.Headers.TryAddWithoutValidation(beacon.ApiKeyHeader, beacon.ApiKey);
                break;

            case ListmonkProvider listmonk:
                message.Content = JsonContent.Create(
                    new ListmonkSubscriptionRequest(email, name, listmonk.ListUuids),
                    TeatimeJsonContext.Default.ListmonkSubscriptionRequest);
                break;

            case MailchimpProvider mailchimp:
                message.Content = JsonContent.Create(
                    new MailchimpMemberRequest(
                        email,
                        mailchimp.Status,
                        name is null ? null : new MailchimpMergeFields(name)),
                    TeatimeJsonContext.Default.MailchimpMemberRequest);
                // Mailchimp takes the key as the password of a basic credential; the user part is ignored.
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"teatime:{mailchimp.ApiKey}")));
                break;
        }

        return message;
    }

    private async Task<SubscribeResult> InterpretAsync(
        INewsletterProvider provider, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // Mailchimp reports an existing member as a 400, which is a success from the reader's side.
        if (provider is MailchimpProvider && response.StatusCode is HttpStatusCode.BadRequest)
        {
            var problem = await ReadJsonAsync(response, TeatimeJsonContext.Default.MailchimpError, cancellationToken);
            if (problem?.Title is "Member Exists")
                return new SubscribeResult(SubscribeOutcome.AlreadySubscribed);

            return Blame(provider, response.StatusCode, problem?.Detail ?? problem?.Title);
        }

        if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            var reason = provider switch
            {
                ListmonkProvider => (await ReadJsonAsync(response, TeatimeJsonContext.Default.ListmonkError, cancellationToken))?.Message,
                _ => (await ReadJsonAsync(response, TeatimeJsonContext.Default.BeaconErrorResponse, cancellationToken))?.Error,
            };
            return Blame(provider, response.StatusCode, reason);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("The {Provider} newsletter service returned status {Status}", provider.Name, (int)response.StatusCode);
            return new SubscribeResult(SubscribeOutcome.Unavailable);
        }

        return provider switch
        {
            BeaconProvider => await ReadBeaconOutcomeAsync(response, cancellationToken),
            ListmonkProvider => await ReadListmonkOutcomeAsync(response, cancellationToken),
            MailchimpProvider mailchimp => new SubscribeResult(mailchimp.DoubleOptIn
                ? SubscribeOutcome.ConfirmationSent
                : SubscribeOutcome.Subscribed),
            _ => new SubscribeResult(SubscribeOutcome.Subscribed),
        };
    }

    private async Task<SubscribeResult> ReadBeaconOutcomeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // The preference token in this reply is deliberately discarded rather than shown to the reader.
        var results = await ReadJsonAsync(response, TeatimeJsonContext.Default.BeaconTokenResponseArray, cancellationToken);
        return new SubscribeResult(results is { Length: > 0 } && results[0].DoubleOptIn
            ? SubscribeOutcome.ConfirmationSent
            : SubscribeOutcome.Subscribed);
    }

    private async Task<SubscribeResult> ReadListmonkOutcomeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await ReadJsonAsync(response, TeatimeJsonContext.Default.ListmonkSubscriptionResponse, cancellationToken);
        return new SubscribeResult(body?.Data?.HasOptin == true
            ? SubscribeOutcome.ConfirmationSent
            : SubscribeOutcome.Subscribed);
    }

    /// <summary>
    /// Decides whether a rejection is the reader's fault or ours. Providers use one status for both the
    /// address and our own settings, so only an explicit complaint about the email reaches the reader.
    /// </summary>
    private SubscribeResult Blame(INewsletterProvider provider, HttpStatusCode status, string? reason)
    {
        var text = reason ?? "no error message";

        if (IsEmailComplaint(text))
        {
            logger.LogWarning("The {Provider} newsletter service rejected an address: {Reason}", provider.Name, text);
            return new SubscribeResult(SubscribeOutcome.InvalidEmail);
        }

        logger.LogError(
            "The {Provider} newsletter service rejected the request with status {Status}, which points at the extension config: {Reason}",
            provider.Name, (int)status, text);
        return new SubscribeResult(SubscribeOutcome.Unavailable);
    }

    /// <summary>True when the provider's complaint is about the address the reader typed, not our settings.</summary>
    internal static bool IsEmailComplaint(string reason) =>
        reason.Contains("email", StringComparison.OrdinalIgnoreCase)
        || reason.Contains("e-mail", StringComparison.OrdinalIgnoreCase);

    private static async Task<T?> ReadJsonAsync<T>(
        HttpResponseMessage response, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException or NotSupportedException)
        {
            return default;
        }
    }

    /// <summary>Rejects an obviously bad address before it costs a round trip.</summary>
    private static string? Normalize(string? email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 254)
            return null;

        try
        {
            return new MailAddress(trimmed).Address == trimmed ? trimmed : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? Trim(string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }
}
