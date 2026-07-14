using Teatime.Configuration;
using Teatime.Services;

namespace Teatime.Endpoints;

internal static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/raw/{**path}", GetRawMarkdown).RequireRateLimiting(RateLimitPolicies.Search);
        // Catch-all has lowest route precedence automatically; registered last for readability
        app.MapGet("/{**path}", (string? path, HttpContext context, PageRequestHandler handler) =>
            handler.HandleAsync(path, context));
        return app;
    }

    internal static async Task<IResult> GetRawMarkdown(
        string? path,
        bool? view,
        ContentService docs,
        DocsOptions options,
        HttpContext context)
    {
        path = (path ?? "").Trim('/').ToLowerInvariant();
        if (path.EndsWith(".md", StringComparison.Ordinal))
            path = path[..^3];
        var page = await docs.GetPageAsync(path, context.RequestAborted);
        if (page?.OriginalRelativePath is not { } relPath)
            return Results.NotFound();

        var docsRoot = Path.GetFullPath(options.RootPath);
        var filePath = Path.GetFullPath(Path.Combine(docsRoot, relPath.Replace('/', Path.DirectorySeparatorChar)));
        if (!filePath.StartsWith(docsRoot, StringComparison.Ordinal) || !File.Exists(filePath))
            return Results.NotFound();

        var fileInfo = new FileInfo(filePath);
        var lastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc);
        var etag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{fileInfo.LastWriteTimeUtc.Ticks:x}\"");

        if (view == true)
            return Results.File(filePath, "text/plain; charset=utf-8", lastModified: lastModified, entityTag: etag);

        var filename = Path.GetFileName(relPath);
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
        return Results.File(filePath, "text/markdown; charset=utf-8", lastModified: lastModified, entityTag: etag);
    }
}
