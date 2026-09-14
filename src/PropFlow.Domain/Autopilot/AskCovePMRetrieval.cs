namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Scores and ranks a tenant's findings against one Ask CovePM question — pure word-overlap, no
/// embeddings or external search index (none exists in this codebase, and CPM-8.10 does not add
/// one). Deliberately simple: a finding's <see cref="AutopilotFinding.SignalType"/>,
/// <see cref="AutopilotFinding.SubjectType"/> and <see cref="AutopilotFinding.Summary"/> are the
/// only text this task's retrieval reads — the same fields the daily brief already shows a
/// reviewer, so "what Ask CovePM can find" is never a surprise relative to what the brief already
/// surfaces. A caller (<c>EfAskCovePMService</c>) fetches the tenant's current, workable findings
/// first — this type does not touch persistence.
/// </summary>
public static class AskCovePMRetrieval
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "what", "which", "who", "whom", "how",
        "why", "when", "where", "do", "does", "did", "has", "have", "had", "for", "and", "or",
        "of", "in", "on", "at", "to", "with", "about", "any", "there", "this", "that",
    };

    /// <summary>The top <paramref name="take"/> findings whose text shares at least one token
    /// with the question, most-overlapping first, ties broken by severity then soonest-detected —
    /// the same ordering the brief itself uses, so a tie resolves the same way in both places.
    /// Empty when nothing overlaps at all — the caller reads that as "no matching data" rather
    /// than falling back to an arbitrary or most-recent finding, which would answer a question
    /// about one thing with evidence about another.</summary>
    public static IReadOnlyList<AutopilotFinding> Rank(string question, IReadOnlyList<AutopilotFinding> candidates, int take)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var questionTokens = Tokenize(question);
        if (questionTokens.Count == 0 || candidates.Count == 0) return [];

        return candidates
            .Select(finding => (Finding: finding, Score: Overlap(questionTokens, SearchableText(finding))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Finding.Severity)
            .ThenBy(x => x.Finding.DetectedAt)
            .Take(take)
            .Select(x => x.Finding)
            .ToList();
    }

    private static string SearchableText(AutopilotFinding finding) =>
        $"{finding.SignalType} {finding.SubjectType} {finding.Summary}";

    private static int Overlap(HashSet<string> questionTokens, string text)
    {
        var textTokens = Tokenize(text);
        return questionTokens.Count(textTokens.Contains);
    }

    private static HashSet<string> Tokenize(string text) =>
        text
            .Split([' ', '\t', '\n', '\r', '.', ',', '?', '!', ';', ':', '"', '\'', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length >= 3 && !Stopwords.Contains(token))
            .ToHashSet();
}
