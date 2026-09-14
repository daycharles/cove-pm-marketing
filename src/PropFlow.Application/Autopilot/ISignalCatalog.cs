using PropFlow.Domain.Autopilot;

namespace PropFlow.Application.Autopilot;

/// <summary>
/// One detected finding paired with the evidence that explains it (CPM-8.03) —
/// <see cref="Evidence"/>.FindingId always equals <see cref="Finding"/>.Id; carried as two
/// objects rather than merged into one because <see cref="AutopilotFinding"/>'s own shape (and
/// its eventual persistence) was fixed by CPM-8.01 before evidence existed.
/// </summary>
public sealed record AutopilotFindingWithEvidence(AutopilotFinding Finding, AutopilotEvidence Evidence);

/// <summary>
/// Runs every CPM-8.02/CPM-8.03 analyzer for the current tenant and returns the findings they
/// detect — each with its evidence — bound to the given run. One method, no parameters besides
/// the run id — same shape as
/// <see cref="PropFlow.Application.Attention.IAttentionQueue.BuildAsync"/>: a whole-tenant sweep,
/// not a query object. The caller (a future task — this does not persist anything, matching
/// CPM-8.01's own domain-only scope) supplies <paramref name="runId"/>, which must already exist
/// as an <see cref="AutopilotRun"/>; whether it does is not this interface's concern.
/// </summary>
public interface ISignalCatalog
{
    Task<IReadOnlyList<AutopilotFindingWithEvidence>> EvaluateAsync(Guid runId, CancellationToken cancellationToken);
}
