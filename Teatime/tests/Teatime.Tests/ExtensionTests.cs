using Microsoft.Extensions.Logging.Abstractions;
using Teatime.Configuration;
using Teatime.Services.Extensions;

namespace Teatime.Tests;

public sealed class ExtensionLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("teatime-ext").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ExtensionSet Load(string json)
    {
        File.WriteAllText(Path.Combine(_dir, ExtensionLoader.FileName), json);
        return ExtensionLoader.Load(_dir, NullLogger.Instance);
    }

    [Fact]
    public void MissingFile_YieldsNoExtensions()
    {
        Assert.True(ExtensionLoader.Load(_dir, NullLogger.Instance).IsEmpty);
    }

    [Fact]
    public void MalformedJson_YieldsNoExtensions()
    {
        Assert.True(Load("{ not json").IsEmpty);
    }

    [Fact]
    public void DisabledExtension_StaysInactive()
    {
        var set = Load("""
            { "extensions": { "medama": { "enabled": false, "url": "https://m.example.com" } } }
            """);

        Assert.True(set.IsEmpty);
    }

    [Fact]
    public void Matomo_WithUrlAndSiteId_Activates()
    {
        var set = Load("""
            { "extensions": { "matomo": { "enabled": true, "url": "https://analytics.example.com/", "site_id": "7" } } }
            """);

        var matomo = Assert.Single(set.Active);
        Assert.Equal("matomo", matomo.Name);
        Assert.Equal(["https://analytics.example.com"], matomo.CspSources);
        Assert.Contains("_paq.push(['setSiteId','7'])", matomo.Scripts[0].Inline);
        Assert.Contains("_paq.push(['disableCookies'])", matomo.Scripts[0].Inline);
        Assert.Equal("https://analytics.example.com/matomo.js", matomo.Scripts[1].Src);
    }

    [Fact]
    public void Matomo_HonoursCamelCaseSiteId()
    {
        var set = Load("""
            { "extensions": { "matomo": { "enabled": true, "url": "https://a.example.com", "siteId": "3" } } }
            """);

        Assert.Contains("'setSiteId','3'", Assert.Single(set.Active).Scripts[0].Inline);
    }

    [Fact]
    public void Matomo_CookiesStayOnWhenAsked()
    {
        var set = Load("""
            { "extensions": { "matomo": { "enabled": true, "url": "https://a.example.com", "siteId": "3", "disableCookies": false } } }
            """);

        Assert.DoesNotContain("disableCookies", Assert.Single(set.Active).Scripts[0].Inline);
    }

    [Theory]
    [InlineData("\"url\": \"analytics.example.com\", \"siteId\": \"1\"")]   // not absolute
    [InlineData("\"url\": \"ftp://a.example.com\", \"siteId\": \"1\"")]     // wrong scheme
    [InlineData("\"url\": \"https://a.example.com\", \"siteId\": \"\"")]    // no site id
    [InlineData("\"url\": \"https://a.example.com\", \"siteId\": \"abc\"")] // non-numeric site id
    [InlineData("\"siteId\": \"1\"")]                                      // no url
    public void Matomo_WithBadSettings_StaysInactive(string settings)
    {
        Assert.True(Load($$"""{ "extensions": { "matomo": { "enabled": true, {{settings}} } } }""").IsEmpty);
    }

    [Fact]
    public void Plausible_DefaultsToCloudHost()
    {
        var set = Load("""
            { "extensions": { "plausible": { "enabled": true, "domain": "blog.example.com" } } }
            """);

        var plausible = Assert.Single(set.Active);
        var script = Assert.Single(plausible.Scripts);
        Assert.Equal("https://plausible.io/js/script.js", script.Src);
        Assert.True(script.Defer);
        Assert.Equal(["https://plausible.io"], plausible.CspSources);
        Assert.Contains(new KeyValuePair<string, string>("data-domain", "blog.example.com"), script.Attributes!);
    }

    [Fact]
    public void Plausible_UsesSelfHostedUrlAndScriptVariant()
    {
        var set = Load("""
            { "extensions": { "plausible": { "enabled": true, "domain": "blog.example.com",
              "url": "https://stats.example.com", "script": "script.outbound-links.js" } } }
            """);

        Assert.Equal("https://stats.example.com/js/script.outbound-links.js", Assert.Single(set.Active).Scripts[0].Src);
    }

    [Theory]
    [InlineData("\"domain\": \"https://blog.example.com\"")]                       // scheme in domain
    [InlineData("\"domain\": \"blog.example.com\", \"script\": \"../evil.js\"")]   // path traversal
    [InlineData("\"domain\": \"blog.example.com\", \"script\": \"script.php\"")]   // not a script
    [InlineData("\"script\": \"script.js\"")]                                      // no domain
    public void Plausible_WithBadSettings_StaysInactive(string settings)
    {
        Assert.True(Load($$"""{ "extensions": { "plausible": { "enabled": true, {{settings}} } } }""").IsEmpty);
    }

    [Fact]
    public void Medama_WithUrl_Activates()
    {
        var set = Load("""
            { "extensions": { "medama": { "enabled": true, "url": "https://medama.example.com" } } }
            """);

        Assert.Equal("https://medama.example.com/script.js", Assert.Single(set.Active).Scripts[0].Src);
    }

    [Fact]
    public void MultipleExtensions_ShareOneDistinctSourceList()
    {
        var set = Load("""
            { "extensions": {
                "matomo": { "enabled": true, "url": "https://a.example.com", "siteId": "1" },
                "medama": { "enabled": true, "url": "https://a.example.com/medama" } } }
            """);

        Assert.Equal(2, set.Active.Count);
        Assert.Equal(["https://a.example.com"], set.CspSources);
        Assert.Equal("matomo,medama", set.Signature);
    }

    [Fact]
    public void Beacon_WithUrlBucketAndKey_Activates()
    {
        var set = Load("""
            { "extensions": { "beacon": { "enabled": true, "url": "https://beacon-api.melosso.com",
              "bucket": "newsletter_en", "apiKey": "Beacon-Api-Key" } } }
            """);

        var beacon = ((BeaconProvider)set.Newsletter!);
        Assert.Equal("https://beacon-api.melosso.com/api/tokens/generate", beacon.Endpoint.ToString());
        Assert.Equal("newsletter_en", beacon.Bucket);
        Assert.Equal("newsletter", beacon.Permission);
        Assert.Equal("X-Api-Key", beacon.ApiKeyHeader);
        Assert.Equal("en", beacon.Language);
        Assert.True(beacon.SkipPermissionUpdate);
        Assert.Equal("beacon", set.Signature);
    }

    [Fact]
    public void Beacon_ReadsApiKeyFromEnvironment()
    {
        Environment.SetEnvironmentVariable("TEATIME_TEST_BEACON_KEY", "secret-from-env");
        try
        {
            var set = Load("""
                { "extensions": { "beacon": { "enabled": true, "url": "https://b.example.com",
                  "bucket": "news", "apiKey": "${TEATIME_TEST_BEACON_KEY}" } } }
                """);

            Assert.Equal("secret-from-env", ((BeaconProvider)set.Newsletter!).ApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEATIME_TEST_BEACON_KEY", null);
        }
    }

    [Fact]
    public void Beacon_WithUnsetEnvironmentKey_StaysInactive()
    {
        var set = Load("""
            { "extensions": { "beacon": { "enabled": true, "url": "https://b.example.com",
              "bucket": "news", "apiKey": "${TEATIME_TEST_MISSING_KEY}" } } }
            """);

        Assert.Null(set.Newsletter);
        Assert.True(set.IsEmpty);
    }

    [Theory]
    [InlineData("\"bucket\": \"news\"")]                                                   // no key
    [InlineData("\"apiKey\": \"k\"")]                                                      // no bucket
    [InlineData("\"bucket\": \"news letter\", \"apiKey\": \"k\"")]                         // space in bucket
    [InlineData("\"bucket\": \"news\", \"apiKey\": \"k\", \"language\": \"jp\"")]          // unsupported language
    [InlineData("\"bucket\": \"news\", \"apiKey\": \"k\", \"apiKeyHeader\": \"Bad Header\"")]
    public void Beacon_WithBadSettings_StaysInactive(string settings)
    {
        var set = Load($$"""
            { "extensions": { "beacon": { "enabled": true, "url": "https://b.example.com", {{settings}} } } }
            """);

        Assert.Null(set.Newsletter);
    }

    [Fact]
    public void Beacon_CoexistsWithAnalytics()
    {
        var set = Load("""
            { "extensions": {
                "medama": { "enabled": true, "url": "https://m.example.com" },
                "beacon": { "enabled": true, "url": "https://b.example.com", "bucket": "news", "apiKey": "k" } } }
            """);

        Assert.Equal("medama,beacon", set.Signature);
        Assert.Equal(["https://m.example.com"], set.CspSources);
    }

    [Fact]
    public void Beacon_SendsCustomFields()
    {
        var set = Load("""
            { "extensions": { "beacon": { "enabled": true, "url": "https://b.example.com",
              "bucket": "news", "apiKey": "k", "customFields": { "source": "blog" } } } }
            """);

        var beacon = (BeaconProvider)set.Newsletter!;
        Assert.Equal("blog", beacon.CustomFields!["source"]);
    }

    [Fact]
    public void Listmonk_WithListUuid_Activates()
    {
        var set = Load("""
            { "extensions": { "listmonk": { "enabled": true, "url": "https://lists.example.com",
              "listUuid": "eb420c55-4cfb-4972-92ba-c93c34ba475d" } } }
            """);

        var listmonk = (ListmonkProvider)set.Newsletter!;
        Assert.Equal("https://lists.example.com/api/public/subscription", listmonk.Endpoint.ToString());
        Assert.Equal(["eb420c55-4cfb-4972-92ba-c93c34ba475d"], listmonk.ListUuids);
    }

    [Fact]
    public void Listmonk_MergesBothUuidFieldsWithoutDuplicates()
    {
        var set = Load("""
            { "extensions": { "listmonk": { "enabled": true, "url": "https://lists.example.com",
              "listUuid": "eb420c55-4cfb-4972-92ba-c93c34ba475d",
              "listUuids": ["eb420c55-4cfb-4972-92ba-c93c34ba475d", "0c554cfb-eb42-4972-92ba-c93c34ba475d"] } } }
            """);

        Assert.Equal(2, ((ListmonkProvider)set.Newsletter!).ListUuids.Count);
    }

    [Theory]
    [InlineData("\"url\": \"https://l.example.com\"")]                          // no list
    [InlineData("\"url\": \"https://l.example.com\", \"listUuid\": \"7\"")] // not a uuid
    [InlineData("\"listUuid\": \"eb420c55-4cfb-4972-92ba-c93c34ba475d\"")]      // no url
    public void Listmonk_WithBadSettings_StaysInactive(string settings)
    {
        Assert.Null(Load($$"""{ "extensions": { "listmonk": { "enabled": true, {{settings}} } } }""").Newsletter);
    }

    [Fact]
    public void Mailchimp_DerivesDataCentreFromKeySuffix()
    {
        Environment.SetEnvironmentVariable("TEATIME_TEST_MC_KEY", "abc123def456-us21");
        try
        {
            var set = Load("""
                { "extensions": { "mailchimp": { "enabled": true, "listId": "a1b2c3",
                  "apiKey": "${TEATIME_TEST_MC_KEY}" } } }
                """);

            var mailchimp = (MailchimpProvider)set.Newsletter!;
            Assert.Equal("https://us21.api.mailchimp.com/3.0/lists/a1b2c3/members", mailchimp.Endpoint.ToString());
            Assert.Equal("pending", mailchimp.Status);
            Assert.True(mailchimp.DoubleOptIn);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEATIME_TEST_MC_KEY", null);
        }
    }

    // The key is account-wide, so a literal in the content folder is refused even when it would work.
    [Fact]
    public void Mailchimp_RefusesALiteralApiKey()
    {
        var set = Load("""
            { "extensions": { "mailchimp": { "enabled": true, "listId": "a1b2c3", "apiKey": "abc123-us21" } } }
            """);

        Assert.Null(set.Newsletter);
        Assert.Contains("mailchimp", set.Rejected);
    }

    [Fact]
    public void Mailchimp_WithoutDataCentreSuffix_StaysInactive()
    {
        Environment.SetEnvironmentVariable("TEATIME_TEST_MC_BAD", "keywithoutsuffix");
        try
        {
            var set = Load("""
                { "extensions": { "mailchimp": { "enabled": true, "listId": "a1b2c3",
                  "apiKey": "${TEATIME_TEST_MC_BAD}" } } }
                """);

            Assert.Null(set.Newsletter);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEATIME_TEST_MC_BAD", null);
        }
    }

    // Picking a winner silently would be worse than refusing: the blog would mail the wrong list.
    [Fact]
    public void TwoNewsletterProviders_DisableBothAndAreReported()
    {
        var set = Load("""
            { "extensions": {
                "beacon": { "enabled": true, "url": "https://b.example.com", "bucket": "news", "apiKey": "k" },
                "listmonk": { "enabled": true, "url": "https://l.example.com",
                              "listUuid": "eb420c55-4cfb-4972-92ba-c93c34ba475d" } } }
            """);

        Assert.Null(set.Newsletter);
        Assert.Equal(["beacon", "listmonk"], set.Rejected);
    }

    [Fact]
    public void UnclosedEnvironmentReference_IsRefusedRatherThanSentAsAKey()
    {
        var set = Load("""
            {"extensions":{"beacon":{"enabled":true,"url":"https://b.example.com","bucket":"news","apiKey":"${TEATIME_MISSING_BRACE"}}}
            """);

        Assert.Null(set.Newsletter);
        Assert.Equal(["beacon"], set.Rejected);
    }

    [Fact]
    public void RejectedAnalyticsIsReported()
    {
        var set = Load("""
            { "extensions": { "medama": { "enabled": true, "url": "not-a-url" } } }
            """);

        Assert.Equal(["medama"], set.Rejected);
    }

    [Fact]
    public void UrlWithCredentialsOrQuery_IsRejected()
    {
        Assert.False(ExtensionLoader.TryBaseUrl("https://user:pass@a.example.com", out _, out _));
        Assert.False(ExtensionLoader.TryBaseUrl("https://a.example.com/?x=1", out _, out _));
        Assert.False(ExtensionLoader.TryBaseUrl("https://a.example.com/'+alert(1)+'", out _, out _));
    }
}

