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

/// <summary>
/// A read-only snapshot of one open lease notice. PropertyId is resolved by the caller
/// (Lease.SpaceId → Space.PropertyId — a join this snapshot's own table cannot express) and
/// carried through untouched; null when it could not be resolved.
/// </summary>
public sealed record LeaseNoticeSignalSnapshot(Guid LeaseNoticeId, DateOnly DueOn, Guid? PropertyId = null);

/// <summary>A read-only snapshot of one lease charge still owed. PropertyId, same resolution as above.</summary>
public sealed record LeaseChargeSignalSnapshot(Guid LeaseChargeId, decimal Outstanding, DateOnly DueOn, Guid? PropertyId = null);

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

        var evidence = new EvidenceCandidate(
            [new CalculationInput("Due on", notice.DueOn.ToString("yyyy-MM-dd")), new CalculationInput("Days until due", daysUntilDue.ToString())],
            [new SourceLink("LeaseNotice", notice.LeaseNoticeId)],
            // A lapsed lease notice risks a missed renewal or move-out coordination window, not a
            // clean dollar figure — no EstimatedAmount rather than a guessed one.
            new ImpactEstimate(ImpactCategory.Operational, "A missed lease notice deadline risks a coordination gap for renewal or move-out.", null),
            Confidence: 1.0);

        return new SignalCandidate(SignalTypes.LeaseDeadline, severity, "LeaseNotice", notice.LeaseNoticeId, summary, now, now, evidence, notice.PropertyId);
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

        var evidence = new EvidenceCandidate(
            [new CalculationInput("Outstanding", amount), new CalculationInput("Due on", charge.DueOn.ToString("yyyy-MM-dd")), new CalculationInput("Days until due", daysUntilDue.ToString())],
            [new SourceLink("LeaseCharge", charge.LeaseChargeId)],
            new ImpactEstimate(ImpactCategory.Financial, "Rent or other lease charge not yet collected.", charge.Outstanding),
            Confidence: 1.0);

        return new SignalCandidate(SignalTypes.PaymentDeadline, severity, "LeaseCharge", charge.LeaseChargeId, summary, now, now, evidence, charge.PropertyId);
    }
}
