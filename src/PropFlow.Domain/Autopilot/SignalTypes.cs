namespace PropFlow.Domain.Autopilot;

// The closed catalog CPM-8.01 left open (AutopilotFinding.SignalType is validated free text, not
// an enum, because this catalog did not exist yet — see AutopilotFinding.cs's own comment and
// docs/backlog.md's CPM-8.01 note). CPM-8.02 owns it: every analyzer in this namespace produces a
// SignalCandidate whose SignalType is one of these constants, never an ad hoc string.
//
// Four signal types are not new rules — they are the existing Attention queue's findings
// (PropFlow.Domain.Attention.AttentionRules), reused rather than reimplemented, the same way
// CPM-8.01 reused AttentionSeverity: SlaRisk/StalledWork/VendorFollowUp/TurnRisk map onto
// AttentionReason.SlaBreach+UnassignedEmergency+Overdue / WaitingOnVendor+WaitingOnResident /
// WaitingOnVendor / UnitTurnAtRisk respectively (see EfSignalCatalog's mapping). RepeatRepair
// reuses IRepeatRepairDetector directly, tenant-wide rather than only-on-open-work, which is
// deliberately broader than the Attention queue's own RepeatRepair reason (that one only fires
// when a repeat-repair asset also has open work referencing it — an Autopilot signal should catch
// the asset before the next work item opens on it, not after). The other six signal types
// (leasing, accounting, compliance, assets) are new deterministic rules this task adds.
public static class SignalTypes
{
    // Reused from the existing Attention queue.
    public const string SlaRisk = "SlaRisk";
    public const string StalledWork = "StalledWork";
    public const string VendorFollowUp = "VendorFollowUp";
    public const string TurnRisk = "TurnRisk";
    public const string RepeatRepair = "RepeatRepair";

    // New in CPM-8.02.
    public const string LeaseDeadline = "LeaseDeadline";
    public const string PaymentDeadline = "PaymentDeadline";
    public const string BudgetVariance = "BudgetVariance";
    public const string InvoiceException = "InvoiceException";
    public const string ComplianceDeadline = "ComplianceDeadline";
    public const string AssetReplacement = "AssetReplacement";

    public static readonly IReadOnlyList<string> All =
    [
        SlaRisk, StalledWork, VendorFollowUp, TurnRisk, RepeatRepair,
        LeaseDeadline, PaymentDeadline, BudgetVariance, InvoiceException, ComplianceDeadline, AssetReplacement,
    ];
}
