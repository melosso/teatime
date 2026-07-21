using Teatime.Models;

namespace Teatime.Services.Extensions;

public static partial class ExtensionLoader
{
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
}
