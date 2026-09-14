using PropFlow.Domain.Autopilot;

namespace PropFlow.Application.Autopilot;

public enum GovernedActionOutcome { Executed, Failed, CapabilityDenied, ConsentDenied }

// CapabilityDenied/ConsentDenied deliberately do NOT call AutopilotActionProposal.MarkFailed -
// Failed is reserved for a genuine execution attempt the adapter itself rejected (the same
// domain distinction ActionProposalStatus.Failed already draws from Rejected: this is not a
// terminal decision about the proposal, it is "the actor who tried to execute this one wasn't
// allowed to, try again as someone who is" or "the resident hasn't granted this channel". The
// proposal stays Approved either way, so a properly-authorized actor (or the same actor once
// consent changes) can retry without needing to re-propose and re-approve from scratch.
public sealed record GovernedActionResult(GovernedActionOutcome Outcome, string Detail);

/// <summary>
/// The boundary layer <see cref="PropFlow.Domain.Autopilot.AutopilotActionProposal"/>'s own class
/// comment names: re-checks <c>RequiredExecutionCapability</c> against the actor's ACTUAL claims
/// at the moment of execution (not whatever was true when the proposal was approved - that is the
/// entire point of a re-check), checks <c>RequiredConsentType</c> against the real
/// <c>Resident.AllowsContact</c> state when an adapter sets one, then runs the real subsystem
/// mutation for the proposal's <c>ActionType</c> and records the outcome on the proposal itself.
/// </summary>
public interface IGovernedActionExecutor
{
    // Takes the already-loaded, already-Approved proposal (the caller's job — same split
    // AutopilotEndpoints.cs's own Decide() helper already makes between "load and check state"
    // and "apply the transition") rather than an id, so this never has to duplicate a
    // not-found/wrong-status check the endpoint already made.
    Task<GovernedActionResult> ExecuteAsync(
        AutopilotActionProposal proposal,
        Guid actorId,
        IReadOnlyCollection<string> actorCapabilities,
        CancellationToken cancellationToken);
}
