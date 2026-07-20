using System.Text.Json.Serialization;
using Teatime.Models;
using Teatime.Services;

namespace Teatime.Serialization;

// Source-generated JSON metadata for API response types; no runtime reflection, AOT-ready; unknown types fall back to reflection via the resolver chain
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<PageSummary>))]
[JsonSerializable(typeof(BuildVersionResponse))]
[JsonSerializable(typeof(IReadOnlyList<SearchResult>))]
[JsonSerializable(typeof(SearchResult[]))]
[JsonSerializable(typeof(GroupedSearchResponse))]
[JsonSerializable(typeof(SearchIndexExport))]
[JsonSerializable(typeof(BookmarkCache))]
[JsonSerializable(typeof(BeaconTokenRequest[]))]
[JsonSerializable(typeof(BeaconTokenResponse[]))]
[JsonSerializable(typeof(BeaconErrorResponse))]
[JsonSerializable(typeof(ListmonkSubscriptionRequest))]
[JsonSerializable(typeof(ListmonkSubscriptionResponse))]
[JsonSerializable(typeof(ListmonkError))]
[JsonSerializable(typeof(MailchimpMemberRequest))]
[JsonSerializable(typeof(MailchimpError))]
[JsonSerializable(typeof(SubscribeRequest))]
[JsonSerializable(typeof(SubscribeResponse))]
internal sealed partial class TeatimeJsonContext : JsonSerializerContext;
