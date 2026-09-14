using PropFlow.Application.Autopilot;
using PropFlow.Application.Work;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.Communications;
using PropFlow.Domain.Procurement;
using PropFlow.Domain.Approvals;
using PropFlow.Infrastructure.Persistence;

namespace PropFlow.Infrastructure.Autopilot;

// CPM-8.08. The one boundary layer that actually enforces the two AutopilotActionProposal fields
// this codebase's authorization/consent model has an existing primitive for
// (RequiredExecutionCapability against the actor's real claims, RequiredConsentType against
// Resident.AllowsContact). RequiredApprovalCapability, IdempotencyKey's dedup (a DB unique index,
// already added in this task's migration) and ConcurrencyToken (checked per-adapter, against
// whichever target entity actually carries a version - not a general engine) are covered
// elsewhere; see AutopilotActionProposal's own class comment for the complete map.
public sealed class EfGovernedActionExecutor(
    AutopilotStore autopilot,
    OperationsStore operations,
    IWorkOperations workOperations,
    TimeProvider clock) : IGovernedActionExecutor
{
    public async Task<GovernedActionResult> ExecuteAsync(
        AutopilotActionProposal proposal, Guid actorId, IReadOnlyCollection<string> actorCapabilities, CancellationToken cancellationToken)
    {
        if (!actorCapabilities.Contains(proposal.RequiredExecutionCapability))
            return new GovernedActionResult(GovernedActionOutcome.CapabilityDenied,
                $"Executing this action needs the '{proposal.RequiredExecutionCapability}' capability.");

        if (proposal.RequiredConsentType is { } consentType)
        {
            var consentCheck = await CheckConsentAsync(consentType, proposal.Payload, cancellationToken);
            if (consentCheck is { } denial) return new GovernedActionResult(GovernedActionOutcome.ConsentDenied, denial);
        }

        var now = clock.GetUtcNow();
        try
        {
            var outcome = proposal.ActionType switch
            {
                GovernedActionTypes.AssignVendor => await ExecuteAssignVendorAsync(proposal, actorId, cancellationToken),
                GovernedActionTypes.AssignEmployee => await ExecuteAssignEmployeeAsync(proposal, actorId, cancellationToken),
                GovernedActionTypes.ScheduleWork => await ExecuteScheduleWorkAsync(proposal, actorId, cancellationToken),
                GovernedActionTypes.CreateFollowUp => await ExecuteCreateFollowUpAsync(proposal, actorId, cancellationToken),
                GovernedActionTypes.DraftCommunication => ExecuteDraftCommunication(proposal),
                GovernedActionTypes.RequestApproval => await ExecuteRequestApprovalAsync(proposal, actorId, now, cancellationToken),
                GovernedActionTypes.CreatePurchaseOrderDraft => await ExecuteCreatePurchaseOrderDraftAsync(proposal, now, cancellationToken),
                _ => throw new InvalidOperationException($"No executor is registered for action type '{proposal.ActionType}'."),
            };

            if (outcome.Succeeded)
            {
                proposal.MarkExecuted(outcome.Detail, now);
                RecordAudit(ActionAuditEventTypes.Executed, proposal, actorId, now, outcome.Detail);
                await autopilot.SaveChangesAsync(cancellationToken);
                return new GovernedActionResult(GovernedActionOutcome.Executed, outcome.Detail);
            }

            proposal.MarkFailed(outcome.Detail, now);
            RecordAudit(ActionAuditEventTypes.Failed, proposal, actorId, now, outcome.Detail);
            await autopilot.SaveChangesAsync(cancellationToken);
            return new GovernedActionResult(GovernedActionOutcome.Failed, outcome.Detail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var detail = Truncate(ex.Message, AutopilotActionProposal.ExecutionOutcomeMaxLength);
            proposal.MarkFailed(detail, now);
            RecordAudit(ActionAuditEventTypes.Failed, proposal, actorId, now, detail);
            await autopilot.SaveChangesAsync(cancellationToken);
            return new GovernedActionResult(GovernedActionOutcome.Failed, detail);
        }
    }

    private sealed record AdapterOutcome(bool Succeeded, string Detail);
    private static AdapterOutcome Ok(string detail) => new(true, detail);
    private static AdapterOutcome Fail(string detail) => new(false, detail);

    private async Task<AdapterOutcome> ExecuteAssignVendorAsync(AutopilotActionProposal proposal, Guid actorId, CancellationToken ct)
    {
        var workId = RequireGuid(proposal.Payload, "WorkId");
        var vendorId = RequireGuid(proposal.Payload, "VendorId");
        var version = ParseVersion(proposal.ConcurrencyToken);
        var outcome = await workOperations.AssignVendorAsync(workId, vendorId, actorId, version, ct);
        return outcome is AssignmentOutcome.Updated or AssignmentOutcome.Unchanged
            ? Ok($"Vendor {vendorId} assigned to work item {workId}.")
            : Fail($"Vendor assignment did not apply: {outcome}.");
    }

    private async Task<AdapterOutcome> ExecuteAssignEmployeeAsync(AutopilotActionProposal proposal, Guid actorId, CancellationToken ct)
    {
        var workId = RequireGuid(proposal.Payload, "WorkId");
        var employeeId = RequireGuid(proposal.Payload, "EmployeeId");
        var version = ParseVersion(proposal.ConcurrencyToken);
        var outcome = await workOperations.AssignEmployeeAsync(workId, employeeId, actorId, version, ct);
        return outcome is AssignmentOutcome.Updated or AssignmentOutcome.Unchanged
            ? Ok($"Employee {employeeId} assigned to work item {workId}.")
            : Fail($"Employee assignment did not apply: {outcome}.");
    }

    // No dedicated "reschedule" operation on IWorkOperations - UpdateAsync is a full replace, so
    // this reads the current item and changes only ScheduledStart/ScheduledEnd, the same way a
    // human editing the work item's schedule field in the UI would leave everything else as is.
    private async Task<AdapterOutcome> ExecuteScheduleWorkAsync(AutopilotActionProposal proposal, Guid actorId, CancellationToken ct)
    {
        var workId = RequireGuid(proposal.Payload, "WorkId");
        var start = RequireDate(proposal.Payload, "ScheduledStart");
        var end = RequireDate(proposal.Payload, "ScheduledEnd");
        var version = ParseVersion(proposal.ConcurrencyToken) ?? 0;

        var work = await workOperations.GetAsync(workId, ct);
        if (work is null) return Fail($"Work item {workId} was not found.");

        var command = new UpdateWorkCommand(
            work.Title, work.Description, work.CategoryId, work.Priority, work.PropertyId, work.BuildingId,
            work.SpaceId, work.ResidentId, work.AssetId, work.DueDate, work.Cost, work.InternalNotes,
            work.ResidentVisibleNotes, work.Status, start, end, version, actorId);
        var outcome = await workOperations.UpdateAsync(workId, command, ct);
        return outcome == WorkWriteOutcome.Updated
            ? Ok($"Work item {workId} scheduled {start:u} to {end:u}.")
            : Fail($"Scheduling did not apply: {outcome}.");
    }

    private async Task<AdapterOutcome> ExecuteCreateFollowUpAsync(AutopilotActionProposal proposal, Guid actorId, CancellationToken ct)
    {
        var propertyId = RequireGuid(proposal.Payload, "PropertyId");
        var title = RequireText(proposal.Payload, "Title");
        var description = proposal.Payload["Description"];
        var dueDate = OptionalDate(proposal.Payload, "DueDate");
        var created = await workOperations.CreateAsync(
            new CreateWorkCommand(title, propertyId, actorId, description, DueDate: dueDate), ct);
        return Ok($"Follow-up work item {created.Id} created.");
    }

    // Never calls the Communications outbox - see GovernedActionTypes.DraftCommunication's own
    // comment on why this action type stops at a reviewable draft.
    private static AdapterOutcome ExecuteDraftCommunication(AutopilotActionProposal proposal)
    {
        var recipientId = RequireGuid(proposal.Payload, "RecipientId");
        var draftText = RequireText(proposal.Payload, "DraftText");
        return Ok($"Draft prepared for {recipientId}, held for manual review and send: \"{draftText}\"");
    }

    private async Task<AdapterOutcome> ExecuteRequestApprovalAsync(AutopilotActionProposal proposal, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        var subjectType = RequireText(proposal.Payload, "SubjectType");
        var subjectId = RequireGuid(proposal.Payload, "SubjectId");
        var note = proposal.Payload["Note"];
        var request = ApprovalRequest.Create(autopilot.OrganizationId, Guid.NewGuid(), subjectType, subjectId, actorId, note, now);
        operations.ApprovalRequests.Add(request);
        await operations.SaveChangesAsync(ct);
        return Ok($"Approval requested for {subjectType} {subjectId} (request {request.Id}).");
    }

    private async Task<AdapterOutcome> ExecuteCreatePurchaseOrderDraftAsync(AutopilotActionProposal proposal, DateTimeOffset now, CancellationToken ct)
    {
        var vendorId = RequireGuid(proposal.Payload, "VendorId");
        var number = RequireText(proposal.Payload, "Number");
        var amount = RequireDecimal(proposal.Payload, "Amount");
        var approvalThreshold = RequireDecimal(proposal.Payload, "ApprovalThreshold");
        var propertyId = OptionalGuid(proposal.Payload, "PropertyId");
        var workItemId = OptionalGuid(proposal.Payload, "WorkItemId");
        var order = new PurchaseOrder(autopilot.OrganizationId, Guid.NewGuid(), vendorId, propertyId, workItemId, number, amount, approvalThreshold, now);
        operations.PurchaseOrders.Add(order);
        await operations.SaveChangesAsync(ct);
        return Ok($"Purchase order draft {order.Id} ({number}) created for vendor {vendorId}.");
    }

    private async Task<string?> CheckConsentAsync(string consentType, ActionPayload payload, CancellationToken ct)
    {
        var channel = consentType switch
        {
            "ResidentSms" => MessageChannel.Sms,
            "ResidentEmail" => MessageChannel.Email,
            _ => (MessageChannel?)null,
        };
        if (channel is null) return null; // An unrecognized consent type is a shape this executor doesn't know how to check yet - fail open on the check, not the whole action, since the adapter itself still runs.

        var residentId = RequireGuid(payload, "RecipientId");
        var resident = await operations.Residents.FindAsync([operations.OrganizationId, residentId], ct);
        if (resident is null) return $"Resident {residentId} was not found.";
        return resident.AllowsContact(channel.Value)
            ? null
            : $"Resident {residentId} has not granted {channel} consent.";
    }

    private void RecordAudit(string eventType, AutopilotActionProposal proposal, Guid actorId, DateTimeOffset at, string detail) =>
        autopilot.AuditEntries.Add(AutopilotAuditEntry.Record(
            autopilot.OrganizationId, Guid.NewGuid(), eventType, "AutopilotActionProposal", proposal.Id, actorId, at,
            Truncate(detail, AutopilotAuditEntry.DetailMaxLength)));

    private static Guid RequireGuid(ActionPayload payload, string field) =>
        Guid.TryParse(payload[field], out var value) && value != Guid.Empty
            ? value : throw new InvalidOperationException($"Payload field '{field}' must be a non-empty id.");

    private static Guid? OptionalGuid(ActionPayload payload, string field) =>
        Guid.TryParse(payload[field], out var value) && value != Guid.Empty ? value : null;

    private static string RequireText(ActionPayload payload, string field) =>
        payload[field] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Payload field '{field}' is required.");

    private static DateTimeOffset RequireDate(ActionPayload payload, string field) =>
        DateTimeOffset.TryParse(payload[field], out var value)
            ? value : throw new InvalidOperationException($"Payload field '{field}' must be a valid date/time.");

    private static DateTimeOffset? OptionalDate(ActionPayload payload, string field) =>
        DateTimeOffset.TryParse(payload[field], out var value) ? value : null;

    private static decimal RequireDecimal(ActionPayload payload, string field) =>
        decimal.TryParse(payload[field], out var value) ? value : throw new InvalidOperationException($"Payload field '{field}' must be a number.");

    private static uint? ParseVersion(string? concurrencyToken) => uint.TryParse(concurrencyToken, out var value) ? value : null;

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
