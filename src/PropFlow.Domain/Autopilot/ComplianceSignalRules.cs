using PropFlow.Domain.Attention;
using PropFlow.Domain.Properties;

namespace PropFlow.Domain.Autopilot;

public static class ComplianceSignalThresholds
{
    public static readonly int UpcomingLeadDays = 14;
}

/// <summary>
/// Takes the real <see cref="ComplianceObligation"/> entity rather than a snapshot record — unlike
/// work items and leases, an obligation already carries everything the rule needs plus the exact
/// overdue/escalated predicates (<c>IsOverdue</c>/<c>IsEscalated</c>) as public methods on itself,
/// so re-deriving that date math into a separate snapshot would just be a second, driftable copy
/// of it. <c>EfSignalCatalog</c> loads real entities for this one, not a projection.
/// </summary>
public static class ComplianceSignalRules
{
    public static SignalCandidate? EvaluateComplianceObligation(ComplianceObligation obligation, DateOnly today, DateTimeOffset now)
    {
        if (obligation.IsEscalated(today))
            return new SignalCandidate(SignalTypes.ComplianceDeadline, AttentionSeverity.Critical, "ComplianceObligation", obligation.Id,
                $"{obligation.Title} is overdue and past its escalation window.", now, now);

        if (obligation.IsOverdue(today))
            return new SignalCandidate(SignalTypes.ComplianceDeadline, AttentionSeverity.Warning, "ComplianceObligation", obligation.Id,
                $"{obligation.Title} is past due.", now, now);

        if (obligation.Status != ComplianceObligationStatus.Active) return null;

        var daysUntilDue = obligation.DueOn.DayNumber - today.DayNumber;
        if (daysUntilDue > ComplianceSignalThresholds.UpcomingLeadDays) return null;

        var summary = daysUntilDue == 0
            ? $"{obligation.Title} is due today."
            : $"{obligation.Title} due in {AutopilotFormatting.Days(daysUntilDue)}.";
        return new SignalCandidate(SignalTypes.ComplianceDeadline, AttentionSeverity.Warning, "ComplianceObligation", obligation.Id, summary, now, now);
    }
}
