namespace PropFlow.Domain.Autopilot;

public enum ActionProposalStatus { Proposed, Approved, Rejected, Executed, Failed }

// A concrete, single action derived from one approved AutopilotRecommendation, carried through to
// execution. CPM-8.01 shipped this thin on purpose - "typed action schemas, preview/diff, a
// capability re-check at execution time, idempotency keys, concurrency and consent are CPM-8.07's
// 'governed action framework', not this task's" (its own words) - and CPM-8.07 (this task)
// extends the SAME lifecycle rather than inventing a second one, exactly as that comment asked.
//
//   Proposed --Approve--> Approved --MarkExecuted--> Executed (terminal)
//            --Reject---> Rejected (terminal)                --MarkFailed----> Failed (terminal)
//
// An execution attempt can fail without ever having been Approved having gone through - a
// downstream adapter (CPM-8.08) can reject a payload the domain itself considered fine - so
// MarkFailed is reachable from Approved, not only from a prior failed attempt.
//
// What CPM-8.07 adds, and what it deliberately does NOT enforce here:
//   - Payload/Preview: the typed, structured version of PayloadSummary a reviewer actually
//     inspects before deciding (see ActionPayload.cs's own comment on why fields stay open text).
//   - RequiredApprovalCapability/RequiredExecutionCapability: which capability governs deciding
//     versus executing this specific proposal ("approval routing" and "capability re-check at
//     execution"). Domain only CARRIES these strings - checking them against an actor's actual
//     claims is a boundary concern (the same way every HTTP endpoint in this codebase calls
//     .RequireAuthorization(Capabilities.X) rather than a domain type comparing role names,
//     .claude/rules/architecture.md's own rule), and there is no endpoint yet to do that
//     checking. A future infra task authorizes against these by name via
//     IAuthorizationService.AuthorizeAsync(user, proposal.RequiredExecutionCapability) or
//     equivalent, once one exists.
//   - IdempotencyKey: required and shape-validated here; the actual dedup guarantee (a unique
//     index) is a persistence-layer concern this task does not add, the same way CPM-8.01 left
//     no EF mapping at all - there is nothing to index yet.
//   - RequiredConsentType/ConcurrencyToken: optional, because not every action touches a
//     resident's consent or a versioned aggregate. Checking either against the real
//     ApplicationConsent/EF xmin state is, again, infra's job once an executor exists.
// This mirrors how TenantEntity carries an OrganizationId without itself enforcing RLS - the
// entity is the contract several enforcement layers point at, not the enforcement.
public sealed class AutopilotActionProposal : TenantEntity
{
    public const int ActionTypeMaxLength = 100;
    public const int PayloadSummaryMaxLength = 1000;
    public const int DecisionReasonMaxLength = 1000;
    public const int ExecutionOutcomeMaxLength = 1000;
    public const int CapabilityMaxLength = 100;
    public const int IdempotencyKeyMaxLength = 200;
    public const int ConcurrencyTokenMaxLength = 200;
    public const int ConsentTypeMaxLength = 100;

    // EF materialization.
    private AutopilotActionProposal(Guid organizationId, Guid id) : base(organizationId, id) { }

    // ActionType is validated free-text, not the closed adapter catalog CPM-8.08 will define -
    // that catalog does not exist yet, and this task only needs to know an action has a kind, not
    // which kinds are legal. PayloadSummary stays a human-readable one-liner for a list row
    // ("Assign vendor Acme Plumbing to WO-00042"); Payload/Preview below are the structured
    // version a reviewer inspects before deciding.
    public AutopilotActionProposal(
        Guid organizationId,
        Guid id,
        Guid recommendationId,
        string actionType,
        string payloadSummary,
        Guid proposedBy,
        DateTimeOffset proposedAt,
        ActionPayload payload,
        ActionPreview preview,
        string requiredApprovalCapability,
        string requiredExecutionCapability,
        string idempotencyKey,
        string? requiredConsentType = null,
        string? concurrencyToken = null)
        : base(organizationId, id)
    {
        RecommendationId = AutopilotText.RequireId(recommendationId, nameof(recommendationId));
        ActionType = AutopilotText.RequireSingleLine(actionType, nameof(actionType), ActionTypeMaxLength);
        PayloadSummary = AutopilotText.RequireSingleLine(payloadSummary, nameof(payloadSummary), PayloadSummaryMaxLength);
        ProposedBy = AutopilotText.RequireId(proposedBy, nameof(proposedBy));
        ProposedAt = proposedAt.ToUniversalTime();
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Preview = preview ?? throw new ArgumentNullException(nameof(preview));
        RequiredApprovalCapability = AutopilotText.RequireSingleLine(requiredApprovalCapability, nameof(requiredApprovalCapability), CapabilityMaxLength);
        RequiredExecutionCapability = AutopilotText.RequireSingleLine(requiredExecutionCapability, nameof(requiredExecutionCapability), CapabilityMaxLength);
        IdempotencyKey = AutopilotText.RequireSingleLine(idempotencyKey, nameof(idempotencyKey), IdempotencyKeyMaxLength);
        RequiredConsentType = AutopilotText.OptionalText(requiredConsentType, nameof(requiredConsentType), ConsentTypeMaxLength);
        ConcurrencyToken = AutopilotText.OptionalText(concurrencyToken, nameof(concurrencyToken), ConcurrencyTokenMaxLength);
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

    public ActionPayload Payload { get; private set; } = null!;
    public ActionPreview Preview { get; private set; } = null!;
    public string RequiredApprovalCapability { get; private set; } = "";
    public string RequiredExecutionCapability { get; private set; } = "";
    public string IdempotencyKey { get; private set; } = "";
    public string? RequiredConsentType { get; private set; }
    public string? ConcurrencyToken { get; private set; }

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
