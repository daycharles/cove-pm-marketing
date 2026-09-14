using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropFlow.Application;
using PropFlow.Application.Autopilot;
using PropFlow.Application.Work;
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

        // CPM-8.08. Recommendations get the same minimal, mechanical persistence ActionProposal
        // itself has — no generation intelligence (a manager writes Description by hand, or a
        // future task automates it); this task only unblocks the real dependency
        // AutopilotActionProposal has always had on an approved AutopilotRecommendation existing.
        group.MapPost("/findings/{id:guid}/recommendations", async (Guid id, CreateRecommendationRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var findingExists = await store.Findings.AnyAsync(x => x.Id == id, ct);
            if (!findingExists) return Results.NotFound();
            try
            {
                var recommendation = new AutopilotRecommendation(store.OrganizationId, Guid.NewGuid(), id,
                    request.Description, Actor(user), clock.GetUtcNow());
                store.Recommendations.Add(recommendation);
                await store.SaveChangesAsync(ct);
                return Results.Created($"/api/autopilot/recommendations/{recommendation.Id}", ToRecommendationResponse(recommendation));
            }
            catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        });

        group.MapGet("/findings/{id:guid}/recommendations", async (Guid id, AutopilotStore store, CancellationToken ct) =>
            Results.Ok(await store.Recommendations.AsNoTracking().Where(x => x.FindingId == id)
                .OrderByDescending(x => x.ProposedAt).Select(x => ToRecommendationResponse(x)).ToListAsync(ct)));

        group.MapPost("/recommendations/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await DecideRecommendation(id, store, ct, r => r.Approve(Actor(user), clock.GetUtcNow())));

        group.MapPost("/recommendations/{id:guid}/reject", async (Guid id, RejectRecommendationRequest? request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await DecideRecommendation(id, store, ct, r => r.Reject(Actor(user), clock.GetUtcNow(), request?.Reason)));

        // One route per action type rather than one generic "propose an action" route with a free
        // payload: each adapter's inputs are genuinely different (a vendor id, a schedule window,
        // a draft's channel and text), and a generic route would just move that same per-type
        // parsing into a body no OpenAPI/request-binding gets to validate. Every route shares the
        // same shape: load the Approved recommendation, read whatever server-side state the
        // preview/concurrency-token needs, build the typed payload and preview, persist the
        // proposal Proposed. Execution — the real subsystem mutation — happens only later, at
        // POST .../actions/{id}/execute, never here.
        group.MapPost("/recommendations/{id:guid}/actions/assign-vendor", async (Guid id, AssignVendorActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, IWorkOperations workOperations, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var work = await workOperations.GetAsync(request.WorkId, ct);
            if (work is null) return Results.Problem(statusCode: 400, title: $"Work item {request.WorkId} was not found.");
            var version = await workOperations.VersionAsync(request.WorkId, ct);
            var payload = new ActionPayload([new ActionField("WorkId", request.WorkId.ToString()), new ActionField("VendorId", request.VendorId.ToString())]);
            var preview = new ActionPreview($"Assign vendor {request.VendorId} to work item {request.WorkId}.",
                [new ActionFieldChange("VendorId", work.VendorId?.ToString(), request.VendorId.ToString())]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.AssignVendor, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.AssignVendor,
                concurrencyToken: version?.ToString(), ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/assign-employee", async (Guid id, AssignEmployeeActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, IWorkOperations workOperations, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var work = await workOperations.GetAsync(request.WorkId, ct);
            if (work is null) return Results.Problem(statusCode: 400, title: $"Work item {request.WorkId} was not found.");
            var version = await workOperations.VersionAsync(request.WorkId, ct);
            var payload = new ActionPayload([new ActionField("WorkId", request.WorkId.ToString()), new ActionField("EmployeeId", request.EmployeeId.ToString())]);
            var preview = new ActionPreview($"Assign employee {request.EmployeeId} to work item {request.WorkId}.",
                [new ActionFieldChange("EmployeeId", work.EmployeeId?.ToString(), request.EmployeeId.ToString())]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.AssignEmployee, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.AssignEmployee,
                concurrencyToken: version?.ToString(), ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/schedule-work", async (Guid id, ScheduleWorkActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, IWorkOperations workOperations, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            if (request.ScheduledEnd <= request.ScheduledStart)
                return Results.Problem(statusCode: 400, title: "ScheduledEnd must be after ScheduledStart.");
            var work = await workOperations.GetAsync(request.WorkId, ct);
            if (work is null) return Results.Problem(statusCode: 400, title: $"Work item {request.WorkId} was not found.");
            var version = await workOperations.VersionAsync(request.WorkId, ct);
            var payload = new ActionPayload([
                new ActionField("WorkId", request.WorkId.ToString()),
                new ActionField("ScheduledStart", request.ScheduledStart.ToString("O")),
                new ActionField("ScheduledEnd", request.ScheduledEnd.ToString("O")),
            ]);
            var preview = new ActionPreview($"Schedule work item {request.WorkId} for {request.ScheduledStart:u} to {request.ScheduledEnd:u}.",
                [
                    new ActionFieldChange("ScheduledStart", work.ScheduledStart?.ToString("O"), request.ScheduledStart.ToString("O")),
                    new ActionFieldChange("ScheduledEnd", work.ScheduledEnd?.ToString("O"), request.ScheduledEnd.ToString("O")),
                ]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.ScheduleWork, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.UpdateWork,
                concurrencyToken: version?.ToString(), ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/follow-up", async (Guid id, CreateFollowUpActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var fields = new List<ActionField> { new("PropertyId", request.PropertyId.ToString()), new("Title", request.Title) };
            if (!string.IsNullOrWhiteSpace(request.Description)) fields.Add(new ActionField("Description", request.Description));
            if (request.DueDate is { } due) fields.Add(new ActionField("DueDate", due.ToString("O")));
            var payload = new ActionPayload(fields);
            var preview = new ActionPreview($"Create follow-up work item \"{request.Title}\" on property {request.PropertyId}.",
                [new ActionFieldChange("Title", Before: null, After: request.Title)]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.CreateFollowUp, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.CreateWork, ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/communication-draft", async (Guid id, CommunicationDraftActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var payload = new ActionPayload([
                new ActionField("RecipientType", request.RecipientType),
                new ActionField("RecipientId", request.RecipientId.ToString()),
                new ActionField("Channel", request.Channel),
                new ActionField("DraftText", request.DraftText),
            ]);
            var preview = new ActionPreview($"Draft {request.Channel} message to {request.RecipientType} {request.RecipientId}.",
                [new ActionFieldChange("DraftText", Before: null, After: request.DraftText)]);
            // Only a resident recipient has a consent model in this codebase today
            // (Resident.AllowsContact) - a vendor recipient carries no consent gate to check.
            var consentType = request.RecipientType == "Resident"
                ? (request.Channel == "Sms" ? "ResidentSms" : "ResidentEmail")
                : null;
            return await CreateProposal(store, recommendation!, GovernedActionTypes.DraftCommunication, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.SendResidentMessage,
                requiredConsentType: consentType, ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/request-approval", async (Guid id, RequestApprovalActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var fields = new List<ActionField> { new("SubjectType", request.SubjectType), new("SubjectId", request.SubjectId.ToString()) };
            if (!string.IsNullOrWhiteSpace(request.Note)) fields.Add(new ActionField("Note", request.Note));
            var payload = new ActionPayload(fields);
            var preview = new ActionPreview($"Request approval for {request.SubjectType} {request.SubjectId}.",
                [new ActionFieldChange("ApprovalRequested", Before: "false", After: "true")]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.RequestApproval, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.ManageAutopilot, ct: ct);
        });

        group.MapPost("/recommendations/{id:guid}/actions/purchase-order-draft", async (Guid id, PurchaseOrderDraftActionRequest request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
        {
            var (recommendation, recommendationError) = await RequireApprovedRecommendation(id, store, ct);
            if (recommendationError is not null) return recommendationError;
            var fields = new List<ActionField>
            {
                new("VendorId", request.VendorId.ToString()), new("Number", request.Number),
                new("Amount", request.Amount.ToString("F2")), new("ApprovalThreshold", request.ApprovalThreshold.ToString("F2")),
            };
            if (request.PropertyId is { } propertyId) fields.Add(new ActionField("PropertyId", propertyId.ToString()));
            if (request.WorkItemId is { } workItemId) fields.Add(new ActionField("WorkItemId", workItemId.ToString()));
            var payload = new ActionPayload(fields);
            var preview = new ActionPreview($"Draft purchase order {request.Number} for vendor {request.VendorId} ({request.Amount:C}).",
                [new ActionFieldChange("PurchaseOrder", Before: null, After: request.Number)]);
            return await CreateProposal(store, recommendation!, GovernedActionTypes.CreatePurchaseOrderDraft, preview.Description,
                Actor(user), clock.GetUtcNow(), payload, preview, Capabilities.ManageAutopilot, Capabilities.ManageProcurement, ct: ct);
        });

        group.MapGet("/recommendations/{id:guid}/actions", async (Guid id, AutopilotStore store, CancellationToken ct) =>
            Results.Ok(await store.ActionProposals.AsNoTracking().Where(x => x.RecommendationId == id)
                .OrderByDescending(x => x.ProposedAt).Select(x => ToActionProposalResponse(x)).ToListAsync(ct)));

        group.MapPost("/actions/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await DecideAction(id, store, ct, p => p.Approve(Actor(user), clock.GetUtcNow())));

        group.MapPost("/actions/{id:guid}/reject", async (Guid id, RejectActionRequest? request,
            ClaimsPrincipal user, AutopilotStore store, TimeProvider clock, CancellationToken ct) =>
            await DecideAction(id, store, ct, p => p.Reject(Actor(user), clock.GetUtcNow(), request?.Reason)));

        group.MapPost("/actions/{id:guid}/execute", async (Guid id, ClaimsPrincipal user, AutopilotStore store,
            IGovernedActionExecutor executor, CancellationToken ct) =>
        {
            var proposal = await store.ActionProposals.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (proposal is null) return Results.NotFound();
            if (proposal.Status != ActionProposalStatus.Approved)
                return Results.Problem(statusCode: 409, title: "Only an approved action can be executed.");
            var capabilities = user.FindAll(TenantAccess.CapabilityClaim).Select(c => c.Value).ToHashSet();
            var result = await executor.ExecuteAsync(proposal, Actor(user), capabilities, ct);
            return result.Outcome switch
            {
                GovernedActionOutcome.Executed => Results.Ok(ToActionProposalResponse(proposal)),
                GovernedActionOutcome.CapabilityDenied => Results.Problem(statusCode: 403, title: result.Detail),
                GovernedActionOutcome.ConsentDenied => Results.Problem(statusCode: 409, title: result.Detail),
                _ => Results.Ok(ToActionProposalResponse(proposal)), // Failed — the outcome is on the proposal itself, not a 5xx: the executor ran, the adapter refused.
            };
        });

        // CPM-8.10: read-only Ask CovePM. Answered/Unavailable both come back 200 (Unavailable is
        // a normal, fully-supported outcome per IModelGateway's own charter — see
        // EfAskCovePMService's class comment), Refused comes back 422 (a well-formed request the
        // service declined to act on, distinct from a malformed one).
        group.MapPost("/ask", async (AskCovePMHttpRequest request, IAskCovePMService askCovePM, CancellationToken ct) =>
        {
            var result = await askCovePM.AskAsync(request.ToCommand(), ct);
            return result.Outcome switch
            {
                AskCovePMOutcome.Answered => Results.Ok(ToAskResponse(result)),
                AskCovePMOutcome.Unavailable => Results.Ok(ToAskResponse(result)),
                _ => Results.Problem(statusCode: 422, title: result.RefusalReason.ToString()),
            };
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

    // Same shape as Decide, for AutopilotRecommendation.
    private static async Task<IResult> DecideRecommendation(Guid id, AutopilotStore store, CancellationToken ct, Action<AutopilotRecommendation> transition)
    {
        var recommendation = await store.Recommendations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (recommendation is null) return Results.NotFound();
        try
        {
            transition(recommendation);
            await store.SaveChangesAsync(ct);
            return Results.Ok(ToRecommendationResponse(recommendation));
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        catch (InvalidOperationException exception) { return Results.Problem(statusCode: 409, title: exception.Message); }
    }

    // Same shape as Decide, for AutopilotActionProposal.
    private static async Task<IResult> DecideAction(Guid id, AutopilotStore store, CancellationToken ct, Action<AutopilotActionProposal> transition)
    {
        var proposal = await store.ActionProposals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (proposal is null) return Results.NotFound();
        try
        {
            transition(proposal);
            await store.SaveChangesAsync(ct);
            return Results.Ok(ToActionProposalResponse(proposal));
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        catch (InvalidOperationException exception) { return Results.Problem(statusCode: 409, title: exception.Message); }
    }

    // Every "propose an action" route needs the same two things first: the recommendation exists,
    // and it is Approved (an action cannot be proposed from a recommendation nobody has signed
    // off on yet — that is the whole point of the two-step Recommendation → ActionProposal chain
    // AutopilotActionProposal's own class comment describes). Returns the recommendation with a
    // null error on success, or a null recommendation with the IResult to return on failure.
    private static async Task<(AutopilotRecommendation? Recommendation, IResult? Error)> RequireApprovedRecommendation(
        Guid id, AutopilotStore store, CancellationToken ct)
    {
        var recommendation = await store.Recommendations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (recommendation is null) return (null, Results.NotFound());
        if (recommendation.Status != RecommendationStatus.Approved)
            return (null, Results.Problem(statusCode: 409, title: "Only an approved recommendation can propose an action."));
        return (recommendation, null);
    }

    private static async Task<IResult> CreateProposal(
        AutopilotStore store, AutopilotRecommendation recommendation, string actionType, string payloadSummary,
        Guid actor, DateTimeOffset now, ActionPayload payload, ActionPreview preview,
        string requiredApprovalCapability, string requiredExecutionCapability,
        string? requiredConsentType = null, string? concurrencyToken = null, CancellationToken ct = default)
    {
        try
        {
            // Deterministic, not client-supplied: at most one proposal of a given action type per
            // recommendation, so a retried POST (a network timeout resubmit, a double click)
            // collides with the first attempt instead of creating a duplicate — the dedup
            // guarantee AutopilotActionProposal.IdempotencyKey's own comment promises, given the
            // simplest input this task actually has to derive one from.
            var idempotencyKey = $"{recommendation.Id}:{actionType}";
            var proposal = new AutopilotActionProposal(store.OrganizationId, Guid.NewGuid(), recommendation.Id,
                actionType, payloadSummary, actor, now, payload, preview,
                requiredApprovalCapability, requiredExecutionCapability, idempotencyKey,
                requiredConsentType, concurrencyToken);
            store.ActionProposals.Add(proposal);
            await store.SaveChangesAsync(ct);
            return Results.Created($"/api/autopilot/actions/{proposal.Id}", ToActionProposalResponse(proposal));
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
        catch (DbUpdateException) { return Results.Problem(statusCode: 409, title: "An action of this type has already been proposed for this recommendation."); }
    }

    private static RecommendationResponse ToRecommendationResponse(AutopilotRecommendation recommendation) => new(
        recommendation.Id, recommendation.FindingId, recommendation.Description, recommendation.Status.ToString(),
        recommendation.ProposedAt, recommendation.DecidedAt, recommendation.DecisionReason);

    private static ActionProposalResponse ToActionProposalResponse(AutopilotActionProposal proposal) => new(
        proposal.Id, proposal.RecommendationId, proposal.ActionType, proposal.PayloadSummary, proposal.Status.ToString(),
        proposal.Payload.Fields.Select(f => new ActionFieldResponse(f.Name, f.Value)).ToList(),
        proposal.Preview.Description,
        proposal.Preview.Changes.Select(c => new ActionFieldChangeResponse(c.Field, c.Before, c.After)).ToList(),
        proposal.ProposedAt, proposal.DecidedAt, proposal.ExecutedAt, proposal.ExecutionOutcome);

    private static AskCovePMResponse ToAskResponse(AskCovePMResult result) => new(
        result.Outcome.ToString(),
        result.Answer is { } answer
            ? new AskCovePMAnswerResponse(answer.Text, answer.Sources.Select(s => new AskCovePMSourceResponse(s.FindingId, s.SignalType, s.Summary)).ToList())
            : null,
        result.RefusalReason?.ToString(),
        result.FailureReason);

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

// CPM-8.08.
public sealed record CreateRecommendationRequest(string Description);
public sealed record RejectRecommendationRequest(string? Reason);
public sealed record RecommendationResponse(
    Guid Id, Guid FindingId, string Description, string Status,
    DateTimeOffset ProposedAt, DateTimeOffset? DecidedAt, string? DecisionReason);

public sealed record RejectActionRequest(string? Reason);
public sealed record ActionFieldResponse(string Name, string Value);
public sealed record ActionFieldChangeResponse(string Field, string? Before, string After);
public sealed record ActionProposalResponse(
    Guid Id, Guid RecommendationId, string ActionType, string PayloadSummary, string Status,
    IReadOnlyList<ActionFieldResponse> Payload, string PreviewDescription, IReadOnlyList<ActionFieldChangeResponse> PreviewChanges,
    DateTimeOffset ProposedAt, DateTimeOffset? DecidedAt, DateTimeOffset? ExecutedAt, string? ExecutionOutcome);

public sealed record AssignVendorActionRequest(Guid WorkId, Guid VendorId);
public sealed record AssignEmployeeActionRequest(Guid WorkId, Guid EmployeeId);
public sealed record ScheduleWorkActionRequest(Guid WorkId, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd);
public sealed record CreateFollowUpActionRequest(Guid PropertyId, string Title, string? Description, DateTimeOffset? DueDate);
// RecipientType is "Resident" or "Vendor"; Channel is "Sms" or "Email" — free text, not an enum,
// the same call every other Autopilot free-text field this epic makes for a vocabulary this one
// task does not need to close.
public sealed record CommunicationDraftActionRequest(string RecipientType, Guid RecipientId, string Channel, string DraftText);
public sealed record RequestApprovalActionRequest(string SubjectType, Guid SubjectId, string? Note);
public sealed record PurchaseOrderDraftActionRequest(
    Guid VendorId, Guid? PropertyId, Guid? WorkItemId, string Number, decimal Amount, decimal ApprovalThreshold);

// CPM-8.10.
public sealed record AskCovePMHttpRequest(string Question, Guid? PropertyId, Guid? PortfolioId)
{
    public AskCovePMRequest ToCommand() => new(Question, PropertyId, PortfolioId);
}
public sealed record AskCovePMSourceResponse(Guid FindingId, string SignalType, string Summary);
public sealed record AskCovePMAnswerResponse(string Text, IReadOnlyList<AskCovePMSourceResponse> Sources);
public sealed record AskCovePMResponse(string Outcome, AskCovePMAnswerResponse? Answer, string? RefusalReason, string? FailureReason);
