using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Adapts the existing Attention queue's findings into Autopilot signal candidates rather than
/// re-implementing SLA/stalled-work/vendor-follow-up/turn-risk rules a second time — the same
/// "share vocabulary, don't duplicate" call CPM-8.01 already made reusing
/// <see cref="AttentionSeverity"/>. <c>EfSignalCatalog</c> calls <see cref="MapSignalType"/> for
/// every finding <c>IAttentionQueue.BuildAsync</c> returns.
///
/// <see cref="AttentionReason.RepeatRepair"/> maps to <c>null</c> (no candidate) on purpose:
/// Attention's version only fires when a repeat-repair asset also has open work referencing it,
/// narrower than the tenant-wide sweep <c>IRepeatRepairDetector.RepeatRepairAssetIdsAsync</c>
/// gives <c>EfSignalCatalog</c> directly — using both would double-report the same asset under
/// two different subjects (the work item and the asset) for the same underlying condition.
/// </summary>
public static class WorkSignalRules
{
    public static string? MapSignalType(AttentionReason reason) => reason switch
    {
        AttentionReason.UnassignedEmergency or AttentionReason.SlaBreach or AttentionReason.Overdue => SignalTypes.SlaRisk,
        AttentionReason.WaitingOnVendor => SignalTypes.VendorFollowUp,
        AttentionReason.WaitingOnResident => SignalTypes.StalledWork,
        AttentionReason.UnitTurnAtRisk => SignalTypes.TurnRisk,
        AttentionReason.RepeatRepair => null,
        _ => null,
    };
}
