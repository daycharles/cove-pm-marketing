namespace PropFlow.Application.Autopilot;

// CPM-8.10. Outcomes, not exceptions, same as IModelGateway/IScreeningProvider - a refusal is a
// normal, expected result, not a caught error. Unauthorized deliberately does NOT appear here:
// the one capability Ask CovePM needs today (Autopilot.Manage) is already enforced at the HTTP
// boundary (.RequireAuthorization, .claude/rules/architecture.md's own rule), the same as every
// other Autopilot endpoint - there is no narrower per-question authorization concept in this
// codebase yet for this service to enforce a second time.
public enum AskCovePMOutcome { Answered, Refused, Unavailable }

public enum AskCovePMRefusalReason
{
    // The question itself was empty, too long, or matched a known prompt-injection phrase
    // (AskCovePMContextAssembler.LooksLikeInjectionAttempt) - refused before any retrieval runs.
    UnsafeQuestion,
    // Retrieval found no workable finding whose text overlaps the question at all - answering
    // anyway would mean inventing an answer with nothing behind it, which this epic's own
    // "never fabricates missing values" charter (CPM-8.03) already rules out.
    NoMatchingData,
}

public sealed record AskCovePMSource(Guid FindingId, string SignalType, string Summary);

public sealed record AskCovePMAnswer(string Text, IReadOnlyList<AskCovePMSource> Sources);

public sealed record AskCovePMRequest(string Question, Guid? PropertyId, Guid? PortfolioId);

// FailureReason carries IModelGateway's own FailureReason when Outcome is Unavailable - "no
// model provider is configured" today, whatever a real provider's outage message is once one
// exists.
public sealed record AskCovePMResult(
    AskCovePMOutcome Outcome,
    AskCovePMAnswer? Answer,
    AskCovePMRefusalReason? RefusalReason,
    string? FailureReason);

/// <summary>
/// Read-only natural-language questions over Autopilot's approved projections (findings +
/// evidence) - CPM-8.10. Grounded, source-linked answers only; every refusal path is a real
/// outcome the caller must handle, not a special case. "Deterministic operation must remain
/// possible without an AI provider" (the epic's own non-negotiable, restated in
/// <c>NoOpModelGateway</c>'s own comment) applies here exactly as it does to the rest of
/// Autopilot: wired to the no-op gateway, this always answers <see cref="AskCovePMOutcome.Unavailable"/>
/// for a question that passes the safety and retrieval checks - a fully supported outcome, not a
/// degraded one.
/// </summary>
public interface IAskCovePMService
{
    Task<AskCovePMResult> AskAsync(AskCovePMRequest request, CancellationToken cancellationToken);
}
