namespace PropFlow.Application.Autopilot;

public sealed record StructuredOutputValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static readonly StructuredOutputValidationResult Valid = new(true, []);
}

// A provider's raw output (ModelGatewayResult.Output) is untrusted text until this checks it -
// CPM-8.04's "structured output validation". Deliberately narrow: this confirms the output
// parses as JSON and carries every required field with a non-empty value, not a full JSON-Schema
// engine (no new package, no packages.lock.json churn for a validator this task's own no-op
// gateway never has real output to feed it - "machinery, not vendors", the same call FS-S19 made
// for its integration adapters). A future task wiring a real provider can tighten this into a
// real schema if the field set grows past what a required-keys check can defend.
public interface IStructuredOutputValidator
{
    StructuredOutputValidationResult Validate(string output, IReadOnlyCollection<string> requiredFields);
}
