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
}
