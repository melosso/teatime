namespace Teatime.Services.Extensions;

/// <summary>One script tag contributed by an extension. Rendered with the per-request CSP nonce.</summary>
public sealed record ExtensionScript(
    string? Src = null,
    string? Inline = null,
    bool Async = false,
    bool Defer = false,
    IReadOnlyList<KeyValuePair<string, string>>? Attributes = null);

/// <summary>An extension that was enabled and passed verification.</summary>
/// <param name="CspSources">Origins the scripts talk to; widened into script-src, connect-src and img-src.</param>
public sealed record ActiveExtension(
    string Name,
    IReadOnlyList<ExtensionScript> Scripts,
    IReadOnlyList<string> CspSources);

/// <summary>The verified extensions for the current content snapshot.</summary>
/// <param name="Newsletter">The single active newsletter back end, held apart from the browser-facing ones.</param>
/// <param name="Invalid">Extensions that were enabled but did not pass their check, for the startup log.</param>
public sealed record ExtensionSet(
    IReadOnlyList<ActiveExtension> Active,
    INewsletterProvider? Newsletter = null,
    IReadOnlyList<string>? Invalid = null)
{
    public static readonly ExtensionSet Empty = new([]);

    public bool IsEmpty => Active.Count == 0 && Newsletter is null;

    /// <summary>Names of extensions that were enabled but rejected, in declaration order.</summary>
    public IReadOnlyList<string> Rejected { get; } = Invalid ?? [];

    /// <summary>Distinct origins across every active extension, in declaration order.</summary>
    public IReadOnlyList<string> CspSources { get; } =
        Active.SelectMany(e => e.CspSources).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>Names of the active extensions, for change detection and logging.</summary>
    public string Signature { get; } = string.Join(",",
        Active.Select(e => e.Name).Concat(Newsletter is null ? [] : new[] { Newsletter.Name }));
}
