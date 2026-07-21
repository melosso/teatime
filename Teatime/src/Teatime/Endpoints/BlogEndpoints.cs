using Teatime.Configuration;
using Teatime.Services;
using Teatime.Services.Layout;
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

    private static async Task RenderAuthorIndex(HttpContext ctx, AuthorService authors, BlogPageResponder responder, ContentService content)
    {
        var route = Models.ReservedRoutes.Authors;
        var basePath = responder.BasePath;
        var custom = await LookupCustom(content, route.Slug, ctx.RequestAborted);
        if (!route.IsEnabled(content.SiteConfig) || custom?.Enabled == false)
        {
            await responder.Write404Async(ctx);
            return;
        }

        if (TryRedirect(ctx, custom, basePath))
            return;

        var html = RenderCustomIndex(custom, AuthorRenderer.BuildGrid(authors.GetListed(), basePath), route.Slug, basePath,
            () => AuthorRenderer.BuildIndex(authors.GetListed(), basePath));

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: custom?.Title ?? route.Title,
            ContentHtml: html,
            Description: custom?.Description,
            CanonicalPath: route.Slug));
    }

    private static async Task RenderAuthor(string slug, HttpContext ctx, AuthorService authors, PostService posts, BlogPageResponder responder, DocsOptions options, int page)
    {
        var author = authors.GetBySlug(slug);
        if (author is null || author.Hidden || page < 1)
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
                 + PostListRenderer.BuildList(slice, basePath, emptyMessage: Localization.Current.AuthorEmpty)
                 + PostListRenderer.BuildLoadMore(nextUrl);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? Localization.Current.AuthorPagedTitle(author.Name, page) : author.Name,
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
            content = PostListRenderer.BuildList(slice, basePath, emptyMessage: Localization.Current.HomeEmpty);
        }
        else if (page == 1)
        {
            var rest = slice.Skip(1).ToList();
            content = PostListRenderer.BuildFeaturedLead(slice[0], basePath)
                    + (rest.Count > 0 ? PostListRenderer.BuildList(rest, basePath, showPreview: true) : string.Empty);
        }
        else
        {
            content = PostListRenderer.BuildList(slice, basePath, showPreview: true);
        }
        content += PostListRenderer.BuildPager(page, totalPages, basePath);

        var heading = page > 1 ? Localization.Current.HomeHeadingPaged(page) : Localization.Current.HomeHeadingLatest;
        content = $"<h1 class=\"sr-only\">{LayoutProvider.HtmlEncode(heading)}</h1>" + content;

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? Localization.Current.PageTitlePaged(page) : string.Empty,
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

        var body = PostListRenderer.BuildPostHeader(post, basePath, authorName, authorImage, author is { Hidden: false } ? author.Url : null)
                 + PostListRenderer.BuildCover(post, basePath)
                 + post.HtmlContent
                 + PostListRenderer.BuildPostNav(older, newer, basePath);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: post.Title,
            ContentHtml: body,
            Description: post.Description ?? post.Excerpt,
            CanonicalPath: post.Url,
            IsArticle: true,
            ShowComments: true,
            Image: post.Cover,
            Modified: post.Updated ?? post.Date));
    }

    private static async Task RenderTagIndex(HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content)
    {
        var route = Models.ReservedRoutes.Tags;
        var view = await posts.GetViewAsync(ctx.RequestAborted);
        var basePath = responder.BasePath;
        var custom = await LookupCustom(content, route.Slug, ctx.RequestAborted);
        if (!route.IsEnabled(content.SiteConfig) || custom?.Enabled == false)
        {
            await responder.Write404Async(ctx);
            return;
        }

        if (TryRedirect(ctx, custom, basePath))
            return;

        var html = RenderCustomIndex(custom, TagListRenderer.BuildCloud(view.Tags, basePath), route.Slug, basePath,
            () => TagListRenderer.BuildIndex(view.Tags, basePath));

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: custom?.Title ?? route.Title,
            ContentHtml: html,
            Description: custom?.Description,
            CanonicalPath: route.Slug));
    }

    private static async Task RenderTag(string tag, HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content, DocsOptions options, int page)
    {
        var matches = await posts.GetByTagAsync(tag, ctx.RequestAborted);
        if (!Models.ReservedRoutes.Tags.IsEnabled(content.SiteConfig) || matches.Count == 0 || page < 1)
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
        var l = Localization.Current;
        var html = PostListRenderer.BuildList(slice, basePath, heading: page > 1 ? l.TaggedHeadingPaged(tag, page) : l.TaggedHeading(tag))
                 + PostListRenderer.BuildLoadMore(nextUrl);

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: page > 1 ? l.TaggedTitlePaged(tag, page) : l.TaggedTitle(tag),
            ContentHtml: html,
            CanonicalPath: page > 1 ? $"tags/{slug}/page/{page}" : $"tags/{slug}"));
    }

    private static async Task RenderArchive(HttpContext ctx, PostService posts, BlogPageResponder responder, ContentService content)
    {
        var route = Models.ReservedRoutes.Archive;
        var years = await posts.GetArchiveAsync(ctx.RequestAborted);
        var basePath = responder.BasePath;
        var custom = await LookupCustom(content, route.Slug, ctx.RequestAborted);
        if (!route.IsEnabled(content.SiteConfig) || custom?.Enabled == false)
        {
            await responder.Write404Async(ctx);
            return;
        }

        if (TryRedirect(ctx, custom, basePath))
            return;

        var html = RenderCustomIndex(custom, ArchiveRenderer.BuildYears(years, basePath), route.Slug, basePath,
            () => ArchiveRenderer.Build(years, basePath));

        await responder.WriteAsync(ctx, new BlogPageView(
            Title: custom?.Title ?? route.Title,
            ContentHtml: html,
            Description: custom?.Description,
            CanonicalPath: route.Slug));
    }

    // A user-authored content/pages/{name}.md (or content/{name}.md) that owns the title,
    // description, and heading, with {{name}} marking where the generated list is injected.
    private static async ValueTask<Models.DocumentationPage?> LookupCustom(ContentService content, string name, CancellationToken ct) =>
        await content.GetPageAsync($"pages/{name}", ct) ?? await content.GetPageAsync(name, ct);

    private static bool TryRedirect(HttpContext ctx, Models.DocumentationPage? custom, string basePath)
    {
        if (custom?.Redirect is not { Length: > 0 } target)
            return false;

        ctx.Response.Redirect(PageRequestHandler.ResolveRedirect(target, basePath), permanent: false);
        return true;
    }

    // Custom index page: optional cover placed below the heading (reading width, like posts),
    // then the prose with the generated list injected at the {{name}} token. Falls back to the
    // built-in renderer when no page.md exists.
    private static string RenderCustomIndex(Models.DocumentationPage? custom, string listHtml, string name, string basePath, Func<string> fallback)
    {
        if (custom is null) return fallback();
        var body = InsertCoverAfterHeading(custom.HtmlContent, PostListRenderer.BuildCover(custom.Cover, basePath));
        return Inject(body, listHtml, name);
    }

    // Splices the cover in just after the first heading so it reads title-then-cover, matching posts.
    private static string InsertCoverAfterHeading(string html, string cover)
    {
        if (cover.Length == 0) return html;
        var wrapped = $"<div class=\"index-cover\">{cover}</div>";
        var idx = html.IndexOf("</h1>", StringComparison.Ordinal);
        return idx < 0 ? wrapped + html : html.Insert(idx + 5, wrapped);
    }

    private static string Inject(string contentHtml, string listHtml, string name)
    {
        var token = $"{{{{{name}}}}}";
        if (contentHtml.Contains(token, StringComparison.Ordinal))
            return $"<div class=\"prose\">{contentHtml
                .Replace($"<p>{token}</p>", listHtml, StringComparison.Ordinal)
                .Replace(token, listHtml, StringComparison.Ordinal)}</div>";
        return $"<div class=\"prose\">{contentHtml}</div>{listHtml}";
    }
}
