namespace PropFlow.Domain.Autopilot;

/// <summary>
/// The evidence and impact projection behind one <see cref="AutopilotFinding"/> — CPM-8.03's own
/// deliverable: source-record links, the calculation inputs a rule actually read, an optional
/// impact estimate, a confidence, and the same freshness discipline <see cref="AutopilotFinding"/>
/// already carries. One row per finding (<see cref="FindingId"/>), immutable after it is built —
/// evidence is a snapshot of what an analyzer saw, not a thing anyone edits later; a re-run
/// produces new evidence for a new (or the same) finding, it does not revise old evidence in
/// place. Same append-only-by-construction shape as <see cref="AutopilotAuditEntry"/>: a private
/// constructor plus a public static factory, no persistence yet (no EF mapping, no migration —
/// matches every other CPM-8.01/CPM-8.02 type).
/// </summary>
public sealed class AutopilotEvidence : TenantEntity
{
    private readonly List<CalculationInput> inputs = [];
    private readonly List<SourceLink> sourceLinks = [];

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

        var evidence = new AutopilotEvidence(organizationId, id)
        {
            FindingId = AutopilotText.RequireId(findingId, nameof(findingId)),
            Impact = impact,
            Confidence = confidence,
            FreshnessAsOf = freshnessAsOf.ToUniversalTime(),
        };
        evidence.inputs.AddRange(inputs);
        evidence.sourceLinks.AddRange(sourceLinks);
        return evidence;
    }

    public Guid FindingId { get; private set; }
    public IReadOnlyList<CalculationInput> Inputs => inputs;
    public IReadOnlyList<SourceLink> SourceLinks => sourceLinks;
    public ImpactEstimate? Impact { get; private set; }
    public double Confidence { get; private set; }
    // How current the data behind this evidence was when it was assembled — propagated from the
    // same read the finding itself was detected from (SignalCandidate carries one FreshnessAsOf
    // for both), not a second independent clock.
    public DateTimeOffset FreshnessAsOf { get; private set; }
}
