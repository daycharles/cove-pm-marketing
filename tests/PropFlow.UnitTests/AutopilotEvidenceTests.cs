using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotEvidenceTests
{
    private static readonly IReadOnlyList<CalculationInput> OneInput = [new CalculationInput("Outstanding", "$500.00")];
    private static readonly IReadOnlyList<SourceLink> OneLink = [new SourceLink("LeaseCharge", Guid.NewGuid())];

    private static AutopilotEvidence Build(
        IReadOnlyList<CalculationInput>? inputs = null,
        IReadOnlyList<SourceLink>? sourceLinks = null,
        ImpactEstimate? impact = null,
        double confidence = 1.0) =>
        AutopilotEvidence.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            inputs ?? OneInput, sourceLinks ?? OneLink, impact, confidence, DateTimeOffset.UtcNow);

    [Fact]
    public void Build_captures_every_field()
    {
        var organizationId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var freshnessAsOf = DateTimeOffset.UtcNow;
        var impact = new ImpactEstimate(ImpactCategory.Financial, "Unpaid rent", 500m);

        var evidence = AutopilotEvidence.Build(organizationId, id, findingId, OneInput, OneLink, impact, 0.9, freshnessAsOf);

        Assert.Equal(organizationId, evidence.OrganizationId);
        Assert.Equal(id, evidence.Id);
        Assert.Equal(findingId, evidence.FindingId);
        Assert.Equal(OneInput, evidence.Inputs);
        Assert.Equal(OneLink, evidence.SourceLinks);
        Assert.Equal(impact, evidence.Impact);
        Assert.Equal(0.9, evidence.Confidence);
        Assert.Equal(freshnessAsOf, evidence.FreshnessAsOf);
    }

    [Fact]
    public void An_impact_estimate_is_optional()
    {
        var evidence = Build(impact: null);
        Assert.Null(evidence.Impact);
    }

    [Fact]
    public void Evidence_needs_at_least_one_calculation_input()
    {
        Assert.Throws<ArgumentException>(() => Build(inputs: []));
    }

    [Fact]
    public void Evidence_needs_at_least_one_source_link()
    {
        Assert.Throws<ArgumentException>(() => Build(sourceLinks: []));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Confidence_must_be_between_zero_and_one(double confidence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(confidence: confidence));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Confidence_accepts_its_own_boundary_values(double confidence)
    {
        var evidence = Build(confidence: confidence);
        Assert.Equal(confidence, evidence.Confidence);
    }
}
