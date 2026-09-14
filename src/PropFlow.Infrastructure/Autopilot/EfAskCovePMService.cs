using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PropFlow.Application.Autopilot;
using PropFlow.Domain.Autopilot;

namespace PropFlow.Infrastructure.Autopilot;

public sealed class EfAskCovePMService(
    AutopilotStore store,
    IModelGateway gateway,
    IPromptCatalog prompts) : IAskCovePMService
{
    private const int MaxMatches = 5;

    public async Task<AskCovePMResult> AskAsync(AskCovePMRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var question = request.Question?.Trim() ?? "";
        if (question.Length == 0 || question.Length > AskCovePMContextAssembler.QuestionMaxLength ||
            AskCovePMContextAssembler.LooksLikeInjectionAttempt(question))
            return Refused(AskCovePMRefusalReason.UnsafeQuestion);

        // The same "workable set" the brief defaults to (CPM-8.05) - a dismissed or resolved
        // finding is a decided, closed matter, not something Ask CovePM should ground a fresh
        // answer in.
        var query = store.Findings.AsNoTracking()
            .Where(f => f.Status != FindingLifecycleStatus.Dismissed && f.Status != FindingLifecycleStatus.Resolved);
        if (request.PropertyId is { } propertyId) query = query.Where(f => f.PropertyId == propertyId);
        if (request.PortfolioId is { } portfolioId) query = query.Where(f => f.PortfolioId == portfolioId);
        var candidates = await query.ToListAsync(cancellationToken);

        var matches = AskCovePMRetrieval.Rank(question, candidates, MaxMatches);
        if (matches.Count == 0) return Refused(AskCovePMRefusalReason.NoMatchingData);

        var matchIds = matches.Select(f => f.Id).ToList();
        var evidenceByFinding = await store.Evidence.AsNoTracking()
            .Where(e => matchIds.Contains(e.FindingId))
            .ToDictionaryAsync(e => e.FindingId, cancellationToken);

        // A finding missing its evidence row would be a real data-integrity bug (CPM-8.05's
        // EfAutopilotRunner always persists a finding and its evidence together) - surfaced here
        // as a straight exclusion rather than a crash, since one broken row should not take down
        // every other answer.
        var pairs = matches
            .Where(f => evidenceByFinding.ContainsKey(f.Id))
            .Select(f => (Finding: f, Evidence: evidenceByFinding[f.Id]))
            .ToList();
        if (pairs.Count == 0) return Refused(AskCovePMRefusalReason.NoMatchingData);

        var context = AskCovePMContextAssembler.Assemble(question, pairs);
        var promptLookup = prompts.Resolve(StaticPromptCatalog.AskCovePMId);
        if (promptLookup.Outcome != PromptLookupOutcome.Found || promptLookup.Prompt is null)
            throw new InvalidOperationException($"Prompt '{StaticPromptCatalog.AskCovePMId}' is not registered.");

        var result = await gateway.CompleteAsync(
            new ModelGatewayRequest(store.OrganizationId, promptLookup.Prompt.Id, promptLookup.Prompt.Version, context,
                IdempotencyKey(store.OrganizationId, question, matchIds)),
            cancellationToken);

        if (result.Outcome == ModelGatewayOutcome.Unavailable || result.Output is null)
            return new AskCovePMResult(AskCovePMOutcome.Unavailable, null, null, result.FailureReason);

        var sources = pairs.Select(p => new AskCovePMSource(p.Finding.Id, p.Finding.SignalType, p.Finding.Summary)).ToList();
        return new AskCovePMResult(AskCovePMOutcome.Answered, new AskCovePMAnswer(result.Output, sources), null, null);
    }

    private static AskCovePMResult Refused(AskCovePMRefusalReason reason) =>
        new(AskCovePMOutcome.Refused, null, reason, null);

    // A retry of the exact same question, matched to the exact same findings, reuses the same
    // key - the same "a provider returns the first answer for a key it has already seen" contract
    // IScreeningProvider's own IdempotencyKey already documents. Matched-finding ids are part of
    // the key (not just the question text) because the same question asked again after new
    // findings appeared is a genuinely different request, not a retry of the old one.
    private static string IdempotencyKey(Guid organizationId, string question, IReadOnlyList<Guid> matchIds)
    {
        var raw = $"{organizationId}:{question.Trim().ToLowerInvariant()}:{string.Join(',', matchIds.OrderBy(id => id))}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
