using PropFlow.Application.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

// Wraps any IModelGateway with a hard timeout, converting a slow or hung provider call into
// ModelGatewayOutcome.Unavailable rather than letting it run past the caller's patience or throw
// an OperationCanceledException the caller would otherwise have to catch itself - the same
// "outcomes, not exceptions" contract IModelGateway.cs documents. Provider-agnostic: this
// decorates whichever IModelGateway is registered, including NoOpModelGateway (which never times
// out, since it never awaits anything) and, later, a real provider's implementation. The timeout
// itself is real elapsed time (CancellationTokenSource(TimeSpan)), not the injected TimeProvider
// - a real network call times out against the wall clock, not a simulated one; clock is used
// only to measure the Latency this reports, the same audit-relevant value every other Autopilot
// type takes a TimeProvider for.
public sealed class TimeoutModelGateway(IModelGateway inner, AutopilotGatewayOptions options, TimeProvider clock) : IModelGateway
{
    public async Task<ModelGatewayResult> CompleteAsync(ModelGatewayRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var start = clock.GetTimestamp();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.TimeoutMilliseconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await inner.CompleteAsync(request, linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return new ModelGatewayResult(
                ModelGatewayOutcome.Unavailable,
                Output: null,
                FailureReason: $"Timed out after {options.TimeoutMilliseconds}ms.",
                ProviderName: "timeout",
                Latency: clock.GetElapsedTime(start));
        }
    }
}
