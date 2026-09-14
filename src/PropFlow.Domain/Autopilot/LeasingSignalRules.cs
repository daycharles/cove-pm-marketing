using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Fixed lead-time thresholds for the leasing signals. Not yet per-organization configurable —
/// same open question as <see cref="Attention.AttentionThresholds"/>.
/// </summary>
public static class LeasingSignalThresholds
{
    // A lease notice or an upcoming payment starts showing up in the daily brief this many days
    // before it's due, not only once it's overdue — Autopilot is meant to give a manager lead
    // time, not just report what already slipped.
    public static readonly int LeaseDeadlineLeadDays = 14;
    public static readonly int PaymentDeadlineLeadDays = 5;
    // A payment more than this many days overdue escalates from Warning to Critical.
    public static readonly int PaymentSeriouslyOverdueDays = 14;
}

/// <summary>A read-only snapshot of one open lease notice — the two facts the rule needs.</summary>
public sealed record LeaseNoticeSignalSnapshot(Guid LeaseNoticeId, DateOnly DueOn);

/// <summary>A read-only snapshot of one lease charge still owed.</summary>
public sealed record LeaseChargeSignalSnapshot(Guid LeaseChargeId, decimal Outstanding, DateOnly DueOn);

/// <summary>
/// Pure evaluation of the two leasing signals. Each snapshot yields at most one candidate — the
/// caller (EfSignalCatalog) is responsible for only handing in notices/charges that are still
/// open (Status filtering is a query concern, not a rule concern).
/// </summary>
public static class LeasingSignalRules
{
    public static SignalCandidate? EvaluateLeaseNotice(LeaseNoticeSignalSnapshot notice, DateOnly today, DateTimeOffset now)
    {
        var daysUntilDue = notice.DueOn.DayNumber - today.DayNumber;
        if (daysUntilDue > LeasingSignalThresholds.LeaseDeadlineLeadDays) return null;

        var severity = daysUntilDue < 0 ? AttentionSeverity.Critical : AttentionSeverity.Warning;
        var summary = daysUntilDue switch
        {
            < 0 => $"Lease notice is {AutopilotFormatting.Days(-daysUntilDue)} overdue.",
            0 => "Lease notice is due today.",
            _ => $"Lease notice due in {AutopilotFormatting.Days(daysUntilDue)}.",
        };

        return new SignalCandidate(SignalTypes.LeaseDeadline, severity, "LeaseNotice", notice.LeaseNoticeId, summary, now, now);
    }

    public static SignalCandidate? EvaluateLeaseCharge(LeaseChargeSignalSnapshot charge, DateOnly today, DateTimeOffset now)
    {
        if (charge.Outstanding <= 0) return null;
        var daysUntilDue = charge.DueOn.DayNumber - today.DayNumber;
        if (daysUntilDue > LeasingSignalThresholds.PaymentDeadlineLeadDays) return null;

        var severity = daysUntilDue < -LeasingSignalThresholds.PaymentSeriouslyOverdueDays
            ? AttentionSeverity.Critical
            : AttentionSeverity.Warning;
        var amount = AutopilotFormatting.Money(charge.Outstanding);
        var summary = daysUntilDue switch
        {
            < 0 => $"Payment of {amount} is {AutopilotFormatting.Days(-daysUntilDue)} overdue.",
            0 => $"Payment of {amount} is due today.",
            _ => $"Payment of {amount} due in {AutopilotFormatting.Days(daysUntilDue)}.",
        };

        return new SignalCandidate(SignalTypes.PaymentDeadline, severity, "LeaseCharge", charge.LeaseChargeId, summary, now, now);
    }
}
