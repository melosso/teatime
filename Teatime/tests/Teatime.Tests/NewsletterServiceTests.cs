using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Teatime.Endpoints;
using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Extensions;

namespace Teatime.Tests;

/// <summary>Records what left the server and hands back a canned reply.</summary>
internal sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public int Calls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Request = request;
        Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}

internal sealed class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("connection refused");
}

internal sealed class FixedExtensions(INewsletterProvider? newsletter) : IExtensionSource
{
    public ExtensionSet Extensions { get; } = new([], newsletter);
}

public sealed class NewsletterServiceTests
{
    private static readonly BeaconProvider Beacon = new(
        Endpoint: new Uri("https://beacon.example.com/api/tokens/generate"),
        ApiKeyHeader: "X-Api-Key",
        ApiKey: "secret-key",
        Bucket: "newsletter_en",
        Permission: "newsletter",
        Language: "en",
        ExpiryDays: 30,
        CollectName: true,
        SkipPermissionUpdate: true,
        CustomFields: new Dictionary<string, string> { ["source"] = "blog" });

    private static readonly ListmonkProvider Listmonk = new(
        Endpoint: new Uri("https://lists.example.com/api/public/subscription"),
        ListUuids: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301"],
        CollectName: false);

    private static readonly MailchimpProvider Mailchimp = new(
        Endpoint: new Uri("https://us21.api.mailchimp.com/3.0/lists/abc/members"),
        ApiKey: "key-us21",
        Status: "pending",
        CollectName: true);

    private static (NewsletterService Service, StubHandler Handler) Build(
        INewsletterProvider? provider, HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var service = new NewsletterService(
            new HttpClient(handler), new FixedExtensions(provider), NullLogger<NewsletterService>.Instance);
        return (service, handler);
    }

    private static Task<SubscribeResult> Subscribe(NewsletterService service, string? email = "reader@example.com", string? name = null) =>
        service.SubscribeAsync(email, name, CancellationToken.None);

