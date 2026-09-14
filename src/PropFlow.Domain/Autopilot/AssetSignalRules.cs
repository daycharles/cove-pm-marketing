using PropFlow.Domain.Assets;
using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

/// <summary>
/// Takes the real <see cref="Asset"/> entity, same reasoning as
/// <see cref="ComplianceSignalRules"/>: the replacement-due date math already lives on the
/// entity (<c>IsReplacementDue</c>/<c>AgeInYears</c>), so the rule reuses it rather than
/// re-deriving <c>InstalledOn + ExpectedServiceLifeYears</c> into a parallel snapshot.
///
/// The repeat-repair signal type (<see cref="SignalTypes.RepeatRepair"/>) is not evaluated
/// here even though it is asset-scoped — <c>IRepeatRepairDetector</c> already owns that
/// threshold decision (<c>RepairThreshold</c>/<c>WindowDays</c>), and it is an Application-layer
/// interface returning an Application-layer DTO, which Domain cannot reference. EfSignalCatalog
/// builds that candidate directly from <c>IRepeatRepairDetector</c>'s own result.
/// </summary>
public static class AssetSignalRules
{
    public static SignalCandidate? EvaluateAssetReplacement(Asset asset, DateOnly today, DateTimeOffset now)
    {
        if (!asset.IsReplacementDue(today)) return null;

        // IsReplacementDue only returns true when both InstalledOn and ExpectedServiceLifeYears
        // are set (ReplacementDueOn returns null otherwise), so ExpectedServiceLifeYears is
        // guaranteed non-null here.
        var summary = $"{asset.Name} is past its expected {asset.ExpectedServiceLifeYears}-year service life and due for replacement.";
        return new SignalCandidate(SignalTypes.AssetReplacement, AttentionSeverity.Warning, "Asset", asset.Id, summary, now, now);
    }
}
