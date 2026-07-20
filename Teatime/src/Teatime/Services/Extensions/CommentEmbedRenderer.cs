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
            "components:['embed']");

        var follow = remark.Theme == "auto"
            ? """
              function apply(){var t=document.documentElement.getAttribute('data-theme')==='dark'?'dark':'light';
              if(window.REMARK42&&window.REMARK42.changeTheme){window.REMARK42.changeTheme(t);}}
              new MutationObserver(apply).observe(document.documentElement,{attributes:true,attributeFilter:['data-theme']});
              window.addEventListener('load',apply);
              """
            : string.Empty;

        var loading = HtmlEncoder.Default.Encode(l.CommentsLoading);
        var noScript = HtmlEncoder.Default.Encode(l.CommentsNoScript);

        // Remark42's module-aware loader: a fixed /web/embed.js never boots the modern .mjs build.
        const string loader = "!function(e,n){for(var o=0;o<e.length;o++){var r=n.createElement(\"script\")," +
            "c=\".js\",d=n.head||n.body;\"noModule\"in r?(r.type=\"module\",c=\".mjs\"):r.async=!0,r.defer=!0," +
            "r.src=remark_config.host+\"/web/\"+e[o]+c,d.appendChild(r)}}(remark_config.components||[\"embed\"],document);";

        return Styles(nonceAttr)
             + $"<section class=\"teatime-comments\" aria-label=\"Comments\">"
             + $"<div id=\"remark42\"><p class=\"teatime-comments__status\" role=\"status\">{loading}<span class=\"teatime-comments__dots\" aria-hidden=\"true\"></span></p></div>"
             + $"<noscript><p class=\"teatime-comments__status teatime-comments__status--static\">{noScript}</p></noscript>"
             + "</section>"
             + $"<script{nonceAttr}>var remark_config={{{config}}};{follow}{loader}</script>";
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
