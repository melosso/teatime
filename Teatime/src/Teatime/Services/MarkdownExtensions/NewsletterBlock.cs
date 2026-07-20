using System.Net;
using Teatime.Services.Rendering;
using YamlDotNet.Serialization;

namespace Teatime.Services.MarkdownExtensions;

/// <summary>
/// Renders a ```newsletter fenced block into a sign-up form. Every text field takes either a plain
/// string or a map of locale code to text, so a block can carry its own translations alongside the
/// ones in <c>content/locale/</c>. The markup is static, so it is built at content-parse time; the
/// layout wires up the submit handler when the form is present on a page. Everything the reader
/// types goes to Teatime's own /api/subscribe, never to a third party.
/// </summary>
public static class NewsletterBlock
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public static string Render(string body)
    {
        Dictionary<object, object?>? spec;
        try { spec = Yaml.Deserialize<Dictionary<object, object?>>(body); }
        catch (YamlDotNet.Core.YamlException) { return Error("Invalid newsletter block."); }

        spec ??= [];
        var l = Localization.Current;

        var heading = Encode(Text(spec, "heading", l.Code) ?? l.NewsletterHeading);
        var intro = Encode(Text(spec, "intro", l.Code) ?? l.NewsletterIntro);
        var button = Encode(Text(spec, "button", l.Code) ?? l.NewsletterSubmit);
        var placeholder = Encode(Text(spec, "placeholder", l.Code) ?? l.NewsletterEmailPlaceholder);

        // A field is either a flag or its own replacement text, both of which switch it on.
        var (showName, nameText) = Toggle(spec, "name", l.Code);
        var (showConsent, consentText) = Toggle(spec, "consent", l.Code);

        var nameField = showName
            ? $"""
               <label class="newsletter-field">
                 <span>{Encode(nameText ?? l.NewsletterNameLabel)}</span>
                 <input type="text" name="name" autocomplete="name" maxlength="200">
               </label>
               """
            : "";

        var consentField = showConsent
            ? $"""
               <label class="newsletter-consent">
                 <input type="checkbox" name="consent" value="true">
                 <span>{Encode(consentText ?? l.NewsletterConsent)}</span>
               </label>
               """
            : "";

        // The honeypot is hidden from sight and from assistive tech; only a bot fills it in.
        return $"""
            <form class="teatime-newsletter" data-newsletter novalidate
                  data-invalid="{Encode(l.NewsletterInvalidEmail)}"
                  data-consent-required="{Encode(l.NewsletterConsentRequired)}"
                  data-throttled="{Encode(l.NewsletterThrottled)}"
                  data-error="{Encode(l.NewsletterError)}">
              <h2 class="newsletter-heading">{heading}</h2>
              <p class="newsletter-intro">{intro}</p>
              {nameField}
              <label class="newsletter-field">
                <span class="visually-hidden">{Encode(l.NewsletterEmailLabel)}</span>
                <input type="email" name="email" required autocomplete="email" maxlength="254" placeholder="{placeholder}">
              </label>
              {consentField}
              <div class="newsletter-honeypot" aria-hidden="true">
                <label>Website<input type="text" name="website" tabindex="-1" autocomplete="off"></label>
              </div>
              <button type="submit" class="newsletter-submit" data-label="{button}" data-sending="{Encode(l.NewsletterSending)}">{button}</button>
              <p class="newsletter-status" role="status" aria-live="polite"></p>
              <noscript><p class="newsletter-status">{Encode(l.NewsletterError)}</p></noscript>
            </form>
            """;
    }

    /// <summary>
    /// Resolves one field for the active locale. A plain string applies everywhere; a map is looked up
    /// by locale code, then by its base language, then English, before giving up so the built-in string
    /// can take over.
    /// </summary>
    internal static string? Text(Dictionary<object, object?> spec, string key, string code)
    {
        if (!TryValue(spec, key, out var value))
            return null;

        return value switch
        {
            string text => Blank(text) ? null : text,
            IDictionary<object, object?> byLocale => Lookup(byLocale, code),
            _ => null,
        };
    }

    /// <summary>Reads a field that is either a boolean flag or replacement text, both of which enable it.</summary>
    private static (bool Enabled, string? Text) Toggle(Dictionary<object, object?> spec, string key, string code)
    {
        if (!TryValue(spec, key, out var value))
            return (false, null);

        return value switch
        {
            string text when bool.TryParse(text, out var flag) => (flag, null),
            string text => (!Blank(text), Blank(text) ? null : text),
            IDictionary<object, object?> byLocale => (true, Lookup(byLocale, code)),
            bool flag => (flag, null),
            _ => (false, null),
        };
    }

    private static string? Lookup(IDictionary<object, object?> byLocale, string code)
    {
        // "nl-BE" should still find a plain "nl" entry.
        var baseCode = code.Split('-')[0];

        foreach (var candidate in new[] { code, baseCode, "en" })
            if (Find(byLocale, candidate) is { } hit)
                return hit;

        return byLocale.Values.OfType<string>().FirstOrDefault(v => !Blank(v));
    }

    private static string? Find(IDictionary<object, object?> byLocale, string code)
    {
        foreach (var (key, value) in byLocale)
            if (key?.ToString()?.Equals(code, StringComparison.OrdinalIgnoreCase) == true
                && value is string text && !Blank(text))
                return text;

        return null;
    }

    private static bool TryValue(Dictionary<object, object?> spec, string key, out object? value)
    {
        foreach (var (candidate, candidateValue) in spec)
            if (candidate?.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
            {
                value = candidateValue;
                return true;
            }

        value = null;
        return false;
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Error(string message) =>
        $"<div class=\"newsletter-error\">{Encode(message)}</div>";
}
