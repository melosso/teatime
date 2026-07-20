using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Teatime.Models;
using Teatime.Serialization;

namespace Teatime.Services.Extensions;

/// <summary>
/// ALTCHA-compatible proof of work guarding sign-up. Challenges are HMAC-signed with a per-process key
/// and spent once, so nothing is stored and no third party is involved.
/// </summary>
public sealed class AltchaService
{
    // Averages ~15k SHA-256 rounds for a reader, which is a few hundred ms, and costs a bot the same per try.
    private const int MaxNumber = 30_000;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _spent = new(StringComparer.Ordinal);

    public AltchaChallenge Create()
    {
        var expires = DateTimeOffset.UtcNow.Add(Lifetime).ToUnixTimeSeconds();
        var salt = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}?expires={expires}";
        var number = RandomNumberGenerator.GetInt32(MaxNumber);
        var challenge = Hash(salt, number);

        return new AltchaChallenge("SHA-256", challenge, salt, Sign(challenge), MaxNumber);
    }

    public bool Verify(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        AltchaSolution? solution;
        try
        {
            solution = JsonSerializer.Deserialize(
                Convert.FromBase64String(payload.Trim()), TeatimeJsonContext.Default.AltchaSolution);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }

        if (solution is not { Algorithm: "SHA-256", Challenge.Length: > 0, Salt.Length: > 0, Signature.Length: > 0 })
            return false;

        if (solution.Number is < 0 or > MaxNumber)
            return false;

        var expires = Expiry(solution.Salt);
        if (expires < DateTimeOffset.UtcNow)
            return false;

        if (!FixedTimeEquals(Sign(solution.Challenge), solution.Signature))
            return false;

        if (!FixedTimeEquals(Hash(solution.Salt, solution.Number), solution.Challenge))
            return false;

        Sweep();
        return _spent.TryAdd(solution.Challenge, expires);
    }

    private static string Hash(string salt, int number) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + number))).ToLowerInvariant();

    private string Sign(string challenge) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(challenge))).ToLowerInvariant();

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static DateTimeOffset Expiry(string salt)
    {
        var marker = salt.IndexOf("?expires=", StringComparison.Ordinal);
        return marker >= 0 && long.TryParse(salt[(marker + 9)..], out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : DateTimeOffset.MinValue;
    }

    private void Sweep()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (challenge, expires) in _spent)
            if (expires < now)
                _spent.TryRemove(challenge, out _);
    }
}
