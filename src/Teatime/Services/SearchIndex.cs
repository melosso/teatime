using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Teatime.Models;

namespace Teatime.Services;

public sealed partial class SearchIndex
{
    // Bounds keep a hostile query from turning fuzzy matching into a CPU sink
    private const int MaxQueryLength = 128;
    private const int MaxQueryTerms = 8;
    private const int MaxFuzzyCandidates = 3;
    private const double FuzzySimilarityThreshold = 0.5;

    private readonly ConcurrentDictionary<string, List<(string Path, int Score)>> _invertedIndex = new();
    private readonly ConcurrentDictionary<string, List<string>> _trigramIndex = new();
    private readonly ConcurrentDictionary<string, DocumentationPage> _pages = new();
    private volatile bool _isBuilt;

    public void Build(IEnumerable<DocumentationPage> pages)
    {
        _invertedIndex.Clear();
        _trigramIndex.Clear();
        _pages.Clear();

        foreach (var page in pages)
        {
            _pages[page.Path] = page;
            IndexPage(page);
        }

        foreach (var term in _invertedIndex.Keys)
        {
            foreach (var trigram in Trigrams(term))
            {
                _trigramIndex.AddOrUpdate(
                    trigram,
                    _ => [term],
                    (_, list) =>
                    {
                        if (!list.Contains(term))
                            list.Add(term);
                        return list;
                    });
            }
        }

        _isBuilt = true;
    }

    private void IndexPage(DocumentationPage page)
    {
        var titleTerms = Tokenize(page.Title);
        var descTerms = page.Description is { Length: > 0 } ? Tokenize(page.Description) : [];
        var headingTerms = page.Headings.SelectMany(h => Tokenize(h.Text));
        var bodyTerms = Tokenize(GetPlainText(page.HtmlContent));
        var keywordTerms = page.Keywords is { Count: > 0 } kw
            ? new HashSet<string>(kw.SelectMany(k => Tokenize(k)), StringComparer.OrdinalIgnoreCase)
            : [];

        var allTerms = new HashSet<string>(titleTerms, StringComparer.OrdinalIgnoreCase);
        allTerms.UnionWith(descTerms);
        allTerms.UnionWith(headingTerms);
        allTerms.UnionWith(bodyTerms);
        allTerms.UnionWith(keywordTerms);

        foreach (var term in allTerms)
        {
            var score = 0;
            if (titleTerms.Contains(term, StringComparer.OrdinalIgnoreCase))
                score += 10;
            if (descTerms.Contains(term, StringComparer.OrdinalIgnoreCase))
                score += 5;
            if (keywordTerms.Contains(term))
                score += 4;
            if (headingTerms.Any(t => string.Equals(t, term, StringComparison.OrdinalIgnoreCase)))
                score += 3;
            if (bodyTerms.Contains(term, StringComparer.OrdinalIgnoreCase))
                score += 1;

            _invertedIndex.AddOrUpdate(
                term.ToLowerInvariant(),
                _ => [(page.Path, score)],
                (_, list) =>
                {
                    var newList = new List<(string Path, int Score)>(list) { (page.Path, score) };
                    return newList;
                });
        }
    }

    // Projects the built index into a serializable form for static export. Docs are ordered
    // by path so output is deterministic; postings reference docs by that index.
    public SearchIndexExport ExportSnapshot()
    {
        var docs = _pages.Values
            .OrderBy(p => p.Path, StringComparer.Ordinal)
            .Select(p => new SearchDocEntry(p.Path, p.Title, p.Description, PlainText(p.HtmlContent)))
            .ToList();

        var docIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < docs.Count; i++)
            docIndex[docs[i].Path] = i;

        var terms = new Dictionary<string, IReadOnlyList<SearchPosting>>(StringComparer.Ordinal);
        foreach (var (term, postings) in _invertedIndex)
        {
            var mapped = new List<SearchPosting>(postings.Count);
            foreach (var (path, score) in postings)
                if (docIndex.TryGetValue(path, out var idx))
                    mapped.Add(new SearchPosting(idx, score));
            terms[term] = mapped;
        }

