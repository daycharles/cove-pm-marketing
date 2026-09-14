using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.Properties;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class ComplianceSignalRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 14);
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    private static ComplianceObligation Obligation(DateOnly dueOn, int escalationDays = 14) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Fire extinguisher inspection", dueOn, escalationDays);

    [Fact]
    public void An_obligation_far_from_due_produces_no_signal()
    {
        Assert.Null(ComplianceSignalRules.EvaluateComplianceObligation(Obligation(Today.AddDays(60)), Today, Now));
    }

    [Fact]
    public void An_obligation_within_the_lead_window_is_a_warning()
    {
        var candidate = ComplianceSignalRules.EvaluateComplianceObligation(
            Obligation(Today.AddDays(ComplianceSignalThresholds.UpcomingLeadDays)), Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.ComplianceDeadline, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("ComplianceObligation", candidate.SubjectType);
        Assert.Null(candidate.Evidence.Impact?.EstimatedAmount);
        Assert.NotEmpty(candidate.Evidence.Inputs);
        Assert.Contains(candidate.Evidence.SourceLinks, l => l.EntityType == "ComplianceObligation");
    }

    [Fact]
    public void An_overdue_obligation_still_inside_its_escalation_window_is_a_warning()
    {
        var candidate = ComplianceSignalRules.EvaluateComplianceObligation(
            Obligation(Today.AddDays(-1), escalationDays: 14), Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Contains("past due", candidate.Summary);
    }

    [Fact]
    public void An_obligation_past_its_escalation_window_is_critical()
    {
        var candidate = ComplianceSignalRules.EvaluateComplianceObligation(
            Obligation(Today.AddDays(-20), escalationDays: 14), Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Critical, candidate.Severity);
        Assert.Contains("escalat", candidate.Summary);
    }

    [Fact]
    public void A_satisfied_obligation_produces_no_signal_even_if_its_due_date_is_soon()
    {
        var obligation = Obligation(Today.AddDays(2));
        obligation.Satisfy(Today.AddDays(-1));
        Assert.Null(ComplianceSignalRules.EvaluateComplianceObligation(obligation, Today, Now));
    }

    [Fact]
    public void A_waived_obligation_produces_no_signal_even_if_overdue()
    {
        var obligation = Obligation(Today.AddDays(-30));
        obligation.Waive();
        Assert.Null(ComplianceSignalRules.EvaluateComplianceObligation(obligation, Today, Now));
    }
}
