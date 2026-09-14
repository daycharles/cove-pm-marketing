using System.Text.Json;
using PropFlow.Application.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

// Confirms a provider's raw output parses as JSON and carries every required field with a
// non-empty value - see IStructuredOutputValidator.cs for why this stops short of a full
// JSON-Schema engine.
public sealed class JsonStructuredOutputValidator : IStructuredOutputValidator
{
    public StructuredOutputValidationResult Validate(string output, IReadOnlyCollection<string> requiredFields)
    {
        ArgumentNullException.ThrowIfNull(requiredFields);
        if (string.IsNullOrWhiteSpace(output))
            return new StructuredOutputValidationResult(false, ["Output is empty."]);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(output);
        }
        catch (JsonException ex)
        {
            return new StructuredOutputValidationResult(false, [$"Output is not valid JSON: {ex.Message}"]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new StructuredOutputValidationResult(false, ["Output must be a JSON object."]);

            var errors = new List<string>();
            foreach (var field in requiredFields)
            {
                if (!document.RootElement.TryGetProperty(field, out var value))
                {
                    errors.Add($"Missing required field '{field}'.");
                    continue;
                }
                var isEmpty = value.ValueKind switch
                {
                    JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
                    JsonValueKind.Null => true,
                    _ => false,
                };
                if (isEmpty) errors.Add($"Required field '{field}' is empty.");
            }

            return errors.Count == 0 ? StructuredOutputValidationResult.Valid : new StructuredOutputValidationResult(false, errors);
        }
    }
}