        var trigrams = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (trigram, termList) in _trigramIndex)
            trigrams[trigram] = termList.ToList();

        return new SearchIndexExport(docs, terms, trigrams);
    }

    public IReadOnlyList<SearchResult> Search(string query)
    {
        if (!_isBuilt || string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        if (query.Length > MaxQueryLength)
            query = query[..MaxQueryLength];

        var terms = Tokenize(query);
        if (terms.Count == 0)
            return Array.Empty<SearchResult>();
        if (terms.Count > MaxQueryTerms)
            terms = terms.Take(MaxQueryTerms).ToList();

        var scores = new Dictionary<string, (int Score, string? Excerpt)>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in terms)
        {
            var key = term.ToLowerInvariant();
            if (_invertedIndex.TryGetValue(key, out var matches))
            {
                AccumulateMatches(scores, matches, term, scoreDivisor: 1);
                continue;
            }

            // No exact hit; fall back to trigram-similar terms at half weight so typos still match but rank below exact hits
            foreach (var candidate in FindFuzzyCandidates(key))
            {
                if (_invertedIndex.TryGetValue(candidate, out var fuzzyMatches))
                    AccumulateMatches(scores, fuzzyMatches, candidate, scoreDivisor: 2);
            }
        }

        return scores
            .OrderByDescending(kv => kv.Value.Score)
            .ThenBy(kv => kv.Key)
            .Select(kv =>
            {
                var page = _pages[kv.Key];
                return new SearchResult(
                    Path: page.Path,
                    Title: page.Title,
                    Description: page.Description,
                    Excerpt: kv.Value.Excerpt);
            })
            .ToList();
    }

    private void AccumulateMatches(
        Dictionary<string, (int Score, string? Excerpt)> scores,
        List<(string Path, int Score)> matches,
        string excerptTerm,
        int scoreDivisor)
    {
        foreach (var (path, termScore) in matches)
        {
            if (!_pages.TryGetValue(path, out var page))
                continue;

            var effectiveScore = Math.Max(1, termScore / scoreDivisor);
            var excerpt = GetExcerpt(page.HtmlContent, excerptTerm)
                ?? (page.Description is { Length: > 0 } ? GetExcerpt(page.Description, excerptTerm) : null);
            if (scores.TryGetValue(path, out var existing))
                scores[path] = (existing.Score + effectiveScore, existing.Excerpt ?? excerpt);
            else
                scores[path] = (effectiveScore, excerpt);
        }
    }

    private List<string> FindFuzzyCandidates(string term)
    {
        var trigrams = Trigrams(term);
        if (trigrams.Count == 0)
            return [];

        var sharedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var trigram in trigrams)
        {
            if (!_trigramIndex.TryGetValue(trigram, out var termsWithTrigram))
                continue;
            foreach (var indexTerm in termsWithTrigram)
                sharedCounts[indexTerm] = sharedCounts.GetValueOrDefault(indexTerm) + 1;
        }

        return sharedCounts
            .Select(kv => (Term: kv.Key, Similarity: DiceCoefficient(kv.Value, trigrams.Count, TrigramCount(kv.Key))))
            .Where(x => x.Similarity >= FuzzySimilarityThreshold)
            .OrderByDescending(x => x.Similarity)
            .ThenBy(x => x.Term, StringComparer.Ordinal)
            .Take(MaxFuzzyCandidates)
            .Select(x => x.Term)
            .ToList();
    }

    private static double DiceCoefficient(int shared, int countA, int countB) =>
        countA + countB == 0 ? 0 : 2.0 * shared / (countA + countB);

    private static int TrigramCount(string term) => Math.Max(term.Length - 2, 0);

    private static List<string> Trigrams(string term)
    {
        if (term.Length < 3)
            return [];

        var trigrams = new List<string>(term.Length - 2);
        for (var i = 0; i <= term.Length - 3; i++)
            trigrams.Add(term.Substring(i, 3));
        return trigrams;
    }

    private static string? GetExcerpt(string html, string term)
    {
        // Decode entities first -- callers HTML-encode for display, double-escaping otherwise.
        var plainText = System.Net.WebUtility.HtmlDecode(HtmlTagRegex().Replace(html, " "));
        var index = plainText.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        var start = Math.Max(0, index - 60);
        var length = Math.Min(plainText.Length - start, 160);
        var excerpt = plainText.AsSpan(start, length).Trim().ToString();
        if (start > 0) excerpt = "..." + excerpt;
        if (start + length < plainText.Length) excerpt += "...";
        return excerpt;
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var words = WordSplitRegex().Split(text);
        var result = new List<string>(words.Length);
        foreach (var word in words)
        {
            var trimmed = word.Trim();
            if (trimmed.Length > 0)
                result.Add(trimmed.ToLowerInvariant());
        }
        return result;
    }

    private static string GetPlainText(string html) =>
        HtmlTagRegex().Replace(html, " ");

    // Strip tags to spaces then decode entities -- same shape GetExcerpt slices, so a client
    // slicing the exported text with identical window math produces the same excerpt.
    internal static string PlainText(string html) =>
        System.Net.WebUtility.HtmlDecode(HtmlTagRegex().Replace(html, " "));

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\W+")]
    private static partial Regex WordSplitRegex();
}
