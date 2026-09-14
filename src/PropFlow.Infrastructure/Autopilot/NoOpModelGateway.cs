using PropFlow.Application.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

// The default, and for this task the only real, IModelGateway: no network, no credential, always
// Unavailable. This is what "deterministic operation must remain possible without an AI
// provider" (the epic's own non-negotiable) means as running code, not just a design intention -
// every caller of IModelGateway already has to handle Unavailable (ModelGatewayOutcome has no
// other outage-shaped member), so wiring this in production is not a degraded mode, it is a
// fully supported one. A future task adds a real provider's IModelGateway implementation
// alongside this one, the same way SandboxIntegrationAdapter sits alongside MockIntegrationAdapter
// (PF-S19.07) - this file is not meant to be deleted when that happens.
public sealed class NoOpModelGateway : IModelGateway
{
    public const string Name = "none";

    public Task<ModelGatewayResult> CompleteAsync(ModelGatewayRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new ModelGatewayResult(
            ModelGatewayOutcome.Unavailable,
            Output: null,
            FailureReason: "No model provider is configured.",
            ProviderName: Name,
            Latency: TimeSpan.Zero);
        return Task.FromResult(result);
    }
}
