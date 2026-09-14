using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

public static class BudgetVarianceThresholds
{
    public static readonly decimal WarningPercent = 0.20m;
    public static readonly decimal CriticalPercent = 0.50m;
    // A budget line with nothing budgeted against it has an undefined percent variance (division
    // by zero) — any real spend there needs an absolute floor instead, so a $6 charge against an
    // empty line doesn't fire a finding.
    public static readonly decimal UnbudgetedSpendFloor = 500m;
}

public static class InvoiceExceptionThresholds
{
    // A small buffer for ordinary payment-processing lag — an invoice one day past its due date
    // is not yet an exception.
    public static readonly int GraceDays = 3;
    public static readonly int SeriouslyOverdueDays = 30;
}

/// <summary>
/// One budget line's budgeted-vs-actual for one month. The actual side (summed <c>JournalLine</c>
/// debits/credits for the line's account, in its month) is a join <c>EfSignalCatalog</c> does —
/// this rule only sees the two numbers, not how they were derived. PropertyId comes straight off
/// the parent <c>Budget</c>, which already carries one.
/// </summary>
public sealed record BudgetVarianceSnapshot(Guid BudgetLineId, int Month, decimal Budgeted, decimal Actual, Guid? PropertyId = null);

/// <summary>
/// One payable or receivable invoice still owed. <see cref="SubjectType"/> carries which —
/// <c>"PayableInvoice"</c> or <c>"ReceivableInvoice"</c> — since the two entities are otherwise
/// shaped identically and the rule is the same for both. Deliberately no PropertyId: neither
/// entity has one, or a clean join to one — a payable is vendor-scoped, a receivable is
/// resident-scoped, and property filtering genuinely does not apply to this signal type.
/// </summary>
public sealed record InvoiceSignalSnapshot(Guid InvoiceId, string SubjectType, decimal Outstanding, DateOnly DueOn);

public static class AccountingSignalRules
{
    public static SignalCandidate? EvaluateBudgetVariance(BudgetVarianceSnapshot line, DateTimeOffset now)
    {
        var variance = line.Actual - line.Budgeted;
        if (variance == 0) return null;
        var direction = variance > 0 ? "over" : "under";

        string summary;
        AttentionSeverity severity;
        if (line.Budgeted == 0)
        {
            if (Math.Abs(variance) < BudgetVarianceThresholds.UnbudgetedSpendFloor) return null;
            severity = AttentionSeverity.Warning;
            summary = $"{AutopilotFormatting.Money(Math.Abs(variance))} spent in month {line.Month} against a line with no budget.";
        }
        else
        {
            var percent = Math.Abs(variance) / line.Budgeted;
            if (percent < BudgetVarianceThresholds.WarningPercent) return null;
            severity = percent >= BudgetVarianceThresholds.CriticalPercent ? AttentionSeverity.Critical : AttentionSeverity.Warning;
            summary = $"{AutopilotFormatting.Money(Math.Abs(variance))} ({AutopilotFormatting.Percent(percent)}) {direction} budget for month {line.Month}.";
        }

        var evidence = new EvidenceCandidate(
            [
                new CalculationInput("Budgeted", AutopilotFormatting.Money(line.Budgeted)),
                new CalculationInput("Actual", AutopilotFormatting.Money(line.Actual)),
                new CalculationInput("Variance", AutopilotFormatting.Money(variance)),
                new CalculationInput("Month", line.Month.ToString()),
            ],
            [new SourceLink("BudgetLine", line.BudgetLineId)],
            new ImpactEstimate(ImpactCategory.Financial, $"Spending {direction} budget for the month.", Math.Abs(variance)),
            Confidence: 1.0);

        return new SignalCandidate(SignalTypes.BudgetVariance, severity, "BudgetLine", line.BudgetLineId, summary, now, now, evidence, line.PropertyId);
    }

    public static SignalCandidate? EvaluateInvoiceException(InvoiceSignalSnapshot invoice, DateOnly today, DateTimeOffset now)
    {
        if (invoice.Outstanding <= 0) return null;
        var daysOverdue = today.DayNumber - invoice.DueOn.DayNumber;
        if (daysOverdue <= InvoiceExceptionThresholds.GraceDays) return null;

        var severity = daysOverdue >= InvoiceExceptionThresholds.SeriouslyOverdueDays
            ? AttentionSeverity.Critical
            : AttentionSeverity.Warning;
        var summary = $"{AutopilotFormatting.Money(invoice.Outstanding)} unpaid, {AutopilotFormatting.Days(daysOverdue)} past due.";

        var evidence = new EvidenceCandidate(
            [
                new CalculationInput("Outstanding", AutopilotFormatting.Money(invoice.Outstanding)),
                new CalculationInput("Due on", invoice.DueOn.ToString("yyyy-MM-dd")),
                new CalculationInput("Days overdue", daysOverdue.ToString()),
            ],
            [new SourceLink(invoice.SubjectType, invoice.InvoiceId)],
            new ImpactEstimate(ImpactCategory.Financial, "Invoice unpaid past its due date.", invoice.Outstanding),
            Confidence: 1.0);

        return new SignalCandidate(SignalTypes.InvoiceException, severity, invoice.SubjectType, invoice.InvoiceId, summary, now, now, evidence);
    }
}
