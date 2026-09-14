using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class LeasingSignalRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 14);
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_lease_notice_far_in_the_future_produces_no_signal()
    {
        var notice = new LeaseNoticeSignalSnapshot(Guid.NewGuid(), Today.AddDays(30));
        Assert.Null(LeasingSignalRules.EvaluateLeaseNotice(notice, Today, Now));
    }

    [Fact]
    public void A_lease_notice_due_within_the_lead_window_is_a_warning()
    {
        var notice = new LeaseNoticeSignalSnapshot(Guid.NewGuid(), Today.AddDays(LeasingSignalThresholds.LeaseDeadlineLeadDays));
        var candidate = LeasingSignalRules.EvaluateLeaseNotice(notice, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.LeaseDeadline, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("LeaseNotice", candidate.SubjectType);
        Assert.Equal(notice.LeaseNoticeId, candidate.SubjectId);
    }

    [Fact]
    public void A_lease_notice_past_due_is_critical()
    {
        var notice = new LeaseNoticeSignalSnapshot(Guid.NewGuid(), Today.AddDays(-1));
        var candidate = LeasingSignalRules.EvaluateLeaseNotice(notice, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Critical, candidate.Severity);
        Assert.Contains("overdue", candidate.Summary);
    }

    [Fact]
    public void A_lease_notice_due_today_says_so()
    {
        var notice = new LeaseNoticeSignalSnapshot(Guid.NewGuid(), Today);
        var candidate = LeasingSignalRules.EvaluateLeaseNotice(notice, Today, Now);
        Assert.NotNull(candidate);
        Assert.Contains("due today", candidate.Summary);
    }

    [Fact]
    public void A_fully_paid_lease_charge_produces_no_signal_even_if_overdue()
    {
        var charge = new LeaseChargeSignalSnapshot(Guid.NewGuid(), Outstanding: 0m, Today.AddDays(-30));
        Assert.Null(LeasingSignalRules.EvaluateLeaseCharge(charge, Today, Now));
    }

    [Fact]
    public void A_lease_charge_far_from_due_produces_no_signal()
    {
        var charge = new LeaseChargeSignalSnapshot(Guid.NewGuid(), Outstanding: 1200m, Today.AddDays(30));
        Assert.Null(LeasingSignalRules.EvaluateLeaseCharge(charge, Today, Now));
    }

    [Fact]
    public void A_lease_charge_within_the_lead_window_is_a_warning()
    {
        var charge = new LeaseChargeSignalSnapshot(Guid.NewGuid(), Outstanding: 1200m, Today.AddDays(LeasingSignalThresholds.PaymentDeadlineLeadDays));
        var candidate = LeasingSignalRules.EvaluateLeaseCharge(charge, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.PaymentDeadline, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("LeaseCharge", candidate.SubjectType);
        Assert.Contains("$1,200.00", candidate.Summary);
    }

    [Fact]
    public void A_lease_charge_slightly_overdue_is_a_warning()
    {
        var charge = new LeaseChargeSignalSnapshot(Guid.NewGuid(), Outstanding: 500m, Today.AddDays(-3));
        var candidate = LeasingSignalRules.EvaluateLeaseCharge(charge, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
    }

    [Fact]
    public void A_lease_charge_seriously_overdue_is_critical()
    {
        var charge = new LeaseChargeSignalSnapshot(Guid.NewGuid(), Outstanding: 500m,
            Today.AddDays(-(LeasingSignalThresholds.PaymentSeriouslyOverdueDays + 1)));
        var candidate = LeasingSignalRules.EvaluateLeaseCharge(charge, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Critical, candidate.Severity);
    }
}
