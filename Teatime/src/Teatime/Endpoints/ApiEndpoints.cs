using Microsoft.AspNetCore.Http.HttpResults;
using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services;

namespace Teatime.Endpoints;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        api.MapGet("/search", Search).RequireRateLimiting(RateLimitPolicies.Search);
        api.MapGet("/pages", GetPages).RequireRateLimiting(RateLimitPolicies.Search);
        // NOT rate-limited; the hot-reload script polls this every few seconds
        api.MapGet("/build-version", GetBuildVersion);
        return app;
    }

    internal static async Task<Ok<GroupedSearchResponse>> Search(
        string? q, ContentService docs, AuthorService authors, PostService posts,
        PageRequestSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return TypedResults.Ok(GroupedSearchResponse.Empty);

        var postHits = docs.Search(q);
        var view = await posts.GetViewAsync(cancellationToken);
        var grouped = GroupedSearch.Build(q, postHits, authors.GetListed(), view.Tags, settings.BasePath);
        return TypedResults.Ok(grouped);
    }

    // Public page metadata only; no OriginalRelativePath or other server file paths
    internal static async Task<Ok<List<PageSummary>>> GetPages(ContentService docs, CancellationToken cancellationToken)
    {
        var pages = await docs.GetAllPagesAsync(cancellationToken);
        var items = pages
            .OrderBy(p => p.Path)
            .Select(p => new PageSummary(p.Path, p.Title, p.Description, p.LastModified))
            .ToList();
        return TypedResults.Ok(items);
    }

    internal static Ok<BuildVersionResponse> GetBuildVersion(HttpContext context, ContentService docs)
    {
        // "no-store" not just "no-cache"; the hot-reload poll needs the live value every time.
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        return TypedResults.Ok(new BuildVersionResponse(docs.BuildVersion));
    }
}
