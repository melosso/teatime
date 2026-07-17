using System.Collections.Concurrent;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Teatime.Services.MarkdownExtensions;

/// <summary>
/// Grammar-based <see cref="ISyntaxHighlighter"/> backed by TextMateSharp, tokenizing against a
/// github-light/dark theme pair. Each line is tokenized once (scopes are theme-independent) and
/// resolved to a light and dark color per token, emitted as <c>--shiki-light</c>/<c>--shiki-dark</c>
/// CSS vars so Teatime's `[data-theme]` toggle drives token color for free.
/// </summary>
public sealed class TextMateSyntaxHighlighter : ISyntaxHighlighter
{
    private const string LightThemeName = "github-light";
    private const string DarkThemeName = "github-dark";

    // Prevents the leading "$"/">" shell prompt symbol from being selected/copied with the command.
    private static readonly HashSet<string> ShellLangs = new(StringComparer.OrdinalIgnoreCase)
    {
        "shellscript", "shell", "bash", "sh", "zsh", "ksh", "csh", "console"
    };

    private readonly RegistryOptions _registryOptions = new(ThemeName.Light);
    private readonly Registry _registry;
    private readonly ConcurrentDictionary<string, IGrammar?> _grammarsByLang = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<Dictionary<string, string>> _languageIdsByAlias;

    private Theme? _lightTheme;
    private Theme? _darkTheme;

    public TextMateSyntaxHighlighter()
    {
        _registry = new Registry(_registryOptions);
        _languageIdsByAlias = new Lazy<Dictionary<string, string>>(BuildLanguageAliasMap);
    }

    public SyntaxTheme? Theme { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        var rawLight = ReadEmbeddedTheme($"{LightThemeName}.json");
        var rawDark = ReadEmbeddedTheme($"{DarkThemeName}.json");

        _lightTheme = TextMateSharp.Themes.Theme.CreateFromRawTheme(rawLight, _registryOptions);
        _darkTheme = TextMateSharp.Themes.Theme.CreateFromRawTheme(rawDark, _registryOptions);

        Theme = new SyntaxTheme(
            LightName: LightThemeName,
            DarkName: DarkThemeName,
            LightForeground: GetGuiColor(rawLight, "editor.foreground") ?? "#24292e",
            DarkForeground: GetGuiColor(rawDark, "editor.foreground") ?? "#e1e4e8",
            LightBackground: GetGuiColor(rawLight, "editor.background") ?? "#ffffff",
            DarkBackground: GetGuiColor(rawDark, "editor.background") ?? "#24292e");

        return Task.CompletedTask;
    }

    public IReadOnlyList<IReadOnlyList<SyntaxToken>> TokenizeLines(IReadOnlyList<string> lines, string lang)
    {
        var grammar = GetGrammar(lang);
        if (grammar is null || _lightTheme is null || _darkTheme is null)
            return PlainFallback(lines);

        var result = new List<IReadOnlyList<SyntaxToken>>(lines.Count);
        TextMateSharp.Grammars.IStateStack? ruleStack = null;
        var isShell = ShellLangs.Contains(lang);

        foreach (var line in lines)
        {
            var tokenized = grammar.TokenizeLine(line, ruleStack!, TimeSpan.MaxValue);
            ruleStack = tokenized.RuleStack;

            var lineTokens = new List<SyntaxToken>(tokenized.Tokens.Length);
            foreach (var token in tokenized.Tokens)
            {
                var start = Math.Min(token.StartIndex, line.Length);
                var end = Math.Min(token.EndIndex, line.Length);
                if (end <= start)
                    continue;

                var text = line[start..end];
                var lightColor = ResolveColor(_lightTheme, token.Scopes, Theme!.Value.LightForeground);
                var darkColor = ResolveColor(_darkTheme, token.Scopes, Theme!.Value.DarkForeground);
                lineTokens.Add(new SyntaxToken(text, lightColor, darkColor));
            }

            if (isShell)
                ApplyShellPromptStyling(lineTokens);

            result.Add(lineTokens.Count > 0 ? lineTokens : [new SyntaxToken(line, null, null)]);
        }

        return result;
    }

    private static void ApplyShellPromptStyling(List<SyntaxToken> tokens)
    {
        if (tokens.Count < 2)
            return;

        var first = tokens[0].Text.Trim();
        if (first != "$" && first != ">")
            return;
        if (tokens[1].Text.Length == 0 || tokens[1].Text[0] != ' ')
            return;

        tokens[0] = tokens[0] with { Text = first + " " };
        tokens[1] = tokens[1] with { Text = tokens[1].Text[1..] };
    }

    private static string ResolveColor(Theme theme, List<string> scopes, string defaultColor)
    {
        var rules = theme.Match(scopes);
        if (rules.Count == 0 || rules[0].foreground <= 0)
            return defaultColor;

        return theme.GetColor(rules[0].foreground) ?? defaultColor;
    }

    private IGrammar? GetGrammar(string lang)
    {
        return _grammarsByLang.GetOrAdd(lang, static (key, self) =>
        {
            var languageId = self.ResolveLanguageId(key);
            if (languageId is null)
                return null;

            var scopeName = self._registryOptions.GetScopeByLanguageId(languageId);
            return scopeName is null ? null : self._registry.LoadGrammar(scopeName);
        }, this);
    }

    private string? ResolveLanguageId(string lang)
    {
        if (_languageIdsByAlias.Value.TryGetValue(lang, out var id))
            return id;

        var byExtension = _registryOptions.GetLanguageByExtension($".{lang}");
        return byExtension?.Id;
    }

    private Dictionary<string, string> BuildLanguageAliasMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in _registryOptions.GetAvailableLanguages())
        {
            map[language.Id] = language.Id;
            foreach (var alias in language.Aliases ?? [])
                map[alias] = language.Id;
        }
        return map;
    }

    private static IReadOnlyList<IReadOnlyList<SyntaxToken>> PlainFallback(IReadOnlyList<string> lines) =>
        lines.Select(line => (IReadOnlyList<SyntaxToken>)[new SyntaxToken(line, null, null)]).ToList();

    private static IRawTheme ReadEmbeddedTheme(string fileName)
    {
        var assembly = typeof(TextMateSyntaxHighlighter).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded theme resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return ThemeReader.ReadThemeSync(reader);
    }

    private static string? GetGuiColor(IRawTheme theme, string key)
    {
        foreach (var pair in theme.GetGuiColors())
        {
            if (pair.Key == key)
                return pair.Value as string;
        }
        return null;
    }
}
