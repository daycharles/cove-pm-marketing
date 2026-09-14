using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

public enum FindingLifecycleStatus { New, Reviewed, Snoozed, Dismissed, Resolved }

// One detected issue or opportunity from one AutopilotRun. Severity reuses
// PropFlow.Domain.Attention.AttentionSeverity rather than a second three-value enum meaning the
// same thing - Autopilot findings and the existing attention queue are both "something a manager
// should look at, ranked", and CPM-8.05's daily brief is expected to sit next to (not replace)
// /api/attention, so sharing the vocabulary now is cheaper than reconciling two severities later.
//
//   New --Review--> Reviewed --Dismiss--> Dismissed (terminal) <--+
//    |                  |     --Resolve-> Resolved  (terminal) <--+--Reopen-- (from any of the three)
//    |                  |     --Snooze--> Snoozed (not terminal) -+
//    |--Dismiss-------------------------> Dismissed (terminal)
//    |--Resolve-------------------------> Resolved  (terminal)
//    |--Snooze--------------------------> Snoozed (not terminal)
//
// Review is optional, not a gate: a manager can act on or dismiss a finding straight from New,
// and Review exists only to distinguish "seen, still deciding" from "never opened" in the brief.
// Snoozed is deliberately not terminal (IsTerminal stays Dismissed/Resolved only) - a snooze is a
// "come back later", not a decision, and CPM-8.05's brief query is expected to filter a snoozed
// finding out only while SnoozedUntil is still in the future, not remove it from the workable set
// the way Dismissed/Resolved do. Reopen (CPM-8.05) is the ONE sanctioned exit from all three
// "not New/Reviewed" states back to New, mirroring WorkItem.Reopen's "one exit, not one per
// state" discipline (.claude/rules/traps.md) even though it now covers three source states
// instead of one.
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
    //
    // propertyId/portfolioId (CPM-8.05) are denormalized onto the finding at creation time by
    // whichever analyzer/EfSignalCatalog produced it, rather than resolved by a join at query
    // time: AutopilotStore is its own schema/DbContext (tenancy-and-rls.md's "a new module gets
    // its own context" rule), and a LINQ join across two DbContexts is not something EF can do -
    // the same reason Integrations.Conflict.ResolvedByUserId is "a plain Guid with no foreign
    // key" instead of a cross-context navigation. Both are nullable: not every finding is
    // property-scoped (an invoice exception on a vendor-level PayableInvoice has no property to
    // report), and that is a fact about the finding, not a gap to force a value into.
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
        DateTimeOffset freshnessAsOf,
        Guid? propertyId = null,
        Guid? portfolioId = null)
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
        PropertyId = propertyId == Guid.Empty ? null : propertyId;
        PortfolioId = portfolioId == Guid.Empty ? null : portfolioId;
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
    public Guid? PropertyId { get; private set; }
    public Guid? PortfolioId { get; private set; }
    public FindingLifecycleStatus Status { get; private set; } = FindingLifecycleStatus.New;
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? DismissedReason { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public DateTimeOffset? SnoozedUntil { get; private set; }

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

    // CPM-8.05. "Come back later" - hides the finding from an active brief until until has
    // passed, without deciding anything about it. Not gated by Snoozed itself (re-snoozing to
    // push the date further out is allowed), only by the two real decisions.
    public void Snooze(Guid actorId, DateTimeOffset at, DateTimeOffset until)
    {
        RefuseWhenTerminal();
        _ = AutopilotText.RequireId(actorId, nameof(actorId));
        if (until <= at) throw new ArgumentException("Snooze until must be after the current time.", nameof(until));
        Status = FindingLifecycleStatus.Snoozed;
        SnoozedUntil = until.ToUniversalTime();
    }

    // CPM-8.05. The one sanctioned exit from Snoozed/Dismissed/Resolved back to New - see the
    // class comment. Clears every decision-shaped field so a reopened finding reads exactly like
    // one that was never touched, not like a dismissed one wearing a different status.
    public void Reopen(Guid actorId, DateTimeOffset at)
    {
        if (Status is not (FindingLifecycleStatus.Snoozed or FindingLifecycleStatus.Dismissed or FindingLifecycleStatus.Resolved))
            throw new InvalidOperationException("Only a snoozed, dismissed, or resolved finding can be reopened.");
        _ = AutopilotText.RequireId(actorId, nameof(actorId));
        Status = FindingLifecycleStatus.New;
        SnoozedUntil = null;
        DismissedReason = null;
        DecidedAt = null;
        ReviewedBy = null;
        ReviewedAt = null;
    }

    private void RefuseWhenTerminal()
    {
        if (IsTerminal) throw new InvalidOperationException("A dismissed or resolved finding cannot be changed.");
    }
}
