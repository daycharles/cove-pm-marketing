using PropFlow.Domain.Assets;
using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AssetSignalRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 14);
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    private static Asset Asset(DateOnly? installedOn, int? expectedServiceLifeYears)
    {
        var asset = new PropFlow.Domain.Assets.Asset(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, AssetKind.Hvac, "Rooftop HVAC unit 3");
        asset.SetLifecycle(installedOn, null, expectedServiceLifeYears);
        return asset;
    }

    [Fact]
    public void An_asset_with_no_lifecycle_data_produces_no_signal()
    {
        Assert.Null(AssetSignalRules.EvaluateAssetReplacement(Asset(null, null), Today, Now));
    }

    [Fact]
    public void An_asset_well_within_its_service_life_produces_no_signal()
    {
        var asset = Asset(Today.AddYears(-5), expectedServiceLifeYears: 15);
        Assert.Null(AssetSignalRules.EvaluateAssetReplacement(asset, Today, Now));
    }

    [Fact]
    public void An_asset_past_its_expected_service_life_is_a_warning()
    {
        var asset = Asset(Today.AddYears(-16), expectedServiceLifeYears: 15);
        var candidate = AssetSignalRules.EvaluateAssetReplacement(asset, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(SignalTypes.AssetReplacement, candidate.SignalType);
        Assert.Equal(AttentionSeverity.Warning, candidate.Severity);
        Assert.Equal("Asset", candidate.SubjectType);
        Assert.Equal(asset.Id, candidate.SubjectId);
        Assert.Contains("Rooftop HVAC unit 3", candidate.Summary);
        Assert.Contains("15-year", candidate.Summary);
        // No ReplacementCostEstimate was set on this asset — the impact stays Operational with
        // no fabricated dollar amount, not a guessed one.
        Assert.Equal(ImpactCategory.Operational, candidate.Evidence.Impact?.Category);
        Assert.Null(candidate.Evidence.Impact?.EstimatedAmount);
    }

    [Fact]
    public void An_asset_with_a_recorded_replacement_cost_carries_a_financial_impact()
    {
        var asset = Asset(Today.AddYears(-16), expectedServiceLifeYears: 15);
        asset.SetReplacementCost(4200m);
        var candidate = AssetSignalRules.EvaluateAssetReplacement(asset, Today, Now);
        Assert.NotNull(candidate);
        Assert.Equal(ImpactCategory.Financial, candidate.Evidence.Impact?.Category);
        Assert.Equal(4200m, candidate.Evidence.Impact?.EstimatedAmount);
    }

    [Fact]
    public void An_asset_exactly_at_its_service_life_boundary_is_due()
    {
        var asset = Asset(Today.AddYears(-15), expectedServiceLifeYears: 15);
        Assert.NotNull(AssetSignalRules.EvaluateAssetReplacement(asset, Today, Now));
    }
}
