namespace PropFlow.Domain.Autopilot;

public enum ActionProposalStatus { Proposed, Approved, Rejected, Executed, Failed }

// A concrete, single action derived from one approved AutopilotRecommendation, carried through to
// execution. This is deliberately thin: typed action schemas, preview/diff, a capability
// re-check at execution time, idempotency keys, concurrency and consent are CPM-8.07's "governed
// action framework", not this task's. What CPM-8.01 fixes is the shape every later task builds
// on - a proposal exists, is approved or rejected by someone other than its proposer, and is
// executed at most once - so CPM-8.07 extends this lifecycle instead of inventing a second one.
//
//   Proposed --Approve--> Approved --MarkExecuted--> Executed (terminal)
//            --Reject---> Rejected (terminal)                --MarkFailed----> Failed (terminal)
//
// An execution attempt can fail without ever having been Approved having gone through - a
// downstream adapter (CPM-8.08) can reject a payload the domain itself considered fine - so
// MarkFailed is reachable from Approved, not only from a prior failed attempt.
public sealed class AutopilotActionProposal : TenantEntity
{
    public const int ActionTypeMaxLength = 100;
    public const int PayloadSummaryMaxLength = 1000;
    public const int DecisionReasonMaxLength = 1000;
    public const int ExecutionOutcomeMaxLength = 1000;

    // EF materialization.
    private AutopilotActionProposal(Guid organizationId, Guid id) : base(organizationId, id) { }

    // ActionType is validated free-text, not the closed adapter catalog CPM-8.08 will define -
    // that catalog does not exist yet, and this task only needs to know an action has a kind, not
    // which kinds are legal. PayloadSummary is a human-readable description of what the action
    // would do ("Assign vendor Acme Plumbing to WO-00042"), not the typed, executable payload
    // CPM-8.07 introduces - CPM-8.01 has nothing that can execute a payload yet, so storing one
    // here would be a field nothing reads.
    public AutopilotActionProposal(
        Guid organizationId,
        Guid id,
        Guid recommendationId,
        string actionType,
        string payloadSummary,
        Guid proposedBy,
        DateTimeOffset proposedAt)
        : base(organizationId, id)
    {
        RecommendationId = AutopilotText.RequireId(recommendationId, nameof(recommendationId));
        ActionType = AutopilotText.RequireSingleLine(actionType, nameof(actionType), ActionTypeMaxLength);
        PayloadSummary = AutopilotText.RequireSingleLine(payloadSummary, nameof(payloadSummary), PayloadSummaryMaxLength);
        ProposedBy = AutopilotText.RequireId(proposedBy, nameof(proposedBy));
        ProposedAt = proposedAt.ToUniversalTime();
    }

    public Guid RecommendationId { get; private set; }
    public string ActionType { get; private set; } = "";
    public string PayloadSummary { get; private set; } = "";
    public Guid ProposedBy { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public ActionProposalStatus Status { get; private set; } = ActionProposalStatus.Proposed;
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionReason { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public string? ExecutionOutcome { get; private set; }

    public bool IsTerminal => Status is ActionProposalStatus.Rejected or ActionProposalStatus.Executed or ActionProposalStatus.Failed;

    public void Approve(Guid actorId, DateTimeOffset at, string? reason = null) => Decide(ActionProposalStatus.Approved, actorId, at, reason);
    public void Reject(Guid actorId, DateTimeOffset at, string? reason = null) => Decide(ActionProposalStatus.Rejected, actorId, at, reason);

    public void MarkExecuted(string outcome, DateTimeOffset at)
    {
        RequireApproved();
        ExecutionOutcome = AutopilotText.RequireSingleLine(outcome, nameof(outcome), ExecutionOutcomeMaxLength);
        Status = ActionProposalStatus.Executed;
        ExecutedAt = at.ToUniversalTime();
    }

    public void MarkFailed(string reason, DateTimeOffset at)
    {
        RequireApproved();
        ExecutionOutcome = AutopilotText.RequireSingleLine(reason, nameof(reason), ExecutionOutcomeMaxLength);
        Status = ActionProposalStatus.Failed;
        ExecutedAt = at.ToUniversalTime();
    }

    private void Decide(ActionProposalStatus outcome, Guid actorId, DateTimeOffset at, string? reason)
    {
        if (Status != ActionProposalStatus.Proposed)
            throw new InvalidOperationException("Only a proposed action can be approved or rejected.");
        if (actorId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorId));
        if (actorId == ProposedBy) throw new InvalidOperationException("The account that proposed an action cannot decide it.");
        Status = outcome;
        DecidedBy = actorId;
        DecidedAt = at.ToUniversalTime();
        DecisionReason = AutopilotText.OptionalText(reason, nameof(reason), DecisionReasonMaxLength);
    }

    private void RequireApproved()
    {
        if (Status != ActionProposalStatus.Approved)
            throw new InvalidOperationException("Only an approved action can be marked executed or failed.");
    }
}
