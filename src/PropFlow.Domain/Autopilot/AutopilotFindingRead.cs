namespace PropFlow.Domain.Autopilot;

// CPM-8.05's per-viewer "unread/read" concept. No entity in this codebase has needed one before
// (TimelineEntry.ResidentVisible is a broadcast flag, not per-viewer; the Attention queue has no
// persisted rows at all) - a finding is org-wide, but whether a given manager has seen it is
// personal, so it cannot live as a column on AutopilotFinding itself without conflating "this
// exists" with "I've looked at it" for every viewer at once. One row per (finding, viewer);
// MarkRead is an upsert at the caller's persistence layer (find-or-create then update ReadAt),
// not a domain concern - this type only validates its own fields.
public sealed class AutopilotFindingRead : TenantEntity
{
    // EF materialization.
    private AutopilotFindingRead(Guid organizationId, Guid id) : base(organizationId, id) { }

    public AutopilotFindingRead(Guid organizationId, Guid id, Guid findingId, Guid viewerId, DateTimeOffset readAt)
        : base(organizationId, id)
    {
        FindingId = AutopilotText.RequireId(findingId, nameof(findingId));
        ViewerId = AutopilotText.RequireId(viewerId, nameof(viewerId));
        ReadAt = readAt.ToUniversalTime();
    }

    public Guid FindingId { get; private set; }
    public Guid ViewerId { get; private set; }
    public DateTimeOffset ReadAt { get; private set; }

    // A later read (re-opening an already-read finding) moves the marker forward; it never moves
    // backward, so "read" cannot be un-set by a stale write racing an older ReadAt.
    public void Touch(DateTimeOffset at)
    {
        if (at > ReadAt) ReadAt = at.ToUniversalTime();
    }
}
