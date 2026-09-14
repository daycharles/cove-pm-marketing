namespace PropFlow.Application.Autopilot;

// Built on the IScreeningProvider template (itself built on IPaymentGateway): outcomes, not
// exceptions (.claude/rules/architecture.md), and Unavailable is the provider-outage/timeout
// case, deliberately distinct from a real completion with unusable content — a gateway call that
// times out, that the configured provider mode refuses, or that hits no provider at all (the
// no-op default) all come back Unavailable, never an exception the caller has to catch. That is
// what "deterministic operation must remain possible without an AI provider" (the epic's own
// non-negotiable, docs/backlog.md) means at the type level: every caller already has to handle
// Unavailable, so "no provider configured" is not a special case, it is the same case as
// "provider timed out" or "provider had an outage".
public enum ModelGatewayOutcome { Completed, Unavailable }

// Context is the ONLY tenant data a provider call can see — built by
// PropFlow.Domain.Autopilot.AutopilotContextAssembler, never a raw domain entity handed through.
// PromptId/PromptVersion name which IPromptCatalog entry the caller expects the gateway to use;
// the gateway is responsible for actually using it, not the caller re-deriving instructions.
// IdempotencyKey mirrors IScreeningProvider's own — a provider is expected to return the first
// answer for a key it has already seen rather than re-running the call on a retry.
public sealed record ModelGatewayRequest(
    Guid OrganizationId,
    string PromptId,
    int PromptVersion,
    IReadOnlyDictionary<string, string> Context,
    string IdempotencyKey);

// Output is the raw text the provider returned (expected to be validated separately via
// IStructuredOutputValidator before anything trusts its shape) - null whenever Outcome is
// Unavailable, so a caller that only reads Output still cannot mistake an outage for an empty
// answer. Latency and ProviderName are always present (including on Unavailable) so a caller can
// log/measure an outage the same way it logs a success - CPM-8.13's operational metrics task
// depends on that symmetry existing already.
public sealed record ModelGatewayResult(
    ModelGatewayOutcome Outcome,
    string? Output,
    string? FailureReason,
    string ProviderName,
    TimeSpan Latency);

public interface IModelGateway
{
    Task<ModelGatewayResult> CompleteAsync(ModelGatewayRequest request, CancellationToken cancellationToken);
}
