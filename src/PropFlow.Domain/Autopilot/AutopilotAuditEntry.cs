namespace PropFlow.Domain.Autopilot;

// The append-only tenant audit trail for the whole Autopilot aggregate - every run, finding,
// recommendation and action-proposal transition that matters for trust gets one row here. A
// dedicated trail rather than a reuse of the general operations.Timeline (TimelineEntry) on
// purpose: M8's own definition of done requires "every resulting mutation [visible] in an audit
// trail" as a first-class, load-bearing safety property of the whole epic, not an incidental
// side-record the way a WorkItem's timeline entry is - and CPM-8.13 ("operational metrics ...
// model/version traceability") is expected to extend this table with cost, latency and
// model-version columns a general Timeline row has no reason to carry. Append-only is enforced
// the same three ways operations."Timeline" already is (EF refuses a non-Added entry, the
// runtime role gets GRANT SELECT, INSERT only, a trigger raises on UPDATE/DELETE) - the persistence
// task that gives this table a home applies all three, this task only makes the shape unwritable
// from C# by giving it no mutator methods at all.
public sealed class AutopilotAuditEntry : TenantEntity
{
    public const int EventTypeMaxLength = 100;
    public const int SubjectTypeMaxLength = 100;
    public const int DetailMaxLength = 2000;

    // EF materialization.
    private AutopilotAuditEntry(Guid organizationId, Guid id) : base(organizationId, id) { }

    private AutopilotAuditEntry(
        Guid organizationId,
        Guid id,
        string eventType,
        string subjectType,
        Guid subjectId,
        Guid? actorId,
        DateTimeOffset occurredAt,
        string detail)
        : base(organizationId, id)
    {
        EventType = AutopilotText.RequireSingleLine(eventType, nameof(eventType), EventTypeMaxLength);
        SubjectType = AutopilotText.RequireSingleLine(subjectType, nameof(subjectType), SubjectTypeMaxLength);
        SubjectId = AutopilotText.RequireId(subjectId, nameof(subjectId));
        ActorId = actorId == Guid.Empty ? throw new ArgumentException("Actor, if given, must not be empty.", nameof(actorId)) : actorId;
        OccurredAt = occurredAt.ToUniversalTime();
        Detail = AutopilotText.RequireSingleLine(detail, nameof(detail), DetailMaxLength);
    }

    public string EventType { get; private set; } = "";
    public string SubjectType { get; private set; } = "";
    public Guid SubjectId { get; private set; }
    // Null for a system-attributed event - a scheduled run starting or completing on its own has
    // no human actor, and forcing one would misattribute it to whoever happened to trigger the
    // scheduler rather than record the truth that nobody decided this one.
    public Guid? ActorId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Detail { get; private set; } = "";

    public static AutopilotAuditEntry Record(
        Guid organizationId,
        Guid id,
        string eventType,
        string subjectType,
        Guid subjectId,
        Guid? actorId,
        DateTimeOffset occurredAt,
        string detail) =>
        new(organizationId, id, eventType, subjectType, subjectId, actorId, occurredAt, detail);
}
