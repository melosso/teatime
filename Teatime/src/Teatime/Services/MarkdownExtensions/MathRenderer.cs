using System.Linq;
using Jint;

namespace Teatime.Services.MarkdownExtensions;

public sealed class MathRenderer
{
    private readonly Lock _lock = new();
    private Engine? _engine;

    public string RenderToHtml(string latex, bool displayMode)
    {
        lock (_lock)
        {
            var engine = _engine ??= CreateEngine();
            engine.SetValue("__teatimeMathInput", latex);
            engine.SetValue("__teatimeMathDisplay", displayMode);
            return engine
                .Evaluate("katex.renderToString(__teatimeMathInput, { throwOnError: false, displayMode: __teatimeMathDisplay })")
                .AsString();
        }
    }

    private static Engine CreateEngine()
    {
        var engine = new Engine(options => options.Strict(false));
        engine.Execute(ReadEmbeddedKaTeX());
        return engine;
    }

    private static string ReadEmbeddedKaTeX()
    {
        var assembly = typeof(MathRenderer).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("katex.min.js", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded KaTeX resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
