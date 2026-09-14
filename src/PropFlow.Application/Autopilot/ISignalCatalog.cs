using PropFlow.Domain.Autopilot;

namespace PropFlow.Application.Autopilot;

/// <summary>
/// Runs every CPM-8.02 analyzer for the current tenant and returns the findings they detect,
/// bound to the given run. One method, no parameters besides the run id — same shape as
/// <see cref="PropFlow.Application.Attention.IAttentionQueue.BuildAsync"/>: a whole-tenant sweep,
/// not a query object. The caller (a future task — CPM-8.02 does not persist findings, matching
/// CPM-8.01's own domain-only scope) supplies <paramref name="runId"/>, which must already exist
/// as an <see cref="AutopilotRun"/>; whether it does is not this interface's concern.
/// </summary>
public interface ISignalCatalog
{
    Task<IReadOnlyList<AutopilotFinding>> EvaluateAsync(Guid runId, CancellationToken cancellationToken);
}
