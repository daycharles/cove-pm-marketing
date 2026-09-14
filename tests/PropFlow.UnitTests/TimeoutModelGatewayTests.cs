using PropFlow.Application.Autopilot;
using PropFlow.Infrastructure.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class TimeoutModelGatewayTests
{
    private static ModelGatewayRequest Request() =>
        new(Guid.NewGuid(), "any-prompt", 1, new Dictionary<string, string>(), Guid.NewGuid().ToString());

    private sealed class DelayingGateway(TimeSpan delay, ModelGatewayResult onComplete) : IModelGateway
    {
        public async Task<ModelGatewayResult> CompleteAsync(ModelGatewayRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return onComplete;
        }
    }

    private static readonly ModelGatewayResult Completed =
        new(ModelGatewayOutcome.Completed, "{}", null, "fake", TimeSpan.Zero);

    [Fact]
    public async Task A_call_that_finishes_within_the_timeout_passes_through_unchanged()
    {
        var inner = new DelayingGateway(TimeSpan.Zero, Completed);
        var gateway = new TimeoutModelGateway(inner, new AutopilotGatewayOptions { TimeoutMilliseconds = 5000 }, TimeProvider.System);

        var result = await gateway.CompleteAsync(Request(), CancellationToken.None);

        Assert.Equal(ModelGatewayOutcome.Completed, result.Outcome);
        Assert.Equal("{}", result.Output);
    }

    [Fact]
    public async Task A_call_that_outlasts_the_timeout_becomes_unavailable_not_an_exception()
    {
        // A short real timeout against a deliberately slower inner call — this is a genuine
        // (not simulated) cancellation race, kept to tens of milliseconds so the suite stays fast.
        var inner = new DelayingGateway(TimeSpan.FromMilliseconds(500), Completed);
        var gateway = new TimeoutModelGateway(inner, new AutopilotGatewayOptions { TimeoutMilliseconds = 30 }, TimeProvider.System);

        var result = await gateway.CompleteAsync(Request(), CancellationToken.None);

        Assert.Equal(ModelGatewayOutcome.Unavailable, result.Outcome);
        Assert.Null(result.Output);
        Assert.Equal("timeout", result.ProviderName);
        Assert.Contains("Timed out", result.FailureReason);
    }

    [Fact]
    public async Task The_callers_own_cancellation_still_throws_rather_than_being_swallowed_as_a_timeout()
    {
        var inner = new DelayingGateway(TimeSpan.FromMilliseconds(500), Completed);
        var gateway = new TimeoutModelGateway(inner, new AutopilotGatewayOptions { TimeoutMilliseconds = 5000 }, TimeProvider.System);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gateway.CompleteAsync(Request(), cts.Token));
    }
}
