using System.Text.Json;

namespace PropFlow.Domain.Autopilot;

/// <summary>
/// The evidence and impact projection behind one <see cref="AutopilotFinding"/> — CPM-8.03's own
/// deliverable: source-record links, the calculation inputs a rule actually read, an optional
/// impact estimate, a confidence, and the same freshness discipline <see cref="AutopilotFinding"/>
/// already carries. One row per finding (<see cref="FindingId"/>), immutable after it is built —
/// evidence is a snapshot of what an analyzer saw, not a thing anyone edits later; a re-run
/// produces new evidence for a new (or the same) finding, it does not revise old evidence in
/// place. Same append-only-by-construction shape as <see cref="AutopilotAuditEntry"/>: a private
/// constructor plus a public static factory, no mutator methods at all.
///
/// Inputs/SourceLinks are stored as JSON strings (<see cref="InputsJson"/>/
/// <see cref="SourceLinksJson"/>) rather than EF owned collections — the same choice
/// <c>ReportSchedule.FilterJson</c> already made for a variable-shaped list, and simpler than
/// getting EF to map a value-converted collection through a private backing field. The public
/// <see cref="Inputs"/>/<see cref="SourceLinks"/> properties deserialize on read so every
/// existing caller (CPM-8.02's rule files, CPM-8.03's tests) keeps working unchanged. Impact is
/// flattened into three plain nullable columns for the same reason: an EF owned type is more
/// machinery than one nullable enum, string and decimal need.
/// </summary>
public sealed class AutopilotEvidence : TenantEntity
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    // EF materialization.
    private AutopilotEvidence(Guid organizationId, Guid id) : base(organizationId, id) { }

    public static AutopilotEvidence Build(
        Guid organizationId,
        Guid id,
        Guid findingId,
        IReadOnlyList<CalculationInput> inputs,
        IReadOnlyList<SourceLink> sourceLinks,
        ImpactEstimate? impact,
        double confidence,
        DateTimeOffset freshnessAsOf)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(sourceLinks);
        if (inputs.Count == 0)
            throw new ArgumentException("Evidence needs at least one calculation input — a finding with no inputs is not explained.", nameof(inputs));
        if (sourceLinks.Count == 0)
            throw new ArgumentException("Evidence needs at least one source-record link — a finding with no source is not evidence-backed.", nameof(sourceLinks));
        if (confidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        return new AutopilotEvidence(organizationId, id)
        {
            FindingId = AutopilotText.RequireId(findingId, nameof(findingId)),
            InputsJson = JsonSerializer.Serialize(inputs, SerializerOptions),
            SourceLinksJson = JsonSerializer.Serialize(sourceLinks, SerializerOptions),
            ImpactCategory = impact?.Category,
            ImpactDescription = impact?.Description,
            ImpactEstimatedAmount = impact?.EstimatedAmount,
            Confidence = confidence,
            FreshnessAsOf = freshnessAsOf.ToUniversalTime(),
        };
    }

    public Guid FindingId { get; private set; }
    public string InputsJson { get; private set; } = "[]";
    public string SourceLinksJson { get; private set; } = "[]";
    public IReadOnlyList<CalculationInput> Inputs =>
        JsonSerializer.Deserialize<List<CalculationInput>>(InputsJson, SerializerOptions) ?? [];
    public IReadOnlyList<SourceLink> SourceLinks =>
        JsonSerializer.Deserialize<List<SourceLink>>(SourceLinksJson, SerializerOptions) ?? [];
    public ImpactCategory? ImpactCategory { get; private set; }
    public string? ImpactDescription { get; private set; }
    public decimal? ImpactEstimatedAmount { get; private set; }
    public ImpactEstimate? Impact => ImpactCategory is { } category ? new ImpactEstimate(category, ImpactDescription ?? "", ImpactEstimatedAmount) : null;
    public double Confidence { get; private set; }
    // How current the data behind this evidence was when it was assembled — propagated from the
    // same read the finding itself was detected from (SignalCandidate carries one FreshnessAsOf
    // for both), not a second independent clock.
    public DateTimeOffset FreshnessAsOf { get; private set; }
}
