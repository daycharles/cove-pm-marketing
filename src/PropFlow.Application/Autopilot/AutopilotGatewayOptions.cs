namespace PropFlow.Application.Autopilot;

// Bound from the "AutopilotGateway" configuration section, the ScreeningOptions/
// CommunicationsOptions idiom (Program.cs binds it manually via
// builder.Configuration.GetSection(X.SectionName).Get<X>() ?? new X() - no IOptions<T>
// anywhere in this codebase).
public sealed class AutopilotGatewayOptions
{
    public const string SectionName = "AutopilotGateway";

    // How long TimeoutModelGateway waits for the wrapped gateway before converting the call into
    // ModelGatewayOutcome.Unavailable. Configuration, not a constant, for the same reason
    // ScreeningOptions.MaxAttempts is: an operator tunes it without a redeploy once a real
    // provider's real latency is known, which is not yet the case for the no-op gateway this
    // task ships.
    public int TimeoutMilliseconds { get; set; } = 8000;

    public void Validate()
    {
        if (TimeoutMilliseconds < 1)
            throw new InvalidOperationException($"{SectionName}:TimeoutMilliseconds must be at least 1.");
    }
}
