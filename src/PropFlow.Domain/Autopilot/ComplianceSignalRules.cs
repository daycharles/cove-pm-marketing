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
        AttentionSeverity severity;
        string summary;

        if (obligation.IsEscalated(today))
        {
            severity = AttentionSeverity.Critical;
            summary = $"{obligation.Title} is overdue and past its escalation window.";
        }
        else if (obligation.IsOverdue(today))
        {
            severity = AttentionSeverity.Warning;
            summary = $"{obligation.Title} is past due.";
        }
        else
        {
            if (obligation.Status != ComplianceObligationStatus.Active) return null;
            var daysUntilDue = obligation.DueOn.DayNumber - today.DayNumber;
            if (daysUntilDue > ComplianceSignalThresholds.UpcomingLeadDays) return null;

            severity = AttentionSeverity.Warning;
            summary = daysUntilDue == 0
                ? $"{obligation.Title} is due today."
                : $"{obligation.Title} due in {AutopilotFormatting.Days(daysUntilDue)}.";
        }

        // No EstimatedAmount: a compliance obligation's real cost (a fine, a lost certification,
        // a failed inspection) has no reliable dollar figure to attach without guessing one.
        var evidence = new EvidenceCandidate(
            [
                new CalculationInput("Due on", obligation.DueOn.ToString("yyyy-MM-dd")),
                new CalculationInput("Escalation window (days)", obligation.EscalationDays.ToString()),
            ],
            [new SourceLink("ComplianceObligation", obligation.Id)],
            new ImpactEstimate(ImpactCategory.Operational, "An unmet compliance obligation carries regulatory and inspection risk.", null),
            Confidence: 1.0);

        return new SignalCandidate(SignalTypes.ComplianceDeadline, severity, "ComplianceObligation", obligation.Id, summary, now, now, evidence);
    }
}
