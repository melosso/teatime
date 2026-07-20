using Microsoft.AspNetCore.Builder;

namespace Teatime.Configuration;

public static class SecurityHeaders
{
    public const string DefaultCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "style-src-attr 'unsafe-inline'; " +
        "style-src-elem 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://tile.openstreetmap.org https://*.tile.openstreetmap.org; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";

    public static Task Apply(HttpContext context, Func<Task> next) =>
        Apply(context, next, DefaultCsp);

    public static Task Apply(HttpContext context, Func<Task> next, string contentSecurityPolicy)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.ContentSecurityPolicy = contentSecurityPolicy;
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

        if (context.Request.IsHttps)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return next();
    }

    // Extensions load a third-party tracker: the script itself, the beacons it sends, and (Matomo) a pixel.
    private static readonly string[] ExtensionDirectives = ["script-src", "connect-src", "img-src"];

    /// <summary>Widens the fetch directives an extension needs with the origins it was verified against.</summary>
    public static string WithExtraSources(string baseCsp, IReadOnlyList<string> sources)
    {
        if (sources.Count == 0)
            return baseCsp;

        var directives = baseCsp.Split(';').ToList();
        foreach (var name in ExtensionDirectives)
            AppendSources(directives, name, sources);

        return string.Join(";", directives);
    }

    private static void AppendSources(List<string> directives, string name, IReadOnlyList<string> sources)
    {
        var index = directives.FindIndex(d => d.TrimStart().StartsWith(name + " ", StringComparison.Ordinal));
        if (index < 0)
        {
            // Absent directive falls back to default-src, so a fresh one has to restate 'self'.
            directives.Add($" {name} 'self' {string.Join(' ', sources)}");
            return;
        }

        var existing = directives[index];
        foreach (var source in sources)
            if (!existing.Contains(source, StringComparison.Ordinal))
                existing = $"{existing.TrimEnd()} {source}";

        directives[index] = existing;
    }

    public static string BuildNonceCsp(string baseCsp, string nonce)
    {
        var noncePart = $"'nonce-{nonce}'";
        var directives = baseCsp.Split(';');
        for (var i = 0; i < directives.Length; i++)
        {
            var trimmed = directives[i].TrimStart();
            if ((trimmed.StartsWith("script-src ") || (trimmed.StartsWith("style-src ") && !trimmed.StartsWith("style-src-attr")))
                && trimmed.Contains("'unsafe-inline'"))
                directives[i] = directives[i].Replace("'unsafe-inline'", noncePart);
        }
        return string.Join(";", directives);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string? contentSecurityPolicy = null)
    {
        var csp = contentSecurityPolicy ?? SecurityHeaders.DefaultCsp;
        return app.Use((context, next) => SecurityHeaders.Apply(context, next, csp));
    }
}
