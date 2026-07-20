using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Teatime.Models;
using Teatime.Services.Extensions;

namespace Teatime.Tests;

public sealed class AltchaServiceTests
{
    private readonly AltchaService _altcha = new();

    private static string Encode(AltchaSolution solution) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(solution));

    private static int SolveNumber(AltchaChallenge challenge)
    {
        for (var number = 0; number <= challenge.MaxNumber; number++)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(challenge.Salt + number)))
                .ToLowerInvariant();
            if (hash == challenge.Challenge)
                return number;
        }

        throw new InvalidOperationException("challenge had no solution below maxnumber");
    }

    private string Solved(AltchaChallenge challenge) =>
        Encode(new AltchaSolution("SHA-256", challenge.Challenge, SolveNumber(challenge), challenge.Salt, challenge.Signature));

    [Fact]
    public void ChallengeCarriesAnExpiryAndASignature()
    {
        var challenge = _altcha.Create();

        Assert.Equal("SHA-256", challenge.Algorithm);
        Assert.Contains("?expires=", challenge.Salt);
        Assert.NotEmpty(challenge.Signature);
        Assert.Equal(30_000, challenge.MaxNumber);
    }

    [Fact]
    public void EveryChallengeIsDifferent()
    {
        Assert.NotEqual(_altcha.Create().Challenge, _altcha.Create().Challenge);
    }

    [Fact]
    public void SolvedChallengeIsAccepted()
    {
        Assert.True(_altcha.Verify(Solved(_altcha.Create())));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!")]
    [InlineData("bm90IGpzb24=")]
    public void GarbagePayload_IsRefused(string? payload)
    {
        Assert.False(_altcha.Verify(payload));
    }

    [Fact]
    public void WrongNumber_IsRefused()
    {
        var challenge = _altcha.Create();
        var number = SolveNumber(challenge);

        Assert.False(_altcha.Verify(Encode(
            new AltchaSolution("SHA-256", challenge.Challenge, number + 1, challenge.Salt, challenge.Signature))));
    }

    [Fact]
    public void ForgedChallenge_IsRefused()
    {
        var salt = $"deadbeef?expires={DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}";
        var challenge = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + "7"))).ToLowerInvariant();

        Assert.False(_altcha.Verify(Encode(new AltchaSolution("SHA-256", challenge, 7, salt, "00"))));
    }

    [Fact]
    public void SignatureFromAnotherProcess_IsRefused()
    {
        var challenge = new AltchaService().Create();

        Assert.False(_altcha.Verify(Solved(challenge)));
    }

    [Fact]
    public void ExpiredChallenge_IsRefused()
    {
        var salt = $"deadbeef?expires={DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds()}";
        var challenge = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + "7"))).ToLowerInvariant();

        Assert.False(_altcha.Verify(Encode(new AltchaSolution("SHA-256", challenge, 7, salt, "00"))));
    }

    [Fact]
    public void SolutionIsSpentAfterOneUse()
    {
        var payload = Solved(_altcha.Create());

        Assert.True(_altcha.Verify(payload));
        Assert.False(_altcha.Verify(payload));
    }

    [Fact]
    public void NumberBeyondMaxNumber_IsRefused()
    {
        var challenge = _altcha.Create();

        Assert.False(_altcha.Verify(Encode(
            new AltchaSolution("SHA-256", challenge.Challenge, 30_001, challenge.Salt, challenge.Signature))));
    }
}
