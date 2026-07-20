using System.Text.Json.Serialization;

namespace Teatime.Services.Extensions;

// Wire shapes of each newsletter back end, serialized through TeatimeJsonContext.

// Beacon binds expiryDays and name to non-nullable members, so an omitted field is fine but an
// explicit null fails deserialization with a 400 before its handler ever runs.
public sealed record BeaconTokenRequest(
    [property: JsonPropertyName("bucket")] string Bucket,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonPropertyName("permissions")] IReadOnlyDictionary<string, bool> Permissions,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("expiryDays"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ExpiryDays,
    [property: JsonPropertyName("skipPermissionUpdate")] bool SkipPermissionUpdate,
    [property: JsonPropertyName("customFields"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string>? CustomFields = null);

public sealed record BeaconTokenResponse(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("doubleOptIn")] bool DoubleOptIn);

public sealed record BeaconErrorResponse([property: JsonPropertyName("error")] string? Error);

public sealed record ListmonkSubscriptionRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonPropertyName("list_uuids")] IReadOnlyList<string> ListUuids);

public sealed record ListmonkSubscriptionResponse([property: JsonPropertyName("data")] ListmonkSubscriptionData? Data);

public sealed record ListmonkSubscriptionData([property: JsonPropertyName("has_optin")] bool HasOptin);

public sealed record ListmonkError([property: JsonPropertyName("message")] string? Message);

public sealed record MailchimpMemberRequest(
    [property: JsonPropertyName("email_address")] string EmailAddress,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("merge_fields"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MailchimpMergeFields? MergeFields);

public sealed record MailchimpMergeFields([property: JsonPropertyName("FNAME")] string FirstName);

public sealed record MailchimpError(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("detail")] string? Detail);