public sealed class ExtensionHeadRendererTests
{
    [Fact]
    public void EmptySet_RendersNothing()
    {
        Assert.Equal(string.Empty, ExtensionHeadRenderer.Build(ExtensionSet.Empty, "abc"));
    }

    [Fact]
    public void ScriptsCarryNonceAndAttributes()
    {
        var set = new ExtensionSet([
            new ActiveExtension("plausible",
                [new ExtensionScript(
                    Src: "https://plausible.io/js/script.js",
                    Defer: true,
                    Attributes: [new KeyValuePair<string, string>("data-domain", "blog.example.com")])],
                ["https://plausible.io"])
        ]);

        var html = ExtensionHeadRenderer.Build(set, "n0nce");

        Assert.Contains("<script nonce=\"n0nce\" defer src=\"https://plausible.io/js/script.js\" data-domain=\"blog.example.com\">", html);
        Assert.Contains("</script>", html);
    }

    [Fact]
    public void InlineScriptIsNotHtmlEncoded()
    {
        var set = new ExtensionSet([
            new ActiveExtension("matomo", [new ExtensionScript(Inline: "var a=b['c'];")], ["https://a.example.com"])
        ]);

        Assert.Contains(">var a=b['c'];</script>", ExtensionHeadRenderer.Build(set, null));
    }
}

