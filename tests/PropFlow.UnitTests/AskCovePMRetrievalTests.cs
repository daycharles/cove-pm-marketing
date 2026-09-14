using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AskCovePMRetrievalTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    private static AutopilotFinding Finding(
        string signalType, string subjectType, string summary,
        AttentionSeverity severity = AttentionSeverity.Warning, DateTimeOffset? detectedAt = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), signalType, severity, subjectType, Guid.NewGuid(),
            summary, detectedAt ?? Now, Now);

    [Fact]
    public void A_finding_sharing_a_word_with_the_question_is_returned()
    {
        var pest = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control appointment is overdue.");
        var results = AskCovePMRetrieval.Rank("What is overdue for pest control?", [pest], take: 5);
        Assert.Contains(pest, results);
    }

    [Fact]
    public void A_finding_sharing_nothing_with_the_question_is_excluded()
    {
        var unrelated = Finding(SignalTypes.BudgetVariance, "BudgetLine", "Landscaping budget is over by 12 percent.");
        var results = AskCovePMRetrieval.Rank("What is the vendor's insurance expiration date?", [unrelated], take: 5);
        Assert.Empty(results);
    }

    [Fact]
    public void An_empty_or_stopword_only_question_returns_nothing_rather_than_an_arbitrary_finding()
    {
        var finding = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control appointment is overdue.");
        Assert.Empty(AskCovePMRetrieval.Rank("", [finding], take: 5));
        Assert.Empty(AskCovePMRetrieval.Rank("what is the and a", [finding], take: 5));
    }

    [Fact]
    public void More_overlapping_words_ranks_a_finding_higher()
    {
        var strong = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control appointment overdue at Harbor Point.");
        var weak = Finding(SignalTypes.BudgetVariance, "BudgetLine", "Pest control line item is over budget.");
        var results = AskCovePMRetrieval.Rank("Is the pest control appointment overdue at Harbor Point?", [weak, strong], take: 5);
        Assert.Equal(strong, results[0]);
    }

    [Fact]
    public void Results_are_capped_at_take()
    {
        var findings = Enumerable.Range(0, 10)
            .Select(i => Finding(SignalTypes.SlaRisk, "WorkItem", $"Pest control finding number {i} overdue."))
            .ToList();
        var results = AskCovePMRetrieval.Rank("pest control overdue", findings, take: 3);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void A_tie_breaks_by_severity_then_soonest_detected_matching_the_briefs_own_ordering()
    {
        var older = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control overdue.", AttentionSeverity.Critical, Now.AddDays(-2));
        var newer = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control overdue.", AttentionSeverity.Critical, Now);
        var warning = Finding(SignalTypes.SlaRisk, "WorkItem", "Pest control overdue.", AttentionSeverity.Warning, Now.AddDays(-5));
        var results = AskCovePMRetrieval.Rank("pest control overdue", [newer, warning, older], take: 5);
        Assert.Equal([older, newer, warning], results);
    }
}
