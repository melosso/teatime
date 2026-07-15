using System.Text;

namespace Teatime.Services.Layout;

public static partial class LayoutProvider
{
    public static string GetLayout(
        string title,
        string content,
        string navigationHtml,
        string? tocHtml,
        string breadcrumbHtml,
        string paginationHtml,
        string? themeCss = null,
        string? brandText = null,
        string? brandImage = null,
        bool enableDarkMode = true,
        string? footerHtml = null,
        string? socialLinksHtml = null,
        bool enableLiveReload = false,
        bool staticSearch = false,
        long buildVersion = 0,
        string? favicon = null,
        string? description = null,
        string? mobileTopNavHtml = null,
        bool isHomePage = false,
        string? lastUpdatedHtml = null,
        string? editLinkHtml = null,
        bool showScrollIndicator = true,
        string basePath = "",
        string lang = "en",
        string? headTagsHtml = null,
        string? keywordsHtml = null,
        string? canonicalUrl = null,
        string? nonce = null,
        bool hasMath = false,
        bool hasMermaid = false,
        string? pageControlsHtml = null,
        string? rssDiscoveryHtml = null,
        string? promoBarHtml = null,
        bool isArticle = false,
        string? siteNavHtml = null,
        string? footerText = null)
    {
        var scrollIndicatorHtml = showScrollIndicator ? @"<div id=""scroll-indicator""></div>" : "";
        var faviconHtml = BuildFaviconLink(favicon, basePath);
        var homeHref = basePath.Length == 0 ? "/" : $"{basePath}/";
        var brandImageSrc = ResolveAssetUrl(brandImage, basePath);
        var descriptionHtml = !string.IsNullOrWhiteSpace(description)
            ? $"<meta name=\"description\" content=\"{HtmlEncode(description)}\">"
            : "";

        var hasLeftNav = !string.IsNullOrWhiteSpace(navigationHtml);
        var layoutClass = isHomePage
            ? "layout teatime-home-layout"
            : hasLeftNav ? "layout" : "layout no-left-sidebar";
        var mobileSocialHtml = !string.IsNullOrWhiteSpace(socialLinksHtml)
            ? $@"<div class=""sidebar-social-links"">{socialLinksHtml}</div>"
            : "";
        var sidebarLeftHtml = $@"
        <aside class=""sidebar-left"" id=""sidebar-left"" aria-label=""Documentation navigation"">
            {mobileTopNavHtml}
            {navigationHtml}
            {mobileSocialHtml}
        </aside>";
        var breadcrumbAndTocHtml = isHomePage ? "" : $@"
            <nav class=""breadcrumb"" aria-label=""Breadcrumb"">
                {breadcrumbHtml}
                {pageControlsHtml}
            </nav>
            {(tocHtml is null ? "" : $@"<details class=""toc-inline"">
                <summary>On this page</summary>
                <ul class=""toc-list"">
                    {tocHtml}
                </ul>
            </details>")}";
        var sidebarRightHtml = isHomePage || string.IsNullOrWhiteSpace(tocHtml)
            ? ""
            : $@"
        <aside class=""sidebar-right"" aria-label=""Table of contents"">
            <div class=""toc-title"">On This Page</div>
            <div class=""toc-list-wrapper"">
                <div class=""toc-indicator"" aria-hidden=""true""></div>
                <ul class=""toc-list"">
                    {tocHtml}
                </ul>
            </div>
        </aside>";
        var contentClass = isArticle ? "content reading" : "content";
        // Home pages never show "last updated" or prev/next pagination, regardless of caller input.
        var paginationBlock = isHomePage ? "" : paginationHtml;
        var lastUpdatedBlock = isHomePage ? "" : lastUpdatedHtml;
        var editLinkBlock = isHomePage ? "" : editLinkHtml;
        var pageMetaBlock = string.IsNullOrEmpty(editLinkBlock) && string.IsNullOrEmpty(lastUpdatedBlock)
            ? ""
            : $@"<div class=""page-meta""><div class=""page-meta-left"">{editLinkBlock}</div><div class=""page-meta-right"">{lastUpdatedBlock}</div></div>";

        const string darkVars = @"
                color-scheme: dark;
                --shadow-md: 0 8px 24px rgba(0, 0, 0, 0.45);
                --shadow-lg: 0 24px 64px rgba(0, 0, 0, 0.55);
                --bg-color: #0F1210;
                --sidebar-bg: #171A17;
                --text-color: #E7E9E4;
                --text-muted: #9AA09A;
                --accent: #7FA588;
                --accent-light: #1E271F;
                --border: rgba(255, 255, 255, 0.10);
                --code-bg: #171A17;
                --alert-note: #2f81f7;
                --alert-tip: #3fb950;
                --alert-important: #a371f7;
                --alert-warning: #d4a72c;
                --alert-caution: #f85149;
                --promo-text: #dfe6e1;";

        var darkModeMediaQuery = enableDarkMode
            ? $@"@media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) {{{darkVars}
            }}
        }}
        :root[data-theme=""dark""] {{{darkVars}
        }}"
            : "";

        var nonceAttr = nonce is { Length: > 0 } ? $" nonce=\"{nonce}\"" : "";
        // Pre-<style> so no transition can fire; without a stored theme, data-theme stays unset so CSS follows live OS changes.
        var themeInitScript = enableDarkMode
            ? "<script" + nonceAttr + ">(function(){try{var t=localStorage.getItem('teatime-theme');if(t==='dark'||t==='light'){var r=document.documentElement;r.setAttribute('data-theme',t);r.style.colorScheme=t;}}catch(e){}})();</script>"
            : "";

        var themeToggleHtml = enableDarkMode
            ? @"<button type=""button"" class=""theme-toggle"" id=""theme-toggle"" role=""switch"" aria-checked=""false"" aria-label=""Toggle dark mode"">
                <span class=""theme-toggle-thumb"">
                    <svg class=""icon-sun"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41""/></svg>
                    <svg class=""icon-moon"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z""/></svg>
                </span>
            </button>"
            : "";

        var colorSchemeMeta = enableDarkMode
            ? "<meta name=\"color-scheme\" content=\"light dark\">"
            : "<meta name=\"color-scheme\" content=\"light\">";

        var canonicalLink = canonicalUrl is { Length: > 0 }
            ? $"<link rel=\"canonical\" href=\"{HtmlEncode(canonicalUrl)}\">"
            : string.Empty;

        var socialMeta = BuildSocialMeta(canonicalUrl, title, description, isHomePage);

        return $@"
