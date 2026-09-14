using Microsoft.EntityFrameworkCore;
using PropFlow.Application.Assets;
using PropFlow.Application.Attention;
using PropFlow.Application.Autopilot;
using PropFlow.Domain.Accounting;
using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.Leasing;
using PropFlow.Domain.Properties;

namespace PropFlow.Infrastructure.Persistence;

/// <summary>
/// Assembles the CPM-8.02 signal catalog: reuses the existing Attention queue for the four
/// work-item-shaped signals and <see cref="IRepeatRepairDetector"/> for the asset-shaped repeat-
/// repair sweep (both "share vocabulary, don't duplicate" — see WorkSignalRules.cs), then runs
/// the four genuinely new analyzers (leasing, accounting ×2, compliance, assets) added in this
/// task. Every query is tenant-scoped by <see cref="OperationsStore"/>'s EF filter and RLS, same
/// as <see cref="EfAttentionQueue"/>. Purely additive reads — nothing here writes, and
/// AutopilotFinding has no EF mapping yet (CPM-8.01 is domain-only), so every finding returned is
/// an in-memory object the caller is responsible for persisting, once something does.
/// </summary>
public sealed class EfSignalCatalog(
    OperationsStore store,
    IAttentionQueue attentionQueue,
    IRepeatRepairDetector repeatRepair,
    TimeProvider clock) : ISignalCatalog
{
    public async Task<IReadOnlyList<AutopilotFindingWithEvidence>> EvaluateAsync(Guid runId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var organizationId = store.OrganizationId;
        var candidates = new List<SignalCandidate>();

        candidates.AddRange(await EvaluateWorkSignalsAsync(cancellationToken));
        candidates.AddRange(await EvaluateRepeatRepairAsync(now, cancellationToken));
        candidates.AddRange(await EvaluateLeaseNoticesAsync(today, now, cancellationToken));
        candidates.AddRange(await EvaluateLeaseChargesAsync(today, now, cancellationToken));
        candidates.AddRange(await EvaluateBudgetVarianceAsync(today, now, cancellationToken));
        candidates.AddRange(await EvaluateInvoiceExceptionsAsync(today, now, cancellationToken));
        candidates.AddRange(await EvaluateComplianceObligationsAsync(today, now, cancellationToken));
        candidates.AddRange(await EvaluateAssetReplacementAsync(today, now, cancellationToken));

        return candidates
            .Select(c =>
            {
                var finding = new AutopilotFinding(organizationId, Guid.NewGuid(), runId, c.SignalType, c.Severity,
                    c.SubjectType, c.SubjectId, c.Summary, c.DetectedAt, c.FreshnessAsOf);
                var evidence = AutopilotEvidence.Build(organizationId, Guid.NewGuid(), finding.Id,
                    c.Evidence.Inputs, c.Evidence.SourceLinks, c.Evidence.Impact, c.Evidence.Confidence, c.FreshnessAsOf);
                return new AutopilotFindingWithEvidence(finding, evidence);
            })
            .ToList();
    }

    // Reuses the existing Attention queue rather than re-querying WorkItems/Timeline a second
    // time with a second copy of the same rules — see WorkSignalRules.cs. Evidence here is
    // necessarily thinner than the new analyzers': AttentionItem/AttentionFinding weren't
    // designed to carry calculation inputs, so this reports what the row itself already has
    // rather than re-querying WorkItems for fields the queue chose not to expose.
    private async Task<IReadOnlyList<SignalCandidate>> EvaluateWorkSignalsAsync(CancellationToken cancellationToken)
    {
        var queue = await attentionQueue.BuildAsync(cancellationToken);
        var candidates = new List<SignalCandidate>();
        var now = DateTimeOffset.UtcNow;
        foreach (var item in queue.Items)
        {
            foreach (var finding in item.Findings)
            {
                var signalType = WorkSignalRules.MapSignalType(finding.Reason);
                if (signalType is null) continue;

                var evidence = new EvidenceCandidate(
                    [
                        new CalculationInput("Status", item.Status.ToString()),
                        new CalculationInput("Priority", item.Priority.ToString()),
                        new CalculationInput("Due date", item.DueDate?.ToString("yyyy-MM-dd") ?? "not set"),
                    ],
                    [new SourceLink("WorkItem", item.WorkId)],
                    new ImpactEstimate(ImpactCategory.Operational, finding.Detail, null),
                    Confidence: 1.0);

                candidates.Add(new SignalCandidate(signalType, finding.Severity, "WorkItem", item.WorkId,
                    finding.Detail, now, now, evidence));
            }
        }
        return candidates;
    }

    // Tenant-wide, not only assets with currently-open work — see SignalTypes.cs's own comment
    // on why this is deliberately broader than Attention's RepeatRepair reason. Calls AssessAsync
    // per flagged asset (not just the id sweep) so the evidence carries the actual repair count
    // and cost rather than a generic "crossed the threshold" claim with nothing behind it — the
    // asset list here is normally small (crossing the threshold is the exception, not the norm),
    // so the N+1 is not a real cost.
    private async Task<IReadOnlyList<SignalCandidate>> EvaluateRepeatRepairAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var assetIds = await repeatRepair.RepeatRepairAssetIdsAsync(cancellationToken);
        var candidates = new List<SignalCandidate>();
        foreach (var assetId in assetIds)
        {
            var assessment = await repeatRepair.AssessAsync(assetId, categoryId: null, cancellationToken);
            if (assessment is null) continue; // asset vanished between the sweep and the assessment

            var evidence = new EvidenceCandidate(
                [
                    new CalculationInput("Repair count", assessment.RepairCount.ToString()),
                    new CalculationInput("Threshold", assessment.RepairThreshold.ToString()),
                    new CalculationInput("Window (days)", assessment.WindowDays.ToString()),
                    new CalculationInput("Cost in window", AutopilotFormatting.Money(assessment.TotalCostInWindow)),
                ],
                [new SourceLink("Asset", assetId)],
                new ImpactEstimate(ImpactCategory.Financial, "Repair spend on this asset within the detection window.", assessment.TotalCostInWindow),
                Confidence: 1.0);

            candidates.Add(new SignalCandidate(SignalTypes.RepeatRepair, AttentionSeverity.Warning, "Asset", assetId,
                $"Asset has {assessment.RepairCount} repairs in the last {assessment.WindowDays} days, crossing the threshold of {assessment.RepairThreshold}.",
                now, now, evidence));
        }
        return candidates;
    }

    private async Task<IReadOnlyList<SignalCandidate>> EvaluateLeaseNoticesAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var notices = await store.LeaseNotices.AsNoTracking()
            .Where(n => n.Status == LeaseNoticeStatus.Open)
            .Select(n => new { n.Id, n.DueOn })
            .ToListAsync(cancellationToken);

        return notices
            .Select(n => LeasingSignalRules.EvaluateLeaseNotice(new LeaseNoticeSignalSnapshot(n.Id, n.DueOn), today, now))
            .OfType<SignalCandidate>()
            .ToList();
    }

    private async Task<IReadOnlyList<SignalCandidate>> EvaluateLeaseChargesAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var charges = await store.LeaseCharges.AsNoTracking()
            .Where(c => c.Status == LeaseChargeStatus.Open || c.Status == LeaseChargeStatus.PartiallyPaid)
            .Select(c => new { c.Id, c.Amount, c.AmountApplied, c.DueOn })
            .ToListAsync(cancellationToken);

        return charges
            .Select(c => LeasingSignalRules.EvaluateLeaseCharge(
                new LeaseChargeSignalSnapshot(c.Id, c.Amount - c.AmountApplied, c.DueOn), today, now))
            .OfType<SignalCandidate>()
            .ToList();
    }

    // Budgeted vs actual per (account, month) for every line of every Approved budget for the
    // current year, restricted to months that have already started (a future month's actual is
    // necessarily zero, which is not a variance). Actual is signed by the account's normal
    // balance (ChartOfAccount.IsDebitNormal) so an expense account overspending and a revenue
    // account underperforming both read as "over"/"under" correctly, not as opposite signs of
    // the same raw debit-minus-credit number.
    private async Task<IReadOnlyList<SignalCandidate>> EvaluateBudgetVarianceAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var year = today.Year;
        var budgets = await store.Budgets.AsNoTracking()
            .Where(b => b.Year == year && b.Status == BudgetStatus.Approved)
            .Select(b => new { b.Id, b.PropertyId })
            .ToListAsync(cancellationToken);
        if (budgets.Count == 0) return [];
        var propertyByBudget = budgets.ToDictionary(b => b.Id, b => b.PropertyId);

        var budgetIds = budgets.Select(b => b.Id).ToHashSet();
        var lines = await store.BudgetLines.AsNoTracking()
            .Where(l => budgetIds.Contains(l.BudgetId) && l.Month <= today.Month)
            .Select(l => new { l.Id, l.BudgetId, l.AccountId, l.Month, l.Amount })
            .ToListAsync(cancellationToken);
        if (lines.Count == 0) return [];

        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var debitNormalByAccount = await store.ChartOfAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.IsDebitNormal })
            .ToDictionaryAsync(a => a.Id, a => a.IsDebitNormal, cancellationToken);

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);
        var activity = await (
            from jl in store.JournalLines.AsNoTracking()
            join je in store.JournalEntries.AsNoTracking() on jl.EntryId equals je.Id
            where accountIds.Contains(jl.AccountId) && je.EntryDate >= yearStart && je.EntryDate <= yearEnd
            select new { jl.AccountId, jl.PropertyId, je.EntryDate.Month, jl.Debit, jl.Credit })
            .ToListAsync(cancellationToken);

        var activityByKey = activity
            .GroupBy(a => (a.AccountId, a.PropertyId, a.Month))
            .ToDictionary(g => g.Key, g => (Debit: g.Sum(x => x.Debit), Credit: g.Sum(x => x.Credit)));

        var candidates = new List<SignalCandidate>();
        foreach (var line in lines)
        {
            var propertyId = propertyByBudget[line.BudgetId];
            var (debit, credit) = activityByKey.TryGetValue((line.AccountId, (Guid?)propertyId, line.Month), out var sums)
                ? sums : (0m, 0m);
            var isDebitNormal = debitNormalByAccount.TryGetValue(line.AccountId, out var normal) && normal;
            var actual = isDebitNormal ? debit - credit : credit - debit;

            var candidate = AccountingSignalRules.EvaluateBudgetVariance(
                new BudgetVarianceSnapshot(line.Id, line.Month, line.Amount, actual), now);
            if (candidate is not null) candidates.Add(candidate);
        }
        return candidates;
    }

    private async Task<IReadOnlyList<SignalCandidate>> EvaluateInvoiceExceptionsAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var payables = await store.PayableInvoices.AsNoTracking()
            .Where(i => i.Status == PayableStatus.Open)
            .Select(i => new { i.Id, i.Amount, i.AmountPaid, i.DueOn })
            .ToListAsync(cancellationToken);
        var receivables = await store.ReceivableInvoices.AsNoTracking()
            .Where(i => i.Status == ReceivableStatus.Open)
            .Select(i => new { i.Id, i.Amount, i.AmountPaid, i.DueOn })
            .ToListAsync(cancellationToken);

        var candidates = new List<SignalCandidate>();
        candidates.AddRange(payables
            .Select(i => AccountingSignalRules.EvaluateInvoiceException(
                new InvoiceSignalSnapshot(i.Id, "PayableInvoice", i.Amount - i.AmountPaid, i.DueOn), today, now))
            .OfType<SignalCandidate>());
        candidates.AddRange(receivables
            .Select(i => AccountingSignalRules.EvaluateInvoiceException(
                new InvoiceSignalSnapshot(i.Id, "ReceivableInvoice", i.Amount - i.AmountPaid, i.DueOn), today, now))
            .OfType<SignalCandidate>());
        return candidates;
    }

    // Loads real entities, not a projection — ComplianceSignalRules calls the obligation's own
    // IsOverdue/IsEscalated rather than re-deriving that date math (see its own comment).
    private async Task<IReadOnlyList<SignalCandidate>> EvaluateComplianceObligationsAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var obligations = await store.ComplianceObligations.AsNoTracking()
            .Where(o => o.Status == ComplianceObligationStatus.Active)
            .ToListAsync(cancellationToken);

        return obligations
            .Select(o => ComplianceSignalRules.EvaluateComplianceObligation(o, today, now))
            .OfType<SignalCandidate>()
            .ToList();
    }

    // Loads real entities for the same reason ComplianceObligations does.
    private async Task<IReadOnlyList<SignalCandidate>> EvaluateAssetReplacementAsync(DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var assets = await store.Assets.AsNoTracking()
            .Where(a => a.InstalledOn != null && a.ExpectedServiceLifeYears != null)
            .ToListAsync(cancellationToken);

        return assets
            .Select(a => AssetSignalRules.EvaluateAssetReplacement(a, today, now))
            .OfType<SignalCandidate>()
            .ToList();
    }
}
