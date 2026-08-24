using Teatime.Configuration;
using Teatime.Models;
using Teatime.Services;
using Teatime.Services.Layout;
using Teatime.Services.Theming;

namespace Teatime.Endpoints;

internal static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/teatime.css", HttpVerbs.GetAndHead, GetStylesheet);
        app.MapMethods("/teatime.js", HttpVerbs.GetAndHead, GetScript);
        return app;
    }

    private static async Task GetStylesheet(HttpContext ctx, ContentService content, ThemeOptions theme, PageRequestSettings settings)
    {
        var config = content.SiteConfig;
        var themeSelection = ThemeSelection.Resolve(theme, settings.CliTheme, config?.Theme);
        var activeStructure = StructureRegistry.Resolve(settings.CliStructure ?? config?.Structure);
        var themeTokenCss = ThemeCssBuilder.BuildTokenCss(themeSelection.Theme, themeSelection.Mode);
        var componentCss = activeStructure.ComponentCss.Length > 0
            ? themeSelection.Theme.ComponentCss + "\n" + activeStructure.ComponentCss
            : themeSelection.Theme.ComponentCss;

        var asset = LayoutProvider.GetStylesAsset(themeTokenCss, componentCss, settings.BasePath);
        WriteCacheHeaders(ctx);
        ctx.Response.ContentType = "text/css; charset=utf-8";
        await ctx.Response.WriteAsync(asset.Body);
    }

    private static async Task GetScript(HttpContext ctx, ContentService content, ThemeOptions theme, DocsOptions docsOptions, PageRequestSettings settings)
    {
        var config = content.SiteConfig;
        var themeSelection = ThemeSelection.Resolve(theme, settings.CliTheme, config?.Theme);
        var enableDarkMode = themeSelection.Mode == ThemeMode.Auto;

        var asset = LayoutProvider.GetScriptsAsset(docsOptions.EnableHotReload, enableDarkMode, content.BuildVersion, settings.BasePath, docsOptions.IsStaticExport);
        WriteCacheHeaders(ctx);
        ctx.Response.ContentType = "text/javascript; charset=utf-8";
        await ctx.Response.WriteAsync(asset.Body);
    }

    private static void WriteCacheHeaders(HttpContext ctx) =>
        ctx.Response.Headers.CacheControl = ctx.Request.Query.ContainsKey("v")
            ? "public,max-age=31536000,immutable"
            : "no-cache";
}