<!DOCTYPE html>
<html lang=""{HtmlEncode(lang)}"">
<head>
    <meta charset=""UTF-8"">
    {themeInitScript}
    {colorSchemeMeta}
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncode(title)}</title>
    {descriptionHtml}
    {keywordsHtml}
    {canonicalLink}
    {socialMeta}
    {rssDiscoveryHtml}
    {faviconHtml}
    {headTagsHtml}
    {themeCss}
    {GetStyles(darkModeMediaQuery, nonce)}
    {(hasMath ? $"<link rel=\"stylesheet\" href=\"{basePath}/css/katex.min.css\">" : "")}
    {(hasMermaid ? $"<script defer src=\"{basePath}/js/mermaid.min.js\"></script>" : "")}
</head>
<body>
    <a href=""#main-content"" class=""skip-link"">Skip to content</a>
    {promoBarHtml}
    {scrollIndicatorHtml}
    <header class=""topbar"">
        <div class=""masthead-actions"">
            <button type=""button"" class=""icon-btn search-trigger"" id=""search-trigger""
                    aria-haspopup=""dialog"" aria-controls=""search-modal"" aria-label=""Search"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""11"" cy=""11"" r=""7""/><path d=""M21 21l-4.3-4.3""/></svg>
            </button>
            <a class=""icon-btn rss-link"" href=""{basePath}/feed.xml"" aria-label=""RSS feed"" title=""RSS feed"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><path d=""M4 11a9 9 0 0 1 9 9""/><path d=""M4 4a16 16 0 0 1 16 16""/><circle cx=""5"" cy=""19"" r=""1.4"" fill=""currentColor"" stroke=""none""/></svg>
            </a>
            {themeToggleHtml}
        </div>
        <a class=""brand"" href=""{homeHref}"">{(brandImageSrc is not null ? $"<img src=\"{HtmlEncode(brandImageSrc)}\" alt=\"\">" : "<span class=\"brand-mark\" aria-hidden=\"true\">\U0001F375</span>")}{brandText ?? "Teatime"}</a>
        {siteNavHtml}
    </header>
    <div class=""search-overlay"" id=""search-overlay"" hidden>
        <div class=""search-modal"" id=""search-modal"" role=""dialog"" aria-modal=""true"" aria-labelledby=""search-modal-label"">
            <h2 id=""search-modal-label"" class=""sr-only"">Search documentation</h2>
            <div class=""search-modal-header"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""11"" cy=""11"" r=""7""/><path d=""M21 21l-4.3-4.3""/></svg>
                <input type=""search"" class=""search-modal-input"" id=""search-modal-input""
                       placeholder=""Search documentation..."" autocomplete=""off"" enterkeyhint=""search""
                       role=""combobox"" aria-expanded=""false"" aria-controls=""search-modal-results""
                       aria-autocomplete=""list"" aria-label=""Search documentation"">
                <button type=""button"" class=""search-modal-close icon-btn"" id=""search-modal-close"" aria-label=""Close search"">
                    <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><path d=""M18 6L6 18M6 6l12 12""/></svg>
                </button>
            </div>
            <div class=""search-modal-results"" id=""search-modal-results"" role=""listbox"" aria-label=""Search results""></div>
            <div class=""sr-only"" id=""search-modal-status"" role=""status"" aria-live=""polite""></div>
            <ul class=""DocSearch-Commands"" aria-hidden=""true"">
                <li>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""Arrow down"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><path d=""M12 5v14""></path><path d=""m19 12-7 7-7-7""></path></g></svg></kbd>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""Arrow up"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><path d=""m5 12 7-7 7 7""></path><path d=""M12 19V5""></path></g></svg></kbd>
                    <span class=""DocSearch-Label"">Navigate</span>
                </li>
                <li>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""Enter key"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><polyline points=""9 10 4 15 9 20""></polyline><path d=""M20 4v7a4 4 0 0 1-4 4H4""></path></g></svg></kbd>
                    <span class=""DocSearch-Label"">Select</span>
                </li>
                <li>
                    <kbd class=""DocSearch-Commands-Key""><span class=""DocSearch-Escape-Key"">ESC</span></kbd>
                    <span aria-label=""Escape key"" class=""DocSearch-Label"">Close</span>
                </li>
            </ul>
        </div>
    </div>
    <div class=""sidebar-overlay"" id=""sidebar-overlay""></div>
    <div class=""{layoutClass}"">
        {sidebarLeftHtml}
        <main class=""main-container"" id=""main-content"" tabindex=""-1"">
            {breadcrumbAndTocHtml}
            <article class=""{contentClass}"">
                {content}
                {pageMetaBlock}
                {paginationBlock}
                {footerHtml}
            </article>
        </main>
        {sidebarRightHtml}
    </div>
    <footer class=""site-footer"">
        <span class=""site-footer-note"">{(!string.IsNullOrEmpty(footerText) ? HtmlEncode(footerText) : $"© {DateTime.UtcNow.Year} {HtmlEncode(brandText ?? "Teatime")}")}</span>
        <a href=""{basePath}/feed.xml"">RSS</a>
        <a href=""{homeHref}archive/"">Archive</a>
        {socialLinksHtml}
    </footer>
    {GetScripts(enableLiveReload, enableDarkMode, buildVersion, basePath, nonce, staticSearch)}
