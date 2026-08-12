using System.Text.Json.Serialization;

namespace Teatime.Models;

/// <summary>What /api/altcha hands the browser to solve, and what it posts back base64-encoded.</summary>
public sealed record AltchaChallenge(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("maxnumber")] int MaxNumber);

public sealed record AltchaSolution(
    [property: JsonPropertyName("algorithm")] string Algorithm,
    [property: JsonPropertyName("challenge")] string Challenge,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("signature")] string Signature);
