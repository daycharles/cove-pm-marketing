namespace PropFlow.Domain.Autopilot;

// Shared bounded-string guards for the CPM-8 Autopilot aggregate, the same shape
// PropFlow.Domain.Marketing.ApplicationText uses for FS-S05 - kept as its own copy rather than a
// shared reference because neither is public, and a third near-identical copy is cheaper than
// the coupling a shared one would add between two unrelated features.
internal static class AutopilotText
{
    internal static string RequireSingleLine(string? value, string parameter, int maxLength)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length is 0 || trimmed.Length > maxLength)
            throw new ArgumentException($"Value must contain 1 to {maxLength} characters.", parameter);
        foreach (var ch in trimmed)
            if (char.IsControl(ch))
                throw new ArgumentException("Value must not contain control characters.", parameter);
        return trimmed;
    }

    // Multi-line is allowed here: a dismissal reason, a decision reason and a feedback comment
    // are prose a person typed, not a code or a status.
    internal static string? OptionalText(string? value, string parameter, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", parameter);
        return trimmed;
    }

    internal static Guid RequireId(Guid value, string parameter) =>
        value == Guid.Empty ? throw new ArgumentException("ID is required.", parameter) : value;
}
