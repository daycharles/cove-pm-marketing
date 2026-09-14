using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

public enum FindingLifecycleStatus { New, Reviewed, Dismissed, Resolved }

// One detected issue or opportunity from one AutopilotRun. Severity reuses
// PropFlow.Domain.Attention.AttentionSeverity rather than a second three-value enum meaning the
// same thing - Autopilot findings and the existing attention queue are both "something a manager
// should look at, ranked", and CPM-8.05's daily brief is expected to sit next to (not replace)
// /api/attention, so sharing the vocabulary now is cheaper than reconciling two severities later.
//
//   New --Review--> Reviewed --Dismiss--> Dismissed (terminal)
//    |                  |     --Resolve-> Resolved  (terminal)
//    |--Dismiss-------------------------> Dismissed (terminal)
//    |--Resolve-------------------------> Resolved  (terminal)
//
// Review is optional, not a gate: a manager can act on or dismiss a finding straight from New,
// and Review exists only to distinguish "seen, still deciding" from "never opened" in the brief.
public sealed class AutopilotFinding : TenantEntity
{
    public const int SignalTypeMaxLength = 100;
    public const int SubjectTypeMaxLength = 100;
    public const int SummaryMaxLength = 500;
    public const int DismissedReasonMaxLength = 1000;

    // EF materialization.
    private AutopilotFinding(Guid organizationId, Guid id) : base(organizationId, id) { }

    // SignalType and SubjectType are both validated free-text, not closed enums, for the same
    // reason AutopilotRun.Trigger is: CPM-8.02 owns the signal catalog and it does not exist yet,
    // and the set of subject types a finding can point at (a work item today, a lease or a
    // vendor tomorrow) is exactly the kind of open vocabulary TimelineEntry.RelatedObjectType and
    // ApprovalRequest.SubjectType already model this way.
    public AutopilotFinding(
        Guid organizationId,
        Guid id,
        Guid runId,
        string signalType,
        AttentionSeverity severity,
        string subjectType,
        Guid subjectId,
        string summary,
        DateTimeOffset detectedAt,
        DateTimeOffset freshnessAsOf)
        : base(organizationId, id)
    {
        if (!Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        RunId = AutopilotText.RequireId(runId, nameof(runId));
        SignalType = AutopilotText.RequireSingleLine(signalType, nameof(signalType), SignalTypeMaxLength);
        Severity = severity;
        SubjectType = AutopilotText.RequireSingleLine(subjectType, nameof(subjectType), SubjectTypeMaxLength);
        SubjectId = AutopilotText.RequireId(subjectId, nameof(subjectId));
        Summary = AutopilotText.RequireSingleLine(summary, nameof(summary), SummaryMaxLength);
        DetectedAt = detectedAt.ToUniversalTime();
        FreshnessAsOf = freshnessAsOf.ToUniversalTime();
    }

    public Guid RunId { get; private set; }
    public string SignalType { get; private set; } = "";
    public AttentionSeverity Severity { get; private set; }
    public string SubjectType { get; private set; } = "";
    public Guid SubjectId { get; private set; }
    public string Summary { get; private set; } = "";
    public DateTimeOffset DetectedAt { get; private set; }
    // How current the data an analyzer read was when it produced this finding - not when the
    // finding was written. A brief showing a finding whose FreshnessAsOf is hours behind DetectedAt
    // is showing a manager a stale answer to a question that may already have changed underneath
    // it, and CPM-8.05/.06 are expected to surface that gap rather than hide it.
    public DateTimeOffset FreshnessAsOf { get; private set; }
    public FindingLifecycleStatus Status { get; private set; } = FindingLifecycleStatus.New;
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? DismissedReason { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    public bool IsTerminal => Status is FindingLifecycleStatus.Dismissed or FindingLifecycleStatus.Resolved;

    public void Review(Guid actorId, DateTimeOffset at)
    {
        RefuseWhenTerminal();
        if (Status != FindingLifecycleStatus.New)
            throw new InvalidOperationException("Only a new finding can be marked reviewed.");
        ReviewedBy = AutopilotText.RequireId(actorId, nameof(actorId));
        ReviewedAt = at.ToUniversalTime();
        Status = FindingLifecycleStatus.Reviewed;
    }

    public void Dismiss(Guid actorId, DateTimeOffset at, string? reason = null)
    {
        RefuseWhenTerminal();
        _ = AutopilotText.RequireId(actorId, nameof(actorId));
        DismissedReason = AutopilotText.OptionalText(reason, nameof(reason), DismissedReasonMaxLength);
        Status = FindingLifecycleStatus.Dismissed;
        DecidedAt = at.ToUniversalTime();
    }

    public void Resolve(Guid actorId, DateTimeOffset at)
    {
        RefuseWhenTerminal();
        _ = AutopilotText.RequireId(actorId, nameof(actorId));
        Status = FindingLifecycleStatus.Resolved;
        DecidedAt = at.ToUniversalTime();
    }

    private void RefuseWhenTerminal()
    {
        if (IsTerminal) throw new InvalidOperationException("A dismissed or resolved finding cannot be changed.");
    }
}
