using PropFlow.Domain.Autopilot;

namespace PropFlow.Application.Autopilot;

public enum AutopilotRunOutcome { Completed, Failed }

public sealed record AutopilotRunResult(AutopilotRunOutcome Outcome, Guid RunId, int FindingCount, string? FailureReason);

// Orchestrates one CPM-8.02/CPM-8.03 sweep and persists it: creates an AutopilotRun, calls
// ISignalCatalog.EvaluateAsync, saves every finding and its evidence, records one
// AutopilotAuditEntry for the run itself, and marks the run Completed or Failed. Outcomes, not
// exceptions - a signal-catalog failure (a bad query, a provider timeout somewhere downstream)
// is recorded on the run and returned as Failed, never thrown past this interface, so a caller
// (a future scheduled trigger, this task's own manual POST endpoint) never has to wrap this in a
// try/catch to keep the API up.
public interface IAutopilotRunner
{
    Task<AutopilotRunResult> RunAsync(string trigger, CancellationToken cancellationToken);
}
