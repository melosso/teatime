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

        // theme:auto follows Teatime's own toggle, which writes data-theme on the root element.
        var follow = remark.Theme == "auto"
            ? """
              function apply(){var t=document.documentElement.getAttribute('data-theme')==='dark'?'dark':'light';
              if(window.REMARK42&&window.REMARK42.changeTheme){window.REMARK42.changeTheme(t);}}
              new MutationObserver(apply).observe(document.documentElement,{attributes:true,attributeFilter:['data-theme']});
              window.addEventListener('load',apply);
              """
            : string.Empty;

        var src = HtmlEncoder.Default.Encode(remark.BaseUrl);

        return "<section class=\"teatime-comments\" aria-label=\"Comments\"><div id=\"remark42\"></div></section>"
             + $"<script{nonceAttr}>var remark_config={{{config}}};{follow}</script>"
             + $"<script{nonceAttr} defer src=\"{src}/web/embed.js\"></script>";
    }
}
