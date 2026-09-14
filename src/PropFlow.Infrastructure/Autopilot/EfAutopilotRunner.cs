using PropFlow.Application.Autopilot;
using PropFlow.Domain.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

public sealed class EfAutopilotRunner(AutopilotStore store, ISignalCatalog catalog, TimeProvider clock) : IAutopilotRunner
{
    public async Task<AutopilotRunResult> RunAsync(string trigger, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var run = new AutopilotRun(store.OrganizationId, Guid.NewGuid(), trigger, now);
        store.Runs.Add(run);
        // Saved before the catalog runs, same reasoning SyncRun.Begin and OutboxProcessor's claim
        // give: a run that crashes mid-sweep leaves a real Running row behind rather than nothing
        // at all, so "did this run happen" is always answerable from the table.
        await store.SaveChangesAsync(cancellationToken);

        IReadOnlyList<AutopilotFindingWithEvidence> results;
        try
        {
            results = await catalog.EvaluateAsync(run.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failedAt = clock.GetUtcNow();
            run.Fail(Truncate(ex.Message, AutopilotRun.FailureReasonMaxLength), failedAt);
            store.AuditEntries.Add(AutopilotAuditEntry.Record(store.OrganizationId, Guid.NewGuid(), "RunFailed",
                "AutopilotRun", run.Id, actorId: null, failedAt, Truncate(ex.Message, AutopilotAuditEntry.DetailMaxLength)));
            await store.SaveChangesAsync(cancellationToken);
            return new AutopilotRunResult(AutopilotRunOutcome.Failed, run.Id, 0, run.FailureReason);
        }

        foreach (var result in results)
        {
            store.Findings.Add(result.Finding);
            store.Evidence.Add(result.Evidence);
        }

        var completedAt = clock.GetUtcNow();
        run.Complete(results.Count, completedAt);
        store.AuditEntries.Add(AutopilotAuditEntry.Record(store.OrganizationId, Guid.NewGuid(), "RunCompleted",
            "AutopilotRun", run.Id, actorId: null, completedAt, $"Completed with {results.Count} finding(s)."));
        await store.SaveChangesAsync(cancellationToken);

        return new AutopilotRunResult(AutopilotRunOutcome.Completed, run.Id, results.Count, null);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
