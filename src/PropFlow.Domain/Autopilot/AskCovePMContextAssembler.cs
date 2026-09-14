namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Builds the minimized, redacted key-value context one Ask CovePM question is allowed to see —
/// the same "tenant-safe context assembly" <see cref="AutopilotContextAssembler"/> already does
/// for a single finding, extended to the several findings a question's evidence set may span.
/// Every finding's fields land under a numbered <c>finding_N.*</c> prefix (reusing
/// <see cref="AutopilotContextAssembler.Assemble"/> per finding, since <c>ModelGatewayRequest.Context</c>
/// is a flat <c>IReadOnlyDictionary&lt;string,string&gt;</c>, not nested) and the question itself
/// lands under a single <c>question</c> key. This
/// numbered-prefix separation IS the prompt-injection defense CPM-8.10's own scope calls for:
/// resident/vendor-authored text that ends up in a finding's summary or evidence can never be
/// concatenated into one blob a model might read as an instruction — it always arrives keyed as
/// one specific finding's one specific field, labeled data, never free-floating text the way the
/// user's own <c>question</c> key is (see <see cref="StaticPromptCatalog.AskCovePMId"/>'s own
/// instructions for how a real provider is told to treat that distinction).
/// </summary>
public static class AskCovePMContextAssembler
{
    public const int QuestionMaxLength = 500;

    public static IReadOnlyDictionary<string, string> Assemble(
        string question, IReadOnlyList<(AutopilotFinding Finding, AutopilotEvidence Evidence)> matches)
    {
        var trimmed = AutopilotText.RequireSingleLine(question, nameof(question), QuestionMaxLength);
        ArgumentNullException.ThrowIfNull(matches);
        if (matches.Count == 0)
            throw new ArgumentException("At least one matching finding is required.", nameof(matches));

        var context = new Dictionary<string, string> { ["question"] = AutopilotRedaction.Redact(trimmed) };
        for (var i = 0; i < matches.Count; i++)
        {
            var (finding, evidence) = matches[i];
            foreach (var (key, value) in AutopilotContextAssembler.Assemble(finding, evidence))
                context[$"finding_{i + 1}.{key}"] = value;
        }
        return context;
    }

    // Belt-and-suspenders on the asker's own side of the same defense: a question that itself
    // tries to override the instruction prompt is refused before it ever reaches a model, the
    // same "refuse rather than guess" instinct AutopilotFinding's own validation already applies
    // to malformed input. Not a complete defense on its own - a determined attacker can phrase
    // around a fixed phrase list - which is exactly why the numbered-prefix structural separation
    // above is the real control and this is only the cheap first line.
    public static bool LooksLikeInjectionAttempt(string question)
    {
        var lowered = question.ToLowerInvariant();
        return InjectionPhrases.Any(lowered.Contains);
    }

    private static readonly string[] InjectionPhrases =
    [
        "ignore previous instructions",
        "ignore prior instructions",
        "ignore all previous",
        "disregard previous",
        "disregard the above",
        "system prompt",
        "reveal your instructions",
        "you are now",
        "new instructions:",
    ];
}
