using System.Text.Json;

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
// What CPM-8.07 added, and what THIS class still does not enforce (CPM-8.08's
// EfGovernedActionExecutor is the boundary layer that does, for the fields it can):
//   - Payload/Preview: the typed, structured version of PayloadSummary a reviewer actually
//     inspects before deciding (see ActionPayload.cs's own comment on why fields stay open text).
//   - RequiredApprovalCapability/RequiredExecutionCapability: which capability governs deciding
//     versus executing this specific proposal ("approval routing" and "capability re-check at
//     execution"). The domain only CARRIES these strings - checking them against an actor's
//     actual claims is a boundary concern (the same way every HTTP endpoint in this codebase
//     calls .RequireAuthorization(Capabilities.X) rather than a domain type comparing role names,
//     .claude/rules/architecture.md's own rule). CPM-8.08's EfGovernedActionExecutor is that
//     boundary for RequiredExecutionCapability; RequiredApprovalCapability is still unchecked -
//     GovernedActionEndpoints.cs only requires Autopilot.Manage on the approve/reject routes
//     today, matching every other Autopilot decision endpoint, because per-proposal approval
//     routing to a DIFFERENT capability than Autopilot.Manage needs CPM-8.11's autonomy-policy
//     administration to decide when that should even apply - the field is real and stored so
//     that task has something to read, not decoration.
//   - IdempotencyKey: required and shape-validated here, and CPM-8.08's migration gives it a
//     real unique index (OrganizationId, IdempotencyKey) - the dedup guarantee this task's own
//     comment once called a future persistence-layer concern.
//   - RequiredConsentType/ConcurrencyToken: optional, because not every action touches a
//     resident's consent or a versioned aggregate. CPM-8.08's executor checks RequiredConsentType
//     against Resident.AllowsContact for the one adapter that sets it (communication drafts).
//     ConcurrencyToken is checked, when an adapter sets it, as the target WorkItem's own version
//     passed to IWorkOperations - it is NOT a general cross-context optimistic-concurrency engine;
//     an adapter that does not carry one (e.g. a purchase-order draft, which does not update an
//     existing versioned row) simply leaves it null.
// This mirrors how TenantEntity carries an OrganizationId without itself enforcing RLS - the
// entity is the contract several enforcement layers point at, not the enforcement.
public sealed class AutopilotActionProposal : TenantEntity
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    public const int ActionTypeMaxLength = 100;
    public const int PayloadSummaryMaxLength = 1000;
    public const int DecisionReasonMaxLength = 1000;
    public const int ExecutionOutcomeMaxLength = 1000;
    public const int CapabilityMaxLength = 100;
    public const int IdempotencyKeyMaxLength = 200;
    public const int ConcurrencyTokenMaxLength = 200;
    public const int ConsentTypeMaxLength = 100;
    public const int PreviewDescriptionMaxLength = 1000;

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
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(preview);
        PayloadFieldsJson = JsonSerializer.Serialize(payload.Fields, SerializerOptions);
        PreviewDescription = AutopilotText.RequireSingleLine(preview.Description, nameof(preview), PreviewDescriptionMaxLength);
        PreviewChangesJson = JsonSerializer.Serialize(preview.Changes, SerializerOptions);
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

    // Stored as JSON (the same choice AutopilotEvidence.Inputs/SourceLinks already made, for the
    // same reason: simpler than an EF value-converted owned collection through a private backing
    // field). Payload/Preview deserialize on read and re-run ActionPayload/ActionPreview's own
    // validation as a side effect - defense in depth, since the JSON only ever came from an
    // already-valid instance.
    public string PayloadFieldsJson { get; private set; } = "[]";
    public string PreviewDescription { get; private set; } = "";
    public string PreviewChangesJson { get; private set; } = "[]";
    public ActionPayload Payload =>
        new(JsonSerializer.Deserialize<List<ActionField>>(PayloadFieldsJson, SerializerOptions) ?? []);
    public ActionPreview Preview =>
        new(PreviewDescription, JsonSerializer.Deserialize<List<ActionFieldChange>>(PreviewChangesJson, SerializerOptions) ?? []);
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