</body>
</html>";
    }

    public static string Get404Layout(Func<string?, string> htmlEncode, string basePath = "", string lang = "en")
    {
        var homeHref = basePath.Length == 0 ? "/" : $"{basePath}/";
        // Build outside the interpolated block so JS/CSS braces don't need escaping.
        const string darkVars = "--bg-color:#0b0b0b;--text-color:#e5e5e5;--text-muted:#a0a0a0;--accent:#6b8e74";
        const string themeInit = "<script>(function(){" +
            "function apply(){try{var t=localStorage.getItem('teatime-theme');var r=document.documentElement;" +
            "if(t==='dark'||t==='light'){r.setAttribute('data-theme',t);r.style.colorScheme=t;}" +
            "else{r.removeAttribute('data-theme');r.style.colorScheme='';}" +
            "}catch(e){}}" +
            "apply();" +
            "window.addEventListener('pageshow',function(e){if(e.persisted)apply();});" +
            "})()</script>";
        const string darkCss = "@media (prefers-color-scheme: dark) {" +
            ":root:not([data-theme=\"light\"]) {" + darkVars + "}" +
            "}" +
            ":root[data-theme=\"dark\"] {" + darkVars + "}";
        return $@"
<!DOCTYPE html>
<html lang=""{htmlEncode(lang)}"">
<head>
    <meta charset=""UTF-8"">
    {themeInit}
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Page Not Found</title>
    <style>
        :root {{
            --bg-color: #fafafa;
            --text-color: #1a1a1a;
            --text-muted: #666666;
            --accent: #2e4a36;
            --font-sans: system-ui, -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
        }}
        {darkCss}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: var(--font-sans);
            background-color: var(--bg-color);
            color: var(--text-color);
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            line-height: 1.6;
        }}
        .not-found {{
            text-align: center;
        }}
        .not-found h1 {{
            font-size: 4rem;
            font-weight: 600;
            letter-spacing: -0.03em;
            margin-bottom: 0.5rem;
        }}
        .not-found p {{
            color: var(--text-muted);
            margin-bottom: 2rem;
        }}
        .not-found a {{
            color: var(--accent);
            text-decoration: none;
            font-weight: 500;
        }}
        .not-found a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <div class=""not-found"">
        <h1>404</h1>
        <p>The page you're looking for doesn't exist.</p>
        <a href=""{homeHref}"">Return home</a>
    </div>
