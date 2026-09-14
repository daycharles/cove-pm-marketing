using PropFlow.Application.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

// One in-memory, fixed catalog entry per prompt id - no I/O, no configuration. A future task
// that needs prompts editable without a redeploy (an admin screen, a database row) replaces this
// registration, not the IPromptCatalog contract itself. Exactly one entry today: the daily
// brief / explain-a-finding use case CPM-8.05/.06 are expected to be the first real caller of.
public sealed class StaticPromptCatalog : IPromptCatalog
{
    public const string ExplainFindingId = "autopilot.explain-finding";

    private static readonly IReadOnlyDictionary<string, PromptDefinition> Prompts = new Dictionary<string, PromptDefinition>
    {
        [ExplainFindingId] = new PromptDefinition(
            ExplainFindingId,
            Version: 1,
            Instructions: "Summarize the property-management finding described by the given context in one or two " +
                "plain-language sentences a property manager can act on. Use only the facts present in the context; " +
                "never invent a detail, an amount, or a cause the context does not state."),
    };

    public PromptLookupResult Resolve(string promptId) =>
        Prompts.TryGetValue(promptId, out var prompt)
            ? new PromptLookupResult(PromptLookupOutcome.Found, prompt)
            : new PromptLookupResult(PromptLookupOutcome.NotFound, null);
}