    [Fact]
    public async Task NoProvider_ReportsDisabledWithoutACall()
    {
        var (service, handler) = Build(null);

        Assert.False(service.IsEnabled);
        Assert.Equal(SubscribeOutcome.Disabled, (await Subscribe(service)).Outcome);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-address")]
    [InlineData("Reader <reader@example.com>")]
    public async Task BadAddress_IsRefusedBeforeTheRoundTrip(string? email)
    {
        var (service, handler) = Build(Listmonk);

        Assert.Equal(SubscribeOutcome.InvalidEmail, (await Subscribe(service, email)).Outcome);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task OverlongAddress_IsRefused()
    {
        var (service, _) = Build(Listmonk);
        var email = new string('a', 250) + "@example.com";

        Assert.Equal(SubscribeOutcome.InvalidEmail, (await Subscribe(service, email)).Outcome);
    }

    [Fact]
    public async Task Beacon_SendsBucketPermissionAndKeyHeader()
    {
        var (service, handler) = Build(Beacon, body: """[{"token":"t","doubleOptIn":false}]""");

        var result = await Subscribe(service, name: "Ada");

        Assert.Equal(SubscribeOutcome.Subscribed, result.Outcome);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("secret-key", handler.Request.Headers.GetValues("X-Api-Key").Single());
        Assert.Contains("\"bucket\":\"newsletter_en\"", handler.Body);
        Assert.Contains("\"newsletter\":true", handler.Body);
        Assert.Contains("\"name\":\"Ada\"", handler.Body);
        Assert.Contains("\"source\":\"blog\"", handler.Body);
        Assert.Contains("\"skipPermissionUpdate\":true", handler.Body);
    }

    [Fact]
    public async Task Beacon_DoubleOptIn_AsksTheReaderToConfirm()
    {
        var (service, _) = Build(Beacon, body: """[{"token":"t","doubleOptIn":true}]""");

        Assert.Equal(SubscribeOutcome.ConfirmationSent, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task Beacon_EmptyReply_StillCountsAsSubscribed()
    {
        var (service, _) = Build(Beacon, body: "[]");

        Assert.Equal(SubscribeOutcome.Subscribed, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task ProviderComplainsAboutTheAddress_TheReaderIsTold()
    {
        var (service, _) = Build(Beacon, HttpStatusCode.BadRequest, """{"error":"email is not valid"}""");

        Assert.Equal(SubscribeOutcome.InvalidEmail, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task ProviderComplainsAboutOurSettings_TheReaderIsNotBlamed()
    {
        var (service, _) = Build(Beacon, HttpStatusCode.BadRequest, """{"error":"bucket does not exist"}""");

        Assert.Equal(SubscribeOutcome.Unavailable, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task Listmonk_SendsListUuidsAndNoCredential()
    {
        var (service, handler) = Build(Listmonk, body: """{"data":{"has_optin":false}}""");

        var result = await Subscribe(service, name: "Ada");

        Assert.Equal(SubscribeOutcome.Subscribed, result.Outcome);
        Assert.Contains("3f2504e0-4f89-11d3-9a0c-0305e82c3301", handler.Body);
        // collectName is off for this list, so the name is dropped rather than forwarded.
        Assert.DoesNotContain("Ada", handler.Body);
        Assert.Null(handler.Request!.Headers.Authorization);
    }

    [Fact]
    public async Task Listmonk_OptinPending_AsksTheReaderToConfirm()
    {
        var (service, _) = Build(Listmonk, body: """{"data":{"has_optin":true}}""");

        Assert.Equal(SubscribeOutcome.ConfirmationSent, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task Listmonk_BadAddress_IsReportedToTheReader()
    {
        var (service, _) = Build(Listmonk, HttpStatusCode.BadRequest, """{"message":"invalid email"}""");

        Assert.Equal(SubscribeOutcome.InvalidEmail, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task Mailchimp_SendsBasicAuthAndMergeFields()
    {
        var (service, handler) = Build(Mailchimp, body: "{}");

        var result = await Subscribe(service, name: "Ada");

        Assert.Equal(SubscribeOutcome.ConfirmationSent, result.Outcome);
        Assert.Equal("Basic", handler.Request!.Headers.Authorization!.Scheme);
        Assert.Contains("\"status\":\"pending\"", handler.Body);
        Assert.Contains("\"FNAME\":\"Ada\"", handler.Body);
    }

    [Fact]
    public async Task Mailchimp_SubscribedStatus_SkipsTheConfirmation()
    {
        var (service, _) = Build(Mailchimp with { Status = "subscribed" });

        Assert.Equal(SubscribeOutcome.Subscribed, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task Mailchimp_MemberExists_ReadsAsSuccess()
    {
        var (service, _) = Build(Mailchimp, HttpStatusCode.BadRequest, """{"title":"Member Exists","detail":"already a list member"}""");

        var result = await Subscribe(service);

        Assert.Equal(SubscribeOutcome.AlreadySubscribed, result.Outcome);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Mailchimp_OtherRejection_IsOurProblem()
    {
        var (service, _) = Build(Mailchimp, HttpStatusCode.BadRequest, """{"title":"Invalid Resource","detail":"list not found"}""");

        Assert.Equal(SubscribeOutcome.Unavailable, (await Subscribe(service)).Outcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UpstreamFailure_IsNeverBlamedOnTheReader(HttpStatusCode status)
    {
        var (service, _) = Build(Beacon, status, "{}");

        Assert.Equal(SubscribeOutcome.Unavailable, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task UnreachableProvider_ReportsUnavailable()
    {
        var service = new NewsletterService(
            new HttpClient(new ThrowingHandler()), new FixedExtensions(Beacon), NullLogger<NewsletterService>.Instance);

        Assert.Equal(SubscribeOutcome.Unavailable, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task GarbledReply_StillCountsAsSubscribed()
    {
        var (service, _) = Build(Beacon, body: "<html>not json</html>");

        Assert.Equal(SubscribeOutcome.Subscribed, (await Subscribe(service)).Outcome);
    }

    [Fact]
    public async Task LongNameIsTrimmedToTheProvidersLimit()
    {
        var (service, handler) = Build(Beacon, body: "[]");

        await Subscribe(service, name: new string('a', 500));

        Assert.Contains(new string('a', 200) + "\"", handler.Body);
        Assert.DoesNotContain(new string('a', 201), handler.Body);
    }
}

public sealed class SubscribeEndpointTests
{
    private static NewsletterService Service(INewsletterProvider? provider, HttpStatusCode status, string body, out StubHandler handler)
    {
        handler = new StubHandler(status, body);
        return new NewsletterService(
            new HttpClient(handler), new FixedExtensions(provider), NullLogger<NewsletterService>.Instance);
    }

    private static readonly ListmonkProvider Listmonk = new(
        new Uri("https://lists.example.com/api/public/subscription"),
        ["3f2504e0-4f89-11d3-9a0c-0305e82c3301"],
        CollectName: false);

    private static int StatusOf(IResult result) =>
        result is IStatusCodeHttpResult { StatusCode: { } code } ? code : 200;

    private static SubscribeResponse ValueOf(IResult result) =>
        (SubscribeResponse)((IValueHttpResult)result).Value!;

    [Fact]
    public async Task NoProvider_Answers404()
    {
        var service = Service(null, HttpStatusCode.OK, "{}", out _);

        var result = await ApiEndpoints.Subscribe(
            new SubscribeRequest("reader@example.com", null, null, true), service, CancellationToken.None);

        Assert.Equal(404, StatusOf(result));
        Assert.False(ValueOf(result).Ok);
    }

    [Fact]
    public async Task FilledHoneypot_IsThankedAndIgnored()
    {
        var service = Service(Listmonk, HttpStatusCode.OK, "{}", out var handler);

        var result = await ApiEndpoints.Subscribe(
            new SubscribeRequest("bot@example.com", null, "https://spam.example", true), service, CancellationToken.None);

        Assert.Equal(200, StatusOf(result));
        Assert.True(ValueOf(result).Ok);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task GoodAddress_Answers200()
    {
        var service = Service(Listmonk, HttpStatusCode.OK, """{"data":{"has_optin":true}}""", out var handler);

        var result = await ApiEndpoints.Subscribe(
            new SubscribeRequest("reader@example.com", null, null, true), service, CancellationToken.None);

        Assert.Equal(200, StatusOf(result));
        Assert.True(ValueOf(result).Ok);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task BadAddress_Answers400()
    {
        var service = Service(Listmonk, HttpStatusCode.OK, "{}", out _);

        var result = await ApiEndpoints.Subscribe(
            new SubscribeRequest("nope", null, null, true), service, CancellationToken.None);

        Assert.Equal(400, StatusOf(result));
        Assert.False(ValueOf(result).Ok);
    }

    [Fact]
    public async Task UpstreamFailure_Answers502WithoutTheProvidersWords()
    {
        var service = Service(Listmonk, HttpStatusCode.InternalServerError, """{"message":"listmonk exploded at /var/lib"}""", out _);

        var result = await ApiEndpoints.Subscribe(
            new SubscribeRequest("reader@example.com", null, null, true), service, CancellationToken.None);

        Assert.Equal(502, StatusOf(result));
        Assert.DoesNotContain("listmonk", ValueOf(result).Message, StringComparison.OrdinalIgnoreCase);
    }
}
