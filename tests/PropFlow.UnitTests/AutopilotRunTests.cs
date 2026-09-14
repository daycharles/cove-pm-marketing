using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotRunTests
{
    private static AutopilotRun Create() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Scheduled", DateTimeOffset.UtcNow);

    [Fact]
    public void A_new_run_is_running_with_no_completion_state()
    {
        var run = Create();
        Assert.Equal(AutopilotRunStatus.Running, run.Status);
        Assert.Null(run.CompletedAt);
        Assert.Equal(0, run.FindingCount);
        Assert.False(run.IsTerminal);
    }

    [Fact]
    public void An_empty_trigger_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AutopilotRun(Guid.NewGuid(), Guid.NewGuid(), "", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Complete_records_the_finding_count_and_completion_time()
    {
        var run = Create();
        var now = DateTimeOffset.UtcNow;
        run.Complete(7, now);
        Assert.Equal(AutopilotRunStatus.Completed, run.Status);
        Assert.Equal(7, run.FindingCount);
        Assert.Equal(now, run.CompletedAt);
        Assert.True(run.IsTerminal);
    }

    [Fact]
    public void A_negative_finding_count_is_rejected()
    {
        var run = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => run.Complete(-1, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Fail_records_the_reason()
    {
        var run = Create();
        run.Fail("Analyzer threw", DateTimeOffset.UtcNow);
        Assert.Equal(AutopilotRunStatus.Failed, run.Status);
        Assert.Equal("Analyzer threw", run.FailureReason);
        Assert.True(run.IsTerminal);
    }

    [Fact]
    public void A_terminal_run_cannot_be_completed_or_failed_again()
    {
        var run = Create();
        run.Complete(1, DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => run.Complete(2, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => run.Fail("too late", DateTimeOffset.UtcNow));
    }
}
