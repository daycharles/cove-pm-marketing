using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotFindingTests
{
    private static AutopilotFinding Create(AttentionSeverity severity = AttentionSeverity.Warning) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "OverdueCriticalWork", severity,
        "WorkItem", Guid.NewGuid(), "Critical work item overdue", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public void A_new_finding_starts_new_with_no_decision()
    {
        var finding = Create();
        Assert.Equal(FindingLifecycleStatus.New, finding.Status);
        Assert.Null(finding.ReviewedAt);
        Assert.Null(finding.DecidedAt);
        Assert.False(finding.IsTerminal);
    }

    [Fact]
    public void An_undefined_severity_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create((AttentionSeverity)999));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_signal_type_or_subject_type_is_rejected(string blank)
    {
        Assert.Throws<ArgumentException>(() => new AutopilotFinding(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), blank, AttentionSeverity.Warning,
            "WorkItem", Guid.NewGuid(), "Summary", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new AutopilotFinding(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Signal", AttentionSeverity.Warning,
            blank, Guid.NewGuid(), "Summary", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Review_moves_a_new_finding_to_reviewed()
    {
        var finding = Create();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        finding.Review(actor, now);
        Assert.Equal(FindingLifecycleStatus.Reviewed, finding.Status);
        Assert.Equal(actor, finding.ReviewedBy);
        Assert.Equal(now, finding.ReviewedAt);
    }

    [Fact]
    public void Review_cannot_be_repeated()
    {
        var finding = Create();
        finding.Review(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => finding.Review(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Dismiss_is_reachable_directly_from_new_without_a_review_step()
    {
        var finding = Create();
        finding.Dismiss(Guid.NewGuid(), DateTimeOffset.UtcNow, "Not applicable to this property");
        Assert.Equal(FindingLifecycleStatus.Dismissed, finding.Status);
        Assert.Equal("Not applicable to this property", finding.DismissedReason);
        Assert.True(finding.IsTerminal);
    }

    [Fact]
    public void Resolve_is_reachable_after_review()
    {
        var finding = Create();
        finding.Review(Guid.NewGuid(), DateTimeOffset.UtcNow);
        finding.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(FindingLifecycleStatus.Resolved, finding.Status);
        Assert.True(finding.IsTerminal);
    }

    [Fact]
    public void A_terminal_finding_refuses_every_further_transition()
    {
        var finding = Create();
        finding.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => finding.Review(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => finding.Dismiss(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => finding.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void An_empty_actor_is_rejected_on_every_transition()
    {
        var finding = Create();
        Assert.Throws<ArgumentException>(() => finding.Review(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => finding.Dismiss(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => finding.Resolve(Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Snooze_is_reachable_directly_from_new_and_is_not_terminal()
    {
        var finding = Create();
        var now = DateTimeOffset.UtcNow;
        finding.Snooze(Guid.NewGuid(), now, now.AddDays(3));
        Assert.Equal(FindingLifecycleStatus.Snoozed, finding.Status);
        Assert.Equal(now.AddDays(3), finding.SnoozedUntil);
        Assert.False(finding.IsTerminal);
    }

    [Fact]
    public void Snoozing_again_moves_the_until_date_further_out()
    {
        var finding = Create();
        var now = DateTimeOffset.UtcNow;
        finding.Snooze(Guid.NewGuid(), now, now.AddDays(1));
        finding.Snooze(Guid.NewGuid(), now, now.AddDays(7));
        Assert.Equal(now.AddDays(7), finding.SnoozedUntil);
    }

    [Fact]
    public void Snooze_until_must_be_in_the_future()
    {
        var finding = Create();
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => finding.Snooze(Guid.NewGuid(), now, now));
        Assert.Throws<ArgumentException>(() => finding.Snooze(Guid.NewGuid(), now, now.AddMinutes(-1)));
    }

    [Fact]
    public void A_terminal_finding_cannot_be_snoozed()
    {
        var finding = Create();
        finding.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => finding.Snooze(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)));
    }

    [Theory]
    [InlineData(FindingLifecycleStatus.Snoozed)]
    [InlineData(FindingLifecycleStatus.Dismissed)]
    [InlineData(FindingLifecycleStatus.Resolved)]
    public void Reopen_returns_any_of_the_three_non_active_states_to_new_with_a_clean_slate(FindingLifecycleStatus from)
    {
        var finding = Create();
        var now = DateTimeOffset.UtcNow;
        switch (from)
        {
            case FindingLifecycleStatus.Snoozed: finding.Snooze(Guid.NewGuid(), now, now.AddDays(1)); break;
            case FindingLifecycleStatus.Dismissed: finding.Dismiss(Guid.NewGuid(), now, "reason"); break;
            case FindingLifecycleStatus.Resolved: finding.Resolve(Guid.NewGuid(), now); break;
        }

        finding.Reopen(Guid.NewGuid(), now);

        Assert.Equal(FindingLifecycleStatus.New, finding.Status);
        Assert.False(finding.IsTerminal);
        Assert.Null(finding.SnoozedUntil);
        Assert.Null(finding.DismissedReason);
        Assert.Null(finding.DecidedAt);
        Assert.Null(finding.ReviewedBy);
        Assert.Null(finding.ReviewedAt);
    }

    [Fact]
    public void A_new_or_reviewed_finding_cannot_be_reopened_because_it_was_never_closed()
    {
        var finding = Create();
        Assert.Throws<InvalidOperationException>(() => finding.Reopen(Guid.NewGuid(), DateTimeOffset.UtcNow));
        finding.Review(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => finding.Reopen(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reopen_rejects_an_empty_actor()
    {
        var finding = Create();
        finding.Resolve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => finding.Reopen(Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PropertyId_and_PortfolioId_are_optional_and_default_to_null()
    {
        var finding = Create();
        Assert.Null(finding.PropertyId);
        Assert.Null(finding.PortfolioId);
    }

    [Fact]
    public void An_empty_guid_PropertyId_or_PortfolioId_is_treated_as_absent_not_stored_as_empty()
    {
        var finding = new AutopilotFinding(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Signal", AttentionSeverity.Warning,
            "WorkItem", Guid.NewGuid(), "Summary", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            propertyId: Guid.Empty, portfolioId: Guid.Empty);
        Assert.Null(finding.PropertyId);
        Assert.Null(finding.PortfolioId);
    }
}