public sealed class SecurityHeadersExtraSourceTests
{
    [Fact]
    public void NoSources_LeavesPolicyUntouched()
    {
        Assert.Equal(SecurityHeaders.DefaultCsp, SecurityHeaders.WithExtraSources(SecurityHeaders.DefaultCsp, []));
    }

    [Fact]
    public void SourcesWidenScriptConnectAndImage()
    {
        var csp = SecurityHeaders.WithExtraSources(SecurityHeaders.DefaultCsp, ["https://a.example.com"]);

        var directives = csp.Split(';').Select(d => d.Trim()).ToList();
        Assert.Contains(directives, d => d.StartsWith("script-src ") && d.Contains("https://a.example.com"));
        Assert.Contains(directives, d => d.StartsWith("connect-src ") && d.Contains("https://a.example.com"));
        Assert.Contains(directives, d => d.StartsWith("img-src ") && d.Contains("https://a.example.com"));
        Assert.Contains(directives, d => d == "frame-ancestors 'none'");
    }

    [Fact]
    public void WidenedPolicyStillTakesTheNonce()
    {
        var csp = SecurityHeaders.BuildNonceCsp(
            SecurityHeaders.WithExtraSources(SecurityHeaders.DefaultCsp, ["https://a.example.com"]), "n0nce");

        Assert.Contains("script-src 'self' 'nonce-n0nce' https://a.example.com", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
    }

    [Fact]
    public void MissingDirectiveIsAddedWithSelf()
    {
        var csp = SecurityHeaders.WithExtraSources("default-src 'self'", ["https://a.example.com"]);

        Assert.Contains("connect-src 'self' https://a.example.com", csp);
    }

    [Fact]
    public void CustomPolicyWithoutScriptSrc_StillTakesTheNonce()
    {
        var csp = SecurityHeaders.BuildNonceCsp(
            SecurityHeaders.WithExtraSources("default-src 'self'", ["https://a.example.com"]), "n0nce");

        Assert.Contains("script-src 'self' 'nonce-n0nce' https://a.example.com", csp);
        Assert.DoesNotContain("'unsafe-inline'", csp);
    }

    [Fact]
    public void RepeatedSourceIsNotDuplicated()
    {
        var csp = SecurityHeaders.WithExtraSources(SecurityHeaders.DefaultCsp, ["https://a.example.com", "https://a.example.com"]);

        Assert.Equal(1, csp.Split(';').Single(d => d.TrimStart().StartsWith("connect-src "))
            .Split("https://a.example.com").Length - 1);
    }
}
