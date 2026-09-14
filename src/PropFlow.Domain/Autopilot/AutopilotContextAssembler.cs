namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Builds the minimized, redacted key-value context a model gateway call is allowed to see for
/// one finding — CPM-8.04's "tenant-safe context assembly". Deliberately narrow:
/// <see cref="AutopilotFinding.SubjectId"/> and every <see cref="AutopilotEvidence.SourceLinks"/>
/// entry are left out on purpose. A model reasoning about or summarizing a finding does not need
/// the literal internal record id to do that, and every id that leaves this boundary is one more
/// thing a provider now holds — the fewer, the better. Source links stay where they already are
/// (<see cref="AutopilotEvidence"/> itself), for the UI and the audit trail, never forwarded past
/// this assembler. Every free-text value (the summary, the impact description, each calculation
/// input's value) goes through <see cref="AutopilotRedaction.Redact"/> first.
/// </summary>
public static class AutopilotContextAssembler
{
    public static IReadOnlyDictionary<string, string> Assemble(AutopilotFinding finding, AutopilotEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(evidence);
        if (finding.Id != evidence.FindingId)
            throw new ArgumentException("Evidence does not belong to this finding.", nameof(evidence));

        var context = new Dictionary<string, string>
        {
            ["signal_type"] = finding.SignalType,
            ["severity"] = finding.Severity.ToString(),
            ["subject_type"] = finding.SubjectType,
            ["summary"] = AutopilotRedaction.Redact(finding.Summary),
            ["detected_at"] = finding.DetectedAt.ToString("O"),
            ["freshness_as_of"] = finding.FreshnessAsOf.ToString("O"),
            ["confidence"] = evidence.Confidence.ToString("F2"),
            ["impact_category"] = evidence.Impact?.Category.ToString() ?? "none",
            ["impact_description"] = evidence.Impact is { } impact ? AutopilotRedaction.Redact(impact.Description) : "",
            ["impact_estimated_amount"] = evidence.Impact?.EstimatedAmount?.ToString("F2") ?? "unknown",
        };

        foreach (var input in evidence.Inputs)
            context[$"input.{input.Name}"] = AutopilotRedaction.Redact(input.Value);

        return context;
    }
}
