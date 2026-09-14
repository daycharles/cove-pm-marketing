namespace PropFlow.Domain.Autopilot;

// CPM-8.08. AutopilotActionProposal.ActionType stayed validated free text through CPM-8.01/8.07
// on purpose - "the closed adapter catalog CPM-8.08 will define... does not exist yet" (that
// entity's own comment). This is that catalog: the seven adapters CPM-8.08 ships
// (docs/backlog.md's "assign vendor/employee, schedule work, create follow-up, draft resident/
// vendor communication, request approval, and create purchase-order draft" — six backlog items,
// the first bundling two action types). No autonomous payment, lease, bank, role, or audit
// mutation is in this list, matching that same backlog line's own boundary.
public static class GovernedActionTypes
{
    public const string AssignVendor = "AssignVendor";
    public const string AssignEmployee = "AssignEmployee";
    public const string ScheduleWork = "ScheduleWork";
    public const string CreateFollowUp = "CreateFollowUp";
    // Drafts only - execution records the draft text as the outcome and never calls the
    // Communications outbox. Sending remains a human action through the existing communications
    // surface; an autonomous action framework auto-sending resident/vendor messages is exactly
    // the kind of consequence M8's safety charter asks this epic to avoid until autonomy is
    // explicitly enabled (CPM-8.11), which this task does not implement.
    public const string DraftCommunication = "DraftCommunication";
    // Creates an ApprovalRequest (FS-S03) rather than mutating the subject directly - "request
    // approval" is itself a request for a human decision, so the honest execution of this action
    // type is "a request now exists", not any change to whatever needs approving.
    public const string RequestApproval = "RequestApproval";
    // Creates a PurchaseOrder in its default Draft status (PurchaseOrderStatus.Draft) - approval,
    // issuance and invoicing stay separate, human-decided steps this action type does not reach.
    public const string CreatePurchaseOrderDraft = "CreatePurchaseOrderDraft";

    public static readonly IReadOnlyList<string> All =
    [
        AssignVendor, AssignEmployee, ScheduleWork, CreateFollowUp,
        DraftCommunication, RequestApproval, CreatePurchaseOrderDraft,
    ];
}
