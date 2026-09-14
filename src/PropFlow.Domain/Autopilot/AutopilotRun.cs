namespace PropFlow.Domain.Autopilot;

public enum AutopilotRunStatus { Running, Completed, Failed }

// One execution of the cross-suite analysis pipeline (CPM-8.02's analyzers) for one
// organization. A run is the unit findings attach to, so a finding can always answer "which pass
// produced this, and was the underlying data fresh at that time" without re-deriving it - that
// second question is FreshnessAsOf on AutopilotFinding, not here, because different analyzers in
// the same run can read different tables at different moments.
//
//   Running --Complete--> Completed (terminal)
//           --Fail------> Failed    (terminal)
//
// No Cancel and no Reopen: a stuck run is a Fail an operator or a watchdog records, and a
// re-analysis is a new run, not a resumed one - CPM-8.02's analyzers are meant to be safe to
// re-run from scratch, so there is nothing a partial run leaves that a fresh one cannot redo.
public sealed class AutopilotRun : TenantEntity
{
    public const int TriggerMaxLength = 100;
    public const int FailureReasonMaxLength = 1000;

    // EF materialization.
    private AutopilotRun(Guid organizationId, Guid id) : base(organizationId, id) { }

    // Trigger is a validated free-text label ("Scheduled", "Manual", "Backfill", ...) rather
    // than a closed enum: CPM-8.02 owns the actual analyzer/trigger catalog and it does not
    // exist yet, so a closed vocabulary here would just be guessed and renamed later. Matches
    // TimelineEntry.RelatedObjectType's and ApprovalRequest.SubjectType's precedent for exactly
    // this situation - see .claude/rules/ (traps.md has no entry for this one yet, but the
    // pattern is established across FS-S03/FS-S05).
    public AutopilotRun(Guid organizationId, Guid id, string trigger, DateTimeOffset startedAt)
        : base(organizationId, id)
    {
        Trigger = AutopilotText.RequireSingleLine(trigger, nameof(trigger), TriggerMaxLength);
        StartedAt = startedAt.ToUniversalTime();
    }

    public string Trigger { get; private set; } = "";
    public AutopilotRunStatus Status { get; private set; } = AutopilotRunStatus.Running;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    // Set by Complete, not incremented finding-by-finding: the run does not hold a live
    // collection of findings (CPM-8.02/.03 own how findings are persisted and queried), so this
    // is the pipeline reporting its own tally once, the same way ScreeningRequest.Attempts is
    // owned by the caller rather than derived from a child collection.
    public int FindingCount { get; private set; }
    public string? FailureReason { get; private set; }

    public bool IsTerminal => Status is AutopilotRunStatus.Completed or AutopilotRunStatus.Failed;

    public void Complete(int findingCount, DateTimeOffset now)
    {
        RefuseWhenTerminal();
        if (findingCount < 0) throw new ArgumentOutOfRangeException(nameof(findingCount));
        Status = AutopilotRunStatus.Completed;
        FindingCount = findingCount;
        CompletedAt = now.ToUniversalTime();
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        RefuseWhenTerminal();
        FailureReason = AutopilotText.RequireSingleLine(reason, nameof(reason), FailureReasonMaxLength);
        Status = AutopilotRunStatus.Failed;
        CompletedAt = now.ToUniversalTime();
    }

    private void RefuseWhenTerminal()
    {
        if (IsTerminal) throw new InvalidOperationException("A completed or failed run cannot be changed.");
    }
}
