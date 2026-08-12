using System.Text;
using System.Text.Encodings.Web;

namespace Teatime.Services.Extensions;

/// <summary>Renders the verified extensions as head scripts carrying the request's CSP nonce.</summary>
public static class ExtensionHeadRenderer
{
    public static string Build(ExtensionSet extensions, string? nonce)
    {
        if (extensions.IsEmpty)
            return string.Empty;

        var nonceAttr = nonce is { Length: > 0 }
            ? $" nonce=\"{HtmlEncoder.Default.Encode(nonce)}\""
            : string.Empty;

        var sb = new StringBuilder(512);
        foreach (var script in extensions.Active.SelectMany(e => e.Scripts))
        {
            sb.Append("<script").Append(nonceAttr);

            if (script.Async) sb.Append(" async");
            if (script.Defer) sb.Append(" defer");

            if (script.Src is { Length: > 0 } src)
                sb.Append(" src=\"").Append(HtmlEncoder.Default.Encode(src)).Append('"');

            if (script.Attributes is { Count: > 0 } attributes)
                foreach (var (key, value) in attributes)
                    sb.Append(' ').Append(key).Append("=\"").Append(HtmlEncoder.Default.Encode(value)).Append('"');

            // Inline bodies are built from verified settings, so they go out as-is.
            sb.Append('>').Append(script.Inline).Append("</script>").AppendLine();
        }

        return sb.ToString();
    }
}
