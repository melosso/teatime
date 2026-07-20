using System.Text.Json;
using Teatime.Services;

namespace Teatime.Tests;

public sealed class BeaconRequestTests
{
    private static string Serialize(BeaconTokenRequest request) =>
        JsonSerializer.Serialize(new[] { request }, new JsonSerializerOptions());

    private static BeaconTokenRequest Request(string? name = null, int? expiryDays = null) =>
        new(
            Bucket: "newsletter_en",
            Email: "jane@doe.com",
            Name: name,
            Permissions: new Dictionary<string, bool> { ["newsletter"] = true },
            Language: "en",
            ExpiryDays: expiryDays,
            SkipPermissionUpdate: true);

    // Beacon binds expiryDays to a non-nullable int, so an explicit null fails its deserializer with a 400.
    [Fact]
    public void UnsetOptionalFieldsAreOmitted()
    {
        var json = Serialize(Request());

        Assert.DoesNotContain("expiryDays", json);
        Assert.DoesNotContain("name", json);
        Assert.DoesNotContain("null", json);
    }

    [Fact]
    public void SetOptionalFieldsAreSent()
    {
        var json = Serialize(Request(name: "Jane Doe", expiryDays: 30));

        Assert.Contains("\"name\":\"Jane Doe\"", json);
        Assert.Contains("\"expiryDays\":30", json);
    }

    [Fact]
    public void RequiredFieldsUseBeaconsNames()
    {
        var json = Serialize(Request());

        Assert.Contains("\"bucket\":\"newsletter_en\"", json);
        Assert.Contains("\"email\":\"jane@doe.com\"", json);
        Assert.Contains("\"permissions\":{\"newsletter\":true}", json);
        Assert.Contains("\"skipPermissionUpdate\":true", json);
    }

    [Theory]
    [InlineData("Invalid email format", true)]
    [InlineData("Email too long", true)]
    [InlineData("Bucket contains invalid characters", false)]
    [InlineData("Permission contains invalid characters", false)]
    [InlineData("Unsupported language code 'jp'", false)]
    public void OnlyEmailComplaintsAreBlamedOnTheReader(string reason, bool isEmail)
    {
        Assert.Equal(isEmail, NewsletterService.IsEmailComplaint(reason));
    }
}
