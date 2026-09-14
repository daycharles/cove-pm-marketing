using PropFlow.Domain.Attention;

namespace PropFlow.Domain.Autopilot;

// What a pure analyzer rule hands back — everything EfSignalCatalog needs to build a real
// AutopilotFinding except the run id, organization id, and finding id, none of which a pure
// function can know (assembly-layer concerns: the run id comes from the caller, the finding id
// is freshly minted per row). SubjectType is a literal entity-name string ("WorkItem", "Lease",
// ...), matching the TimelineEntry.RelatedObjectType / ApprovalRequest.SubjectType precedent —
// not a closed enum, same reasoning AutopilotFinding.cs already gives.
//
// Evidence is mandatory, not optional (CPM-8.03): every rule already has its calculation inputs
// and the record(s) it read right where it decides Severity and writes Summary, so building the
// evidence anywhere else would mean re-deriving numbers a second time from prose — exactly what
// "never fabricates missing values" is warning against.
//
// PropertyId (CPM-8.05) is nullable and optional (defaults to null): not every finding is
// property-scoped (an invoice exception on a vendor-level PayableInvoice has none), and a rule
// only carries it when its own data already has it. PortfolioId is deliberately NOT here —
// EfSignalCatalog resolves it once, after every candidate is collected, from the distinct
// PropertyIds involved, rather than every rule (or every snapshot) needing to know it.
public sealed record SignalCandidate(
    string SignalType,
    AttentionSeverity Severity,
    string SubjectType,
    Guid SubjectId,
    string Summary,
    DateTimeOffset DetectedAt,
    DateTimeOffset FreshnessAsOf,
    EvidenceCandidate Evidence,
    Guid? PropertyId = null);
