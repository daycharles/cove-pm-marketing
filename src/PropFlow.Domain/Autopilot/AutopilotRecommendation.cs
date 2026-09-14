namespace PropFlow.Domain.Autopilot;

public enum RecommendationStatus { Proposed, Approved, Rejected, Expired }

// A suggested response to one AutopilotFinding, awaiting a manager's decision before it can
// become an AutopilotActionProposal. Deliberately its own aggregate rather than a field on
// AutopilotFinding: one finding can carry more than one candidate recommendation (do nothing,
// reassign, escalate), and the decision trail belongs to whichever one was actually acted on.
//
//   Proposed --Approve--> Approved (terminal)
//            --Reject---> Rejected (terminal)
//            --Expire---> Expired  (terminal)
//
// Approve/Reject enforce the same separation-of-duties rule ApprovalRequest.Decide already does
// (PF-S03.05) - the account that proposed a recommendation can never be the one that decides it.
// This task does not wire Recommendation's decision through the generic ApprovalRequest
// primitive: nothing here persists yet (CPM-8.01 is domain only), and there is no query surface
// to decide from. Whichever later CPM-8 task adds persistence and an endpoint should point that
// decision through ApprovalRequest (SubjectType "AutopilotRecommendation") rather than building a
// second general-purpose approval flow next to the one PF-S03.05 already built - the guard here
// is enforced twice on purpose in the meantime, matching how WorkItem's terminal guard and
// ScreeningRequest's both exist independently rather than one delegating to the other.
public sealed class AutopilotRecommendation : TenantEntity
{
    public const int DescriptionMaxLength = 2000;
    public const int DecisionReasonMaxLength = 1000;

    // EF materialization.
    private AutopilotRecommendation(Guid organizationId, Guid id) : base(organizationId, id) { }

    public AutopilotRecommendation(
        Guid organizationId,
        Guid id,
        Guid findingId,
        string description,
        Guid proposedBy,
        DateTimeOffset proposedAt)
        : base(organizationId, id)
    {
        FindingId = AutopilotText.RequireId(findingId, nameof(findingId));
        Description = AutopilotText.RequireSingleLine(description, nameof(description), DescriptionMaxLength);
        ProposedBy = AutopilotText.RequireId(proposedBy, nameof(proposedBy));
        ProposedAt = proposedAt.ToUniversalTime();
    }

    public Guid FindingId { get; private set; }
    public string Description { get; private set; } = "";
    // The account (or, once CPM-8.04 exists, the model/pipeline identity standing in for one)
    // that proposed this recommendation - never null, so Approve/Reject always has someone to
    // compare the deciding actor against.
    public Guid ProposedBy { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public RecommendationStatus Status { get; private set; } = RecommendationStatus.Proposed;
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionReason { get; private set; }

    public bool IsTerminal => Status != RecommendationStatus.Proposed;

    public void Approve(Guid actorId, DateTimeOffset at, string? reason = null) => Decide(RecommendationStatus.Approved, actorId, at, reason);
    public void Reject(Guid actorId, DateTimeOffset at, string? reason = null) => Decide(RecommendationStatus.Rejected, actorId, at, reason);

    // No actor: a recommendation ages out on its own (CPM-8.05's ordering/staleness rules decide
    // when), so Expire is the one transition that is never attributed to a person.
    public void Expire(DateTimeOffset at)
    {
        RefuseWhenTerminal();
        Status = RecommendationStatus.Expired;
        DecidedAt = at.ToUniversalTime();
    }

    private void Decide(RecommendationStatus outcome, Guid actorId, DateTimeOffset at, string? reason)
    {
        RefuseWhenTerminal();
        if (actorId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorId));
        if (actorId == ProposedBy) throw new InvalidOperationException("The account that proposed a recommendation cannot decide it.");
        Status = outcome;
        DecidedBy = actorId;
        DecidedAt = at.ToUniversalTime();
        DecisionReason = AutopilotText.OptionalText(reason, nameof(reason), DecisionReasonMaxLength);
    }

    private void RefuseWhenTerminal()
    {
        if (IsTerminal) throw new InvalidOperationException("A decided recommendation cannot be changed.");
    }
}
