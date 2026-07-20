namespace Teatime.Models;

/// <summary>Body posted by the newsletter form. <c>Website</c> is the honeypot: humans leave it empty.</summary>
public sealed record SubscribeRequest(string? Email, string? Name, string? Website, bool Consent);

/// <summary>What the newsletter form shows the reader. Never carries anything Beacon returned.</summary>
public sealed record SubscribeResponse(bool Ok, string Message);
