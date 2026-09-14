namespace PropFlow.Domain.Autopilot;

/// <summary>One named, already-formatted value that fed a signal's decision — "Budgeted", "$1,000.00".</summary>
public sealed record CalculationInput(string Name, string Value);

/// <summary>One record a finding's evidence traces back to. <c>EntityType</c> is a literal
/// entity-name string, same convention as <see cref="SignalCandidate.SubjectType"/>.</summary>
public sealed record SourceLink(string EntityType, Guid EntityId);

public enum ImpactCategory { Operational, Financial }

/// <summary>
/// A projected consequence of leaving the finding unaddressed. <see cref="EstimatedAmount"/> is
/// deliberately nullable — a compliance deadline or a stalled work item has a real operational
/// impact with no honest dollar figure attached to it, and "never fabricates missing values"
/// (this task's own charter) means that case reports no amount, not a guessed or zeroed one.
/// </summary>
public sealed record ImpactEstimate(ImpactCategory Category, string Description, decimal? EstimatedAmount);

/// <summary>
/// The evidence half of a <see cref="SignalCandidate"/> — everything <c>AutopilotEvidence.Build</c>
/// needs except the ids only the assembly layer can mint. Every candidate carries one: a finding
/// with no inputs and no source links is not evidence-backed, so there is no candidate shape that
/// omits them.
/// </summary>
public sealed record EvidenceCandidate(
    IReadOnlyList<CalculationInput> Inputs,
    IReadOnlyList<SourceLink> SourceLinks,
    ImpactEstimate? Impact,
    double Confidence);
