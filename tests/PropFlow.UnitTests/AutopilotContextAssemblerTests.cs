using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotContextAssemblerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly ImpactEstimate DefaultImpact = new(ImpactCategory.Financial, "Unpaid rent", 500m);

    // impact has no default of its own (Optional<T>-shaped: not-specified vs. explicitly-null
    // must stay distinguishable) — every caller passes it explicitly, using DefaultImpact for
    // "any real impact will do".
    private static (AutopilotFinding Finding, AutopilotEvidence Evidence) FindingWithEvidence(
        ImpactEstimate? impact, string summary = "Payment of $500.00 due in 3 days.")
    {
        var organizationId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var finding = new AutopilotFinding(organizationId, findingId, Guid.NewGuid(), SignalTypes.PaymentDeadline,
            AttentionSeverity.Warning, "LeaseCharge", Guid.NewGuid(), summary, Now, Now);
        var evidence = AutopilotEvidence.Build(organizationId, Guid.NewGuid(), findingId,
            [new CalculationInput("Outstanding", "$500.00"), new CalculationInput("Days until due", "3")],
            [new SourceLink("LeaseCharge", Guid.NewGuid())],
            impact, confidence: 1.0, Now);
        return (finding, evidence);
    }

    [Fact]
    public void The_context_carries_the_findings_own_fields()
    {
        var (finding, evidence) = FindingWithEvidence(DefaultImpact);
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.Equal(SignalTypes.PaymentDeadline, context["signal_type"]);
        Assert.Equal("Warning", context["severity"]);
        Assert.Equal("LeaseCharge", context["subject_type"]);
        Assert.Equal("1.00", context["confidence"]);
    }

    [Fact]
    public void Every_calculation_input_is_carried_under_its_own_key()
    {
        var (finding, evidence) = FindingWithEvidence(DefaultImpact);
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.Equal("$500.00", context["input.Outstanding"]);
        Assert.Equal("3", context["input.Days until due"]);
    }

    [Fact]
    public void A_null_impact_reports_none_category_and_unknown_amount()
    {
        var (finding, evidence) = FindingWithEvidence(impact: null);
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.Equal("none", context["impact_category"]);
        Assert.Equal("", context["impact_description"]);
        Assert.Equal("unknown", context["impact_estimated_amount"]);
    }

    [Fact]
    public void A_null_estimated_amount_on_a_real_impact_still_reports_unknown()
    {
        var (finding, evidence) = FindingWithEvidence(impact: new ImpactEstimate(ImpactCategory.Operational, "Compliance risk", null));
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.Equal("Operational", context["impact_category"]);
        Assert.Equal("unknown", context["impact_estimated_amount"]);
    }

    [Fact]
    public void The_subject_id_and_source_links_never_reach_the_context()
    {
        var (finding, evidence) = FindingWithEvidence(DefaultImpact);
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.DoesNotContain(context.Values, v => v == finding.SubjectId.ToString());
        Assert.DoesNotContain(context.Keys, k => k.Contains("subject_id", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(context.Keys, k => k.Contains("source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Free_text_values_are_redacted_before_they_reach_the_context()
    {
        var (finding, evidence) = FindingWithEvidence(DefaultImpact, summary: "Contact dana@example.test about the payment.");
        var context = AutopilotContextAssembler.Assemble(finding, evidence);

        Assert.DoesNotContain("dana@example.test", context["summary"]);
        Assert.Contains("[redacted-email]", context["summary"]);
    }

    [Fact]
    public void Evidence_for_a_different_finding_is_refused()
    {
        var (finding, _) = FindingWithEvidence(DefaultImpact);
        var (_, otherEvidence) = FindingWithEvidence(DefaultImpact);
        Assert.Throws<ArgumentException>(() => AutopilotContextAssembler.Assemble(finding, otherEvidence));
    }
}
