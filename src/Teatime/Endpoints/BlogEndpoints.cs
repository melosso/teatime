using Teatime.Configuration;
using Teatime.Services;
using Teatime.Services.Rendering;

namespace Teatime.Endpoints;

internal static class BlogEndpoints
{
    public static IEndpointRouteBuilder MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", (HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options) =>
            RenderHome(ctx, posts, responder, options, 1));

        app.MapGet("/page/{n:int}", (int n, HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options) =>
            RenderHome(ctx, posts, responder, options, n));

        app.MapGet("/posts/{slug}", RenderPost);
        app.MapGet("/tags", RenderTagIndex);
        app.MapGet("/tags/{tag}", RenderTag);
        app.MapGet("/archive", RenderArchive);
        return app;
    }

    private static async Task RenderHome(HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options, int page)
    {
        if (page < 1)
        {
            ctx.Response.Redirect(responder.HomeUrl);
            return;
        }

        var (slice, totalPages) = await posts.GetPageAsync(page, options.PageSize, ctx.RequestAborted);
        if (page > totalPages && page > 1)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var basePath = responder.BasePath;
        string content;
        if (slice.Count == 0)
        {
            content = PostListRenderer.BuildList(slice, basePath, emptyMessage: "No posts yet. Drop a Markdown file in content/posts/.");
        }
        else if (page == 1)
        {
            var rest = slice.Skip(1).ToList();
            content = PostListRenderer.BuildFeaturedLead(slice[0], basePath)
                    + (rest.Count > 0 ? PostListRenderer.BuildList(rest, basePath) : string.Empty);
        }
        else
        {
            content = PostListRenderer.BuildList(slice, basePath);
        }
        content += PostListRenderer.BuildPager(page, totalPages, basePath);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? $"Page {page}" : string.Empty,
            ContentHtml: content,
            CanonicalPath: page > 1 ? $"page/{page}" : ""));
    }

    private static async Task RenderPost(string slug, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content)
    {
        var post = await posts.GetBySlugAsync(slug, ctx.RequestAborted);
        if (post is null)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var basePath = responder.BasePath;
        var config = content.SiteConfig;
        var (older, newer) = await posts.GetPrevNextAsync(post.Slug, ctx.RequestAborted);
        var tocHtml = post.ShowToc && post.Headings.Count > 0
            ? TocHtmlRenderer.BuildTocHtml(post.Headings)
            : null;

        var body = PostListRenderer.BuildPostHeader(post, basePath, config?.Author, config?.AuthorImage)
                 + PostListRenderer.BuildCover(post, basePath)
                 + post.HtmlContent
                 + PostListRenderer.BuildPostNav(older, newer, basePath);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: post.Title,
            ContentHtml: body,
            Description: post.Description ?? post.Excerpt,
            CanonicalPath: post.Url,
            IsArticle: true,
            TocHtml: tocHtml));
    }

    private static async Task RenderTagIndex(HttpContext ctx, PostService posts, BlogPageResponder responder)
    {
        var view = await posts.GetViewAsync(ctx.RequestAborted);
        var content = TagListRenderer.BuildIndex(view.Tags, responder.BasePath);
        await responder.WriteAsync(ctx, new BlogPageView("Tags", content, CanonicalPath: "tags"));
    }

    private static async Task RenderTag(string tag, HttpContext ctx, PostService posts, BlogPageResponder responder)
    {
        var matches = await posts.GetByTagAsync(tag, ctx.RequestAborted);
        if (matches.Count == 0)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var content = PostListRenderer.BuildList(matches, responder.BasePath, heading: $"Tagged “{tag}”");
        await responder.WriteAsync(ctx, new BlogPageView(
            Title: $"Tagged {tag}",
            ContentHtml: content,
            CanonicalPath: $"tags/{Models.PagePath.SlugifySegment(tag)}"));
    }

    private static async Task RenderArchive(HttpContext ctx, PostService posts, BlogPageResponder responder)
    {
        var years = await posts.GetArchiveAsync(ctx.RequestAborted);
        var content = ArchiveRenderer.Build(years, responder.BasePath);
        await responder.WriteAsync(ctx, new BlogPageView("Archive", content, CanonicalPath: "archive"));
    }
}
