using System.Text.RegularExpressions;

namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Defensive redaction for free text that ultimately traces back to a user-entered name rather
/// than a system-computed number or date — a <c>ComplianceObligation.Title</c>, an
/// <c>Asset.Name</c>, flowing into a finding's <c>Summary</c>. Not a general PII classifier: a
/// best-effort net for the two identifiers most likely to appear by accident in a name someone
/// typed. Applied only to the values <see cref="AutopilotContextAssembler"/> sends past the model
/// boundary — never to <see cref="AutopilotEvidence"/> itself, which keeps the real values for
/// PropFlow's own audit trail.
/// </summary>
public static partial class AutopilotRedaction
{
    public static string Redact(string text) =>
        PhonePattern().Replace(EmailPattern().Replace(text, "[redacted-email]"), "[redacted-phone]");

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b")]
    private static partial Regex PhonePattern();
}
