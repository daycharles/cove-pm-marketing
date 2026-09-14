using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

// What a pure analyzer rule hands back — everything EfSignalCatalog needs to build a real
// AutopilotFinding except the run id, organization id, and finding id, none of which a pure
// function can know (assembly-layer concerns: the run id comes from the caller, the finding id
// is freshly minted per row). SubjectType is a literal entity-name string ("WorkItem", "Lease",
// ...), matching the TimelineEntry.RelatedObjectType / ApprovalRequest.SubjectType precedent —
// not a closed enum, same reasoning AutopilotFinding.cs already gives.
public sealed record SignalCandidate(
    string SignalType,
    AttentionSeverity Severity,
    string SubjectType,
    Guid SubjectId,
    string Summary,
    DateTimeOffset DetectedAt,
    DateTimeOffset FreshnessAsOf);
