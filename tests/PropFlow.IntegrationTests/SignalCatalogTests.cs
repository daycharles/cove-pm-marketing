using PropFlow.Domain.Assets;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.Leasing;
using PropFlow.Domain.Properties;
using PropFlow.Infrastructure.Persistence;
using Xunit;

namespace PropFlow.IntegrationTests;

// CPM-8.02/CPM-8.03 have no HTTP endpoint yet (that lands with CPM-8.05's daily brief API), so
// these exercise EfSignalCatalog directly against the runtime-role store, the same connection
// the API would use, rather than through s.Client. IAttentionQueue's own tenant scoping and rule
// correctness are already covered end to end by AttentionQueueTests; these focus on the four
// analyzers CPM-8.02 adds, on their CPM-8.03 evidence, and on tenant isolation for the assembly
// as a whole.
[Collection("PostgreSQL")]
public sealed class SignalCatalogTests(DatabaseFixture fixture)
{
    private static EfSignalCatalog CatalogFor(Scenario s, Guid organization)
    {
        var store = s.Store(organization);
        var repeatRepair = new EfRepeatRepairDetector(store, TimeProvider.System);
        var attentionQueue = new EfAttentionQueue(store, repeatRepair, TimeProvider.System);
        return new EfSignalCatalog(store, attentionQueue, repeatRepair, TimeProvider.System);
    }

    [Fact]
    public async Task An_overdue_lease_charge_produces_a_payment_deadline_finding_with_evidence()
    {
        await using var s = await fixture.CreateScenarioAsync();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        await using (var store = s.AdminStore(s.OrganizationA))
        {
            var lease = new Lease(s.OrganizationA, Guid.NewGuid(), s.ResidentA, s.SpaceA,
                today.AddMonths(-6), today.AddMonths(6), 1500m, null);
            lease.Activate();
            store.Leases.Add(lease);
            store.LeaseCharges.Add(new LeaseCharge(s.OrganizationA, Guid.NewGuid(), lease.Id, LeaseChargeType.Recurring,
                "September rent", 1500m, today.AddDays(-10)));
            await store.SaveChangesAsync();
        }

        var runId = Guid.NewGuid();
        var results = await CatalogFor(s, s.OrganizationA).EvaluateAsync(runId, CancellationToken.None);

        var result = Assert.Single(results, f => f.Finding.SignalType == SignalTypes.PaymentDeadline);
        Assert.Equal(runId, result.Finding.RunId);
        Assert.Equal(s.OrganizationA, result.Finding.OrganizationId);
        Assert.Equal("LeaseCharge", result.Finding.SubjectType);
        Assert.Contains("$1,500.00", result.Finding.Summary);

        Assert.Equal(result.Finding.Id, result.Evidence.FindingId);
        Assert.Equal(s.OrganizationA, result.Evidence.OrganizationId);
        Assert.Contains(result.Evidence.Inputs, i => i.Name == "Outstanding" && i.Value == "$1,500.00");
        Assert.Contains(result.Evidence.SourceLinks, l => l.EntityType == "LeaseCharge");
        Assert.NotNull(result.Evidence.Impact);
        Assert.Equal(1500m, result.Evidence.Impact.EstimatedAmount);
        Assert.Equal(1.0, result.Evidence.Confidence);
    }

    [Fact]
    public async Task An_escalated_compliance_obligation_produces_a_critical_finding_with_no_fabricated_dollar_impact()
    {
        await using var s = await fixture.CreateScenarioAsync();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        await using (var store = s.AdminStore(s.OrganizationA))
        {
            store.ComplianceObligations.Add(new ComplianceObligation(s.OrganizationA, Guid.NewGuid(), s.PropertyA,
                "Fire extinguisher inspection", today.AddDays(-30), escalationDays: 7));
            await store.SaveChangesAsync();
        }

        var results = await CatalogFor(s, s.OrganizationA).EvaluateAsync(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.Single(results, f => f.Finding.SignalType == SignalTypes.ComplianceDeadline);
        Assert.Equal(PropFlow.Domain.Attention.AttentionSeverity.Critical, result.Finding.Severity);
        Assert.Equal("ComplianceObligation", result.Finding.SubjectType);

        // Compliance risk has no honest dollar figure attached to it — "never fabricates missing
        // values" means the impact stays Operational with no EstimatedAmount, not a guessed one.
        Assert.NotNull(result.Evidence.Impact);
        Assert.Null(result.Evidence.Impact.EstimatedAmount);
    }

    [Fact]
    public async Task An_asset_past_its_service_life_produces_an_asset_replacement_finding()
    {
        await using var s = await fixture.CreateScenarioAsync();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        var oldAssetId = Guid.NewGuid();
        await using (var store = s.AdminStore(s.OrganizationA))
        {
            var oldAsset = new Asset(s.OrganizationA, oldAssetId, s.PropertyA, s.SpaceA, AssetKind.WaterHeater, "Aging water heater");
            oldAsset.SetLifecycle(today.AddYears(-15), null, expectedServiceLifeYears: 10);
            store.Assets.Add(oldAsset);
            await store.SaveChangesAsync();
        }

        var results = await CatalogFor(s, s.OrganizationA).EvaluateAsync(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.Single(results, f => f.Finding.SignalType == SignalTypes.AssetReplacement && f.Finding.SubjectId == oldAssetId);
        Assert.Equal("Asset", result.Finding.SubjectType);
        // No ReplacementCostEstimate was set on the asset, so the impact stays Operational with
        // no fabricated dollar amount.
        Assert.Null(result.Evidence.Impact?.EstimatedAmount);
    }

    [Fact]
    public async Task The_catalog_is_tenant_scoped()
    {
        await using var s = await fixture.CreateScenarioAsync();
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        await using (var store = s.AdminStore(s.OrganizationB))
        {
            store.ComplianceObligations.Add(new ComplianceObligation(s.OrganizationB, Guid.NewGuid(), s.PropertyB,
                "Org B only obligation", today.AddDays(-30), escalationDays: 7));
            await store.SaveChangesAsync();
        }

        var resultsForA = await CatalogFor(s, s.OrganizationA).EvaluateAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.DoesNotContain(resultsForA, f => f.Finding.SignalType == SignalTypes.ComplianceDeadline);

        var resultsForB = await CatalogFor(s, s.OrganizationB).EvaluateAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Contains(resultsForB, f => f.Finding.SignalType == SignalTypes.ComplianceDeadline);
        Assert.All(resultsForB, f =>
        {
            Assert.Equal(s.OrganizationB, f.Finding.OrganizationId);
            Assert.Equal(s.OrganizationB, f.Evidence.OrganizationId);
        });
    }
}