</body>
</html>";
    }

    public static string HtmlEncode(string? value) =>
        value != null ? System.Net.WebUtility.HtmlEncode(value) : string.Empty;

    public static string? ResolveAssetUrl(string? url, string basePath)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.StartsWith('/'))
        {
            if (HasBasePathPrefix(url, basePath))
                return url;
            return $"{basePath}{url}";
        }

        return url;
    }

    private static bool HasBasePathPrefix(string path, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return false;
        return path.StartsWith(basePath, StringComparison.Ordinal)
            && (path.Length == basePath.Length || path[basePath.Length] == '/');
    }

    private static string BuildSocialMeta(string? canonicalUrl, string title, string? description, bool isHomePage)
    {
        if (string.IsNullOrEmpty(canonicalUrl)) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"    <meta property=\"og:type\" content=\"{(isHomePage ? "website" : "article")}\">");
        sb.AppendLine($"    <meta property=\"og:title\" content=\"{HtmlEncode(title)}\">");
        sb.AppendLine($"    <meta property=\"og:url\" content=\"{HtmlEncode(canonicalUrl)}\">");
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"    <meta property=\"og:description\" content=\"{HtmlEncode(description)}\">");
            sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{HtmlEncode(description)}\">");
        }
        sb.AppendLine("    <meta name=\"twitter:card\" content=\"summary\">");
        sb.AppendLine($"    <meta name=\"twitter:title\" content=\"{HtmlEncode(title)}\">");
        return sb.ToString();
    }

    private static string GetNonceAttr(string? nonce) =>
        nonce is { Length: > 0 } ? $" nonce=\"{nonce}\"" : string.Empty;

    private static string BuildFaviconLink(string? favicon, string basePath = "")
    {
        if (string.IsNullOrWhiteSpace(favicon))
            return string.Empty;

        // Emoji or plain text fallback; not a URL or path
        if (!favicon.StartsWith('/')
            && !favicon.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !favicon.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>{favicon}</text></svg>";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return $"<link rel=\"icon\" href=\"data:image/svg+xml;base64,{base64}\">";
        }

        var href = ResolveAssetUrl(favicon, basePath);
        return $"<link rel=\"icon\" href=\"{HtmlEncode(href)}\">";
    }
}
