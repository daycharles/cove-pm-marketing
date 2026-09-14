using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropFlow.Application;
using PropFlow.Application.Autopilot;
using PropFlow.Domain.Autopilot;
using PropFlow.Infrastructure.Autopilot;

namespace PropFlow.Api;

// CPM-8.05: the daily brief API. One capability (Autopilot.Manage — see Capabilities.cs's own
// comment on why there is no separate read-only role yet) gates the whole group; findings are
// listed, decided on, snoozed, marked read and given feedback here, and a run can be triggered
// manually. No scheduled trigger exists yet — that is left for whichever task adds one, matching
// CPM-8.01–8.04's pattern of shipping the machinery a slice at a time.
public static class AutopilotEndpoints
{
    public static void MapAutopilotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/autopilot").RequireAuthorization(Capabilities.ManageAutopilot);

        group.MapPost("/runs", async (AutopilotRunRequest? request, IAutopilotRunner runner, CancellationToken ct) =>
        {
            var trigger = string.IsNullOrWhiteSpace(request?.Trigger) ? "Manual" : request.Trigger;
            var result = await runner.RunAsync(trigger, ct);
            return result.Outcome == AutopilotRunOutcome.Completed
                ? Results.Ok(new AutopilotRunResponse(result.RunId, result.FindingCount))
                : Results.Problem(statusCode: 502, title: result.FailureReason ?? "The analysis run failed.");
        });

        group.MapGet("/brief", async (
            Guid? propertyId, Guid? portfolioId, string? signalType, FindingLifecycleStatus? status,
            int? page, int? pageSize, ClaimsPrincipal user, AutopilotStore store, CancellationToken ct) =>
        {
            var actor = Actor(user);
            var query =
                from f in store.Findings.AsNoTracking()
                join e in store.Evidence.AsNoTracking() on f.Id equals e.FindingId
                select new { Finding = f, Evidence = e };

            if (propertyId is { } property) query = query.Where(x => x.Finding.PropertyId == property);
            if (portfolioId is { } portfolio) query = query.Where(x => x.Finding.PortfolioId == portfolio);
            if (signalType is { } signal) query = query.Where(x => x.Finding.SignalType == signal);
            // Default view is the workable set — everything not already decided. Dismissed and
            // Resolved are still reachable, just only when asked for by name, the same "history
            // exists, but isn't the default view" shape /api/work's list gives completed work.
            query = status is { } requested
                ? query.Where(x => x.Finding.Status == requested)
                : query.Where(x => x.Finding.Status != FindingLifecycleStatus.Dismissed && x.Finding.Status != FindingLifecycleStatus.Resolved);

            var total = await query.CountAsync(ct);
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            // Severity first (its declaration order is deliberately ordinal — see
            // AutopilotStore's own comment), then the largest dollar impact, then oldest-detected
            // first within a tie so nothing silently ages out of view behind newer findings.
            var rows = await query
                .OrderByDescending(x => x.Finding.Severity)
                .ThenByDescending(x => x.Evidence.ImpactEstimatedAmount ?? 0)
                .ThenBy(x => x.Finding.DetectedAt)
                .ThenBy(x => x.Finding.Id)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            var findingIds = rows.Select(x => x.Finding.Id).ToList();
            var readFindingIds = (await store.FindingReads.AsNoTracking()
                .Where(r => r.ViewerId == actor && findingIds.Contains(r.FindingId))
                .Select(r => r.FindingId)
                .ToListAsync(ct))
                .ToHashSet();

            var items = rows.Select(x => ToSummary(x.Finding, x.Evidence, readFindingIds.Contains(x.Finding.Id))).ToList();
            return Results.Ok(new AutopilotBriefPage(items, total, currentPage, size));
        });

        group.MapPost("/findings/{id:guid}/review", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await Decide(id, store, ct, f => f.Review(Actor(user), clock.GetUtcNow())));

