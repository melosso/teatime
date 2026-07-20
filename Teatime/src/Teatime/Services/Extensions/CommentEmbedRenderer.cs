using System.Text.Encodings.Web;
using Teatime.Services.Rendering;

namespace Teatime.Services.Extensions;

/// <summary>Renders the comment mount that sits under a post, carrying the request's CSP nonce.</summary>
public static class CommentEmbedRenderer
{
    public static string Build(ICommentProvider? provider, string? nonce, string canonicalUrl, string? lang)
    {
        if (provider is not Remark42Provider remark)
            return string.Empty;

        var l = Localization.Current;
        var nonceAttr = nonce is { Length: > 0 }
            ? $" nonce=\"{HtmlEncoder.Default.Encode(nonce)}\""
            : string.Empty;

        var config = string.Join(",",
            $"host:'{Localization.JsEncode(remark.BaseUrl)}'",
            $"site_id:'{Localization.JsEncode(remark.SiteId)}'",
            $"url:'{Localization.JsEncode(canonicalUrl)}'",
            $"max_shown_comments:{remark.MaxShownComments}",
            $"locale:'{Localization.JsEncode(remark.Locale ?? lang ?? "en")}'",
            $"theme:'{Localization.JsEncode(remark.Theme == "auto" ? "light" : remark.Theme)}'",
            "no_footer:true",
            "components:['embed']");

        // Teatime "light" leaves data-theme unset and follows the OS, so eff() needs the media-query fallback.
        // remark42 caches its own theme and re-applies it after boot, so sync() re-asserts for a few seconds.
        var follow = remark.Theme == "auto"
            ? """
              function eff() {
                  var attr = document.documentElement.getAttribute('data-theme');
                  if (attr === 'dark' || attr === 'light') { return attr; }
                  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
              }
              function apply() {
                  if (window.REMARK42 && window.REMARK42.changeTheme) { window.REMARK42.changeTheme(eff()); }
              }
              function sync() {
                  var n = 0;
                  (function retry() {
                      if (window.REMARK42 && window.REMARK42.changeTheme) {
                          apply();
                          if (n++ < 12) { setTimeout(retry, 300); }
                      } else if (n++ < 50) {
                          setTimeout(retry, 100);
                      }
                  })();
              }
              remark_config.theme = eff();
              new MutationObserver(apply).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
              if (window.matchMedia) { window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', apply); }
              """
            : string.Empty;
        var afterBoot = remark.Theme == "auto" ? "sync();" : string.Empty;

        var loading = HtmlEncoder.Default.Encode(l.CommentsLoading);
        var noScript = HtmlEncoder.Default.Encode(l.CommentsNoScript);

        // Remark42's module-aware loader: a fixed /web/embed.js never boots the modern .mjs build.
        const string loader = "!function(e,n){for(var o=0;o<e.length;o++){var r=n.createElement(\"script\")," +
            "c=\".js\",d=n.head||n.body;\"noModule\"in r?(r.type=\"module\",c=\".mjs\"):r.async=!0,r.defer=!0," +
            "r.src=remark_config.host+\"/web/\"+e[o]+c,d.appendChild(r)}}(remark_config.components||[\"embed\"],document);";

        // Boot near the viewport (else at load): remark42's iframe measurement forces layout, flashing unstyled content if it runs before load.
        var boot = $$"""
            function boot() {
                {{loader}}
                {{afterBoot}}
            }
            var mount = document.getElementById('remark42');
            if ('IntersectionObserver' in window && mount) {
                var io = new IntersectionObserver(function (entries) {
                    if (entries[0].isIntersecting) { io.disconnect(); boot(); }
                }, { rootMargin: '600px' });
                io.observe(mount);
            } else if (document.readyState === 'complete') {
                boot();
            } else {
                window.addEventListener('load', boot);
            }
            """;

        var script = $$"""
            <script{{nonceAttr}}>
            var remark_config = {{{config}}};
            (function () {
            {{follow}}
            {{boot}}
            })();
            </script>
            """;

        // #remark42 must ship empty, else remark42 takes the placeholder as its frame and never mounts.
        return Styles(nonceAttr)
             + $"<section class=\"teatime-comments\" aria-label=\"Comments\">"
             + $"<p class=\"teatime-comments__status\" role=\"status\">{loading}<span class=\"teatime-comments__dots\" aria-hidden=\"true\"></span></p>"
             + "<div id=\"remark42\"></div>"
             + $"<noscript><p class=\"teatime-comments__status teatime-comments__status--static\">{noScript}</p></noscript>"
             + "</section>"
             + script;
    }

    private static string Styles(string nonceAttr) => $$"""
        <style{{nonceAttr}}>
        .teatime-comments {
            margin: clamp(2.75rem, 6vw, 4.5rem) 0 0;
            padding-top: clamp(1.75rem, 4vw, 2.5rem);
            border-top: 1px solid var(--border);
            font-family: var(--font-sans);
        }
        .teatime-comments #remark42 {
            display: block;
            min-height: 3rem;
        }
        .teatime-comments #remark42 iframe {
            max-width: 100%;
        }
        .teatime-comments:has(#remark42 > *) .teatime-comments__status { display: none; }
        /* Element + class beats the prose rules the thread sits inside. */
        .teatime-comments p.teatime-comments__status {
            display: flex;
            align-items: baseline;
            gap: 0.15rem;
            margin: 0;
            color: var(--text-muted);
            font-size: 0.9rem;
            letter-spacing: 0.01em;
        }
        .teatime-comments p.teatime-comments__status--static { color: var(--text-muted); }
        .teatime-comments__dots::after {
            content: "";
            animation: teatime-comments-dots 1.4s steps(1, end) infinite;
        }
        @keyframes teatime-comments-dots {
            0% { content: ""; }
            25% { content: "."; }
            50% { content: ".."; }
            75% { content: "..."; }
        }
        @media (prefers-reduced-motion: reduce) {
            .teatime-comments__dots::after { content: "..."; animation: none; }
        }
        </style>
        """;
}
