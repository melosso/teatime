using Teatime.Configuration;
using Teatime.Services;
using Teatime.Services.Rendering;

namespace Teatime.Endpoints;

internal static class BlogEndpoints
{
    public static IEndpointRouteBuilder MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", (HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options, ContentService content) =>
            RenderHome(ctx, posts, responder, options, 1, content));

        app.MapGet("/page/{n:int}", (int n, HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options, ContentService content) =>
            RenderHome(ctx, posts, responder, options, n, content));

        app.MapGet("/posts/{slug}", RenderPost);
        app.MapGet("/tags", RenderTagIndex);
        app.MapGet("/tags/{tag}", (string tag, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content, DocsOptions options) =>
            RenderTag(tag, ctx, posts, responder, content, options, 1));
        app.MapGet("/tags/{tag}/page/{n:int}", (string tag, int n, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content, DocsOptions options) =>
            RenderTag(tag, ctx, posts, responder, content, options, n));
        app.MapGet("/archive", RenderArchive);
        app.MapGet("/authors", RenderAuthorIndex);
        app.MapGet("/authors/{slug}", (string slug, HttpContext ctx, AuthorService authors, PostService posts, BlogPageResponder responder, DocsOptions options) =>
            RenderAuthor(slug, ctx, authors, posts, responder, options, 1));
        app.MapGet("/authors/{slug}/page/{n:int}", (string slug, int n, HttpContext ctx, AuthorService authors, PostService posts, BlogPageResponder responder, DocsOptions options) =>
            RenderAuthor(slug, ctx, authors, posts, responder, options, n));
        return app;
    }

    private static async Task RenderAuthorIndex(HttpContext ctx, AuthorService authors, BlogPageResponder responder)
    {
        var html = AuthorRenderer.BuildIndex(authors.GetAll(), responder.BasePath);
        await responder.WriteAsync(ctx, new BlogPageView("Authors", html, CanonicalPath: "authors"));
    }

    private static async Task RenderAuthor(string slug, HttpContext ctx, AuthorService authors, PostService posts, BlogPageResponder responder, DocsOptions options, int page)
    {
        var author = authors.GetBySlug(slug);
        if (author is null || page < 1)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var basePath = responder.BasePath;
        var all = await posts.GetByAuthorAsync(author.Id, ctx.RequestAborted);
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)options.PageSize));
        if (page > totalPages && page > 1)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var slice = all.Skip((page - 1) * options.PageSize).Take(options.PageSize).ToList();
        var nextUrl = page < totalPages ? UrlPaths.Href(basePath, $"{author.Url}/page/{page + 1}") : null;
        var html = AuthorRenderer.BuildHeader(author, basePath)
                 + PostListRenderer.BuildList(slice, basePath, emptyMessage: "No posts here yet.")
                 + PostListRenderer.BuildLoadMore(nextUrl);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? $"{author.Name}, page {page}" : author.Name,
            ContentHtml: html,
            CanonicalPath: page > 1 ? $"{author.Url}/page/{page}" : author.Url));
    }

    private static async Task RenderHome(HttpContext ctx, PostService posts, BlogPageResponder responder, DocsOptions options, int page, ContentService contentService)
    {
        if (page < 1)
        {
            ctx.Response.Redirect(responder.HomeUrl);
            return;
        }

        var (slice, totalPages) = await posts.GetPageAsync(page, options.PageSize, ctx.RequestAborted, contentService.SiteConfig?.HomeLimit);
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

        var heading = page > 1 ? $"Posts, page {page}" : "Latest posts";
        content = $"<h1 class=\"sr-only\">{heading}</h1>" + content;

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? $"Page {page}" : string.Empty,
            ContentHtml: content,
            CanonicalPath: page > 1 ? $"page/{page}" : ""));
    }

    private static async Task RenderPost(string slug, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content, AuthorService authors)
    {
        var post = await posts.GetBySlugAsync(slug, ctx.RequestAborted);
        if (post is null)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var basePath = responder.BasePath;
        var config = content.SiteConfig;
        var author = authors.GetById(post.AuthorId);
        var authorName = author?.Name ?? post.AuthorId ?? config?.Author;
        var authorImage = author?.Image ?? config?.AuthorImage;
        var (older, newer) = await posts.GetPrevNextAsync(post.Slug, ctx.RequestAborted);
        var tocHtml = post.ShowToc && post.Headings.Count > 0
            ? TocHtmlRenderer.BuildTocHtml(post.Headings)
            : null;

        var body = PostListRenderer.BuildPostHeader(post, basePath, authorName, authorImage, author?.Url)
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

    private static async Task RenderTagIndex(HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content)
    {
        var view = await posts.GetViewAsync(ctx.RequestAborted);
        var basePath = responder.BasePath;
        var custom = await LookupCustom(content, "tags", ctx.RequestAborted);
        if (content.SiteConfig?.Tags == false || custom?.Enabled == false)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var html = custom is not null
            ? Inject(custom.HtmlContent, TagListRenderer.BuildCloud(view.Tags, basePath), "tags")
            : TagListRenderer.BuildIndex(view.Tags, basePath);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: custom?.Title ?? "Tags",
            ContentHtml: html,
            Description: custom?.Description,
            CanonicalPath: "tags"));
    }

    private static async Task RenderTag(string tag, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content, DocsOptions options, int page)
    {
        var matches = await posts.GetByTagAsync(tag, ctx.RequestAborted);
        if (content.SiteConfig?.Tags == false || matches.Count == 0 || page < 1)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var slug = Models.PagePath.SlugifySegment(tag);
        var basePath = responder.BasePath;
        var totalPages = Math.Max(1, (int)Math.Ceiling(matches.Count / (double)options.PageSize));
        if (page > totalPages && page > 1)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var slice = matches.Skip((page - 1) * options.PageSize).Take(options.PageSize).ToList();
        var nextUrl = page < totalPages ? UrlPaths.Href(basePath, $"tags/{slug}/page/{page + 1}") : null;
        var html = PostListRenderer.BuildList(slice, basePath, heading: page > 1 ? $"Tagged “{tag}”, page {page}" : $"Tagged “{tag}”")
                 + PostListRenderer.BuildLoadMore(nextUrl);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? $"Tagged {tag}, page {page}" : $"Tagged {tag}",
            ContentHtml: html,
            CanonicalPath: page > 1 ? $"tags/{slug}/page/{page}" : $"tags/{slug}"));
    }

    private static async Task RenderArchive(HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content)
    {
        var years = await posts.GetArchiveAsync(ctx.RequestAborted);
        var basePath = responder.BasePath;
        var custom = await LookupCustom(content, "archive", ctx.RequestAborted);
        if (content.SiteConfig?.Archive == false || custom?.Enabled == false)
        {
            await responder.Write404Async(ctx);
            return;
        }

        var html = custom is not null
            ? Inject(custom.HtmlContent, ArchiveRenderer.BuildYears(years, basePath), "archive")
            : ArchiveRenderer.Build(years, basePath);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: custom?.Title ?? "Archive",
            ContentHtml: html,
            Description: custom?.Description,
            CanonicalPath: "archive"));
    }

    // A user-authored content/pages/{name}.md (or content/{name}.md) that owns the title,
    // description, and heading, with {{name}} marking where the generated list is injected.
    private static async ValueTask<Models.DocumentationPage?> LookupCustom(ContentService content, string name, CancellationToken ct) =>
        await content.GetPageAsync($"pages/{name}", ct) ?? await content.GetPageAsync(name, ct);

    private static string Inject(string contentHtml, string listHtml, string name)
    {
        var token = $"{{{{{name}}}}}";
        if (contentHtml.Contains(token, StringComparison.Ordinal))
            return contentHtml
                .Replace($"<p>{token}</p>", listHtml, StringComparison.Ordinal)
                .Replace(token, listHtml, StringComparison.Ordinal);
        return contentHtml + listHtml;
    }
}