        group.MapPost("/findings/{id:guid}/dismiss", async (Guid id, DismissFindingRequest? request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await Decide(id, store, ct, f => f.Dismiss(Actor(user), clock.GetUtcNow(), request?.Reason)));

        group.MapPost("/findings/{id:guid}/resolve", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await Decide(id, store, ct, f => f.Resolve(Actor(user), clock.GetUtcNow())));

        group.MapPost("/findings/{id:guid}/snooze", async (Guid id, SnoozeFindingRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await Decide(id, store, ct, f => f.Snooze(Actor(user), clock.GetUtcNow(), request.Until)));

        group.MapPost("/findings/{id:guid}/reopen", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await Decide(id, store, ct, f => f.Reopen(Actor(user), clock.GetUtcNow())));

        group.MapPost("/findings/{id:guid}/read", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var finding = await store.Findings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (finding is null) return Results.NotFound();
            var actor = Actor(user);
            var now = clock.GetUtcNow();
            var read = await store.FindingReads.FirstOrDefaultAsync(x => x.FindingId == id && x.ViewerId == actor, ct);
            if (read is null)
            {
                store.FindingReads.Add(new AutopilotFindingRead(store.OrganizationId, Guid.NewGuid(), id, actor, now));
            }
            else
            {
                read.Touch(now);
            }
            await store.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/findings/{id:guid}/feedback", async (Guid id, RecordFeedbackRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var exists = await store.Findings.AnyAsync(x => x.Id == id, ct);
            if (!exists) return Results.NotFound();
            try
            {
                var feedback = new AutopilotFeedback(store.OrganizationId, Guid.NewGuid(), id, request.Sentiment,
                    Actor(user), clock.GetUtcNow(), request.Comment);
                store.Feedback.Add(feedback);
                await store.SaveChangesAsync(ct);
                return Results.Created($"/api/autopilot/findings/{id}/feedback/{feedback.Id}", feedback);
            }
            catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        });
    }

    // Every decision endpoint (review/dismiss/resolve/snooze/reopen) shares this shape: load,
    // apply the one domain transition the caller asked for, save, map the two invariant
    // violations the domain can throw to the standard 400/409 split
    // (.claude/rules/architecture.md) — ArgumentException is a bad request, InvalidOperationException
    // is a real conflict (the finding's current state refuses this transition).
    private static async Task<IResult> Decide(Guid id, AutopilotStore store, CancellationToken ct, Action<AutopilotFinding> transition)
    {
        var finding = await store.Findings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (finding is null) return Results.NotFound();
        try
        {
            transition(finding);
            await store.SaveChangesAsync(ct);
            return Results.Ok(finding);
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        catch (InvalidOperationException exception) { return Results.Problem(statusCode: 409, title: exception.Message); }
    }

    private static AutopilotFindingSummary ToSummary(AutopilotFinding finding, AutopilotEvidence evidence, bool isRead) => new(
        finding.Id, finding.SignalType, finding.Severity.ToString(), finding.SubjectType, finding.SubjectId,
        finding.Summary, finding.DetectedAt, finding.FreshnessAsOf, finding.PropertyId, finding.PortfolioId,
        finding.Status.ToString(), finding.SnoozedUntil, isRead,
        evidence.Inputs.Select(i => new CalculationInputResponse(i.Name, i.Value)).ToList(),
        evidence.SourceLinks.Select(l => new SourceLinkResponse(l.EntityType, l.EntityId)).ToList(),
        evidence.Impact is { } impact ? new ImpactResponse(impact.Category.ToString(), impact.Description, impact.EstimatedAmount) : null,
        evidence.Confidence);

    private static Guid Actor(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty ? id : throw new UnauthorizedAccessException();
}

public sealed record AutopilotRunRequest(string? Trigger);
public sealed record AutopilotRunResponse(Guid RunId, int FindingCount);
public sealed record DismissFindingRequest(string? Reason);
public sealed record SnoozeFindingRequest(DateTimeOffset Until);
public sealed record RecordFeedbackRequest(FeedbackSentiment Sentiment, string? Comment);

public sealed record CalculationInputResponse(string Name, string Value);
public sealed record SourceLinkResponse(string EntityType, Guid EntityId);
public sealed record ImpactResponse(string Category, string Description, decimal? EstimatedAmount);
public sealed record AutopilotFindingSummary(
    Guid Id, string SignalType, string Severity, string SubjectType, Guid SubjectId,
    string Summary, DateTimeOffset DetectedAt, DateTimeOffset FreshnessAsOf,
    Guid? PropertyId, Guid? PortfolioId, string Status, DateTimeOffset? SnoozedUntil, bool IsRead,
    IReadOnlyList<CalculationInputResponse> Inputs, IReadOnlyList<SourceLinkResponse> SourceLinks,
    ImpactResponse? Impact, double Confidence);
public sealed record AutopilotBriefPage(IReadOnlyList<AutopilotFindingSummary> Items, int TotalCount, int Page, int PageSize);
