using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AccountingSignalRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 14);
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Actual_matching_budget_exactly_produces_no_signal()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 1000m, Actual: 1000m);
        Assert.Null(AccountingSignalRules.EvaluateBudgetVariance(line, Now));
    }

    [Fact]
    public void A_small_variance_under_the_warning_threshold_produces_no_signal()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 1000m, Actual: 1100m);
        Assert.Null(AccountingSignalRules.EvaluateBudgetVariance(line, Now));
    }

    [Fact]
    public void A_variance_at_the_warning_threshold_is_a_warning()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 1000m, Actual: 1200m);
        var candidate = AccountingSignalRules.EvaluateBudgetVariance(line, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.BudgetVariance, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("BudgetLine", candidate.SubjectType);
        Assert.Contains("over", candidate.Summary);
    }

    [Fact]
    public void A_variance_at_the_critical_threshold_is_critical()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 1000m, Actual: 1500m);
        var candidate = AccountingSignalRules.EvaluateBudgetVariance(line, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Critical, candidate.Severity);
    }

    [Fact]
    public void Underspending_by_enough_also_flags_with_under_in_the_summary()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 1000m, Actual: 700m);
        var candidate = AccountingSignalRules.EvaluateBudgetVariance(line, Now);
        Assert.NotNull(candidate);
        Assert.Contains("under", candidate.Summary);
    }

    [Fact]
    public void A_small_spend_against_an_unbudgeted_line_stays_below_the_absolute_floor()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 0m, Actual: 50m);
        Assert.Null(AccountingSignalRules.EvaluateBudgetVariance(line, Now));
    }

    [Fact]
    public void Spend_over_the_absolute_floor_against_an_unbudgeted_line_is_a_warning()
    {
        var line = new BudgetVarianceSnapshot(Guid.NewGuid(), Month: 9, Budgeted: 0m,
            Actual: BudgetVarianceThresholds.UnbudgetedSpendFloor + 1m);
        var candidate = AccountingSignalRules.EvaluateBudgetVariance(line, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
    }

    [Fact]
    public void A_fully_paid_invoice_produces_no_signal()
    {
        var invoice = new InvoiceSignalSnapshot(Guid.NewGuid(), "PayableInvoice", Outstanding: 0m, Today.AddDays(-60));
        Assert.Null(AccountingSignalRules.EvaluateInvoiceException(invoice, Today, Now));
    }

    [Fact]
    public void An_invoice_inside_the_grace_period_produces_no_signal()
    {
        var invoice = new InvoiceSignalSnapshot(Guid.NewGuid(), "PayableInvoice", Outstanding: 500m, Today.AddDays(-1));
        Assert.Null(AccountingSignalRules.EvaluateInvoiceException(invoice, Today, Now));
    }

    [Fact]
    public void An_invoice_past_the_grace_period_is_a_warning()
    {
        var invoice = new InvoiceSignalSnapshot(Guid.NewGuid(), "PayableInvoice", Outstanding: 500m,
            Today.AddDays(-(InvoiceExceptionThresholds.GraceDays + 1)));
        var candidate = AccountingSignalRules.EvaluateInvoiceException(invoice, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.InvoiceException, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("PayableInvoice", candidate.SubjectType);
    }

    [Fact]
    public void An_invoice_seriously_overdue_is_critical()
    {
        var invoice = new InvoiceSignalSnapshot(Guid.NewGuid(), "ReceivableInvoice", Outstanding: 500m,
            Today.AddDays(-InvoiceExceptionThresholds.SeriouslyOverdueDays));
        var candidate = AccountingSignalRules.EvaluateInvoiceException(invoice, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(AttentionSeverity.Critical, candidate.Severity);
        Assert.Equal("ReceivableInvoice", candidate.SubjectType);
    }
}
