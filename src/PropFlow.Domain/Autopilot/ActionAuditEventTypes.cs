namespace PropFlow.Domain.Autopilot;

// CPM-8.07. Named event types for AutopilotAuditEntry rows describing an AutopilotActionProposal
// transition - a shared vocabulary so whichever infra task first writes these (CPM-8.08's
// adapters, or a persistence follow-up to this one) doesn't invent its own ad hoc strings, the
// same reason SignalTypes.cs already centralizes signal-type constants. No persistence writes
// these yet - this task only fixes the vocabulary a future writer must use, the same "machinery
// before the thing that uses it" shape CPM-8.04's gateway abstraction shipped in.
public static class ActionAuditEventTypes
{
    public const string Proposed = "ActionProposed";
    public const string Approved = "ActionApproved";
    public const string Rejected = "ActionRejected";
    public const string Executed = "ActionExecuted";
    public const string Failed = "ActionFailed";

    public static readonly IReadOnlyList<string> All = [Proposed, Approved, Rejected, Executed, Failed];
}
