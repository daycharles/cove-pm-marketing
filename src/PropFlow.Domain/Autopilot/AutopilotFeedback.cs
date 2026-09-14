namespace PropFlow.Domain.Autopilot;

public enum FeedbackSentiment { Helpful, NotHelpful }

// One person's reaction to one AutopilotFinding, at the moment they recorded it. Append-only by
// design, the same call ApplicationConsent (FS-S05) makes: a changed mind writes a new row rather
// than editing the old one, so "what did this person think when they saw it" is never lost to a
// later correction. CPM-8.13's outcome/metrics work reduces a finding's feedback history to
// whatever it needs (latest wins, majority, etc.) - that reduction does not belong here, the same
// way ApplicationConsent.Effective is a static helper next to the entity rather than logic baked
// into the row itself.
public sealed class AutopilotFeedback : TenantEntity
{
    public const int CommentMaxLength = 1000;

    // EF materialization.
    private AutopilotFeedback(Guid organizationId, Guid id) : base(organizationId, id) { }

    public AutopilotFeedback(
        Guid organizationId,
        Guid id,
        Guid findingId,
        FeedbackSentiment sentiment,
        Guid recordedBy,
        DateTimeOffset recordedAt,
        string? comment = null)
        : base(organizationId, id)
    {
        if (!Enum.IsDefined(sentiment)) throw new ArgumentOutOfRangeException(nameof(sentiment));
        FindingId = AutopilotText.RequireId(findingId, nameof(findingId));
        Sentiment = sentiment;
        RecordedBy = AutopilotText.RequireId(recordedBy, nameof(recordedBy));
        RecordedAt = recordedAt.ToUniversalTime();
        Comment = AutopilotText.OptionalText(comment, nameof(comment), CommentMaxLength);
    }

    public Guid FindingId { get; private set; }
    public FeedbackSentiment Sentiment { get; private set; }
    public Guid RecordedBy { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string? Comment { get; private set; }

    // Reduces a sequence of feedback rows to the effective one per (FindingId, RecordedBy): the
    // latest RecordedAt wins, matching ApplicationConsent.Effective's exact tie-breaking rule
    // (the row later in the sequence wins on an exact tie) for the same reason - callers hand it
    // whatever a query returned without having to sort first.
    public static IReadOnlyDictionary<(Guid FindingId, Guid RecordedBy), AutopilotFeedback> Effective(IEnumerable<AutopilotFeedback> feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        var effective = new Dictionary<(Guid, Guid), AutopilotFeedback>();
        foreach (var entry in feedback)
        {
            var key = (entry.FindingId, entry.RecordedBy);
            if (!effective.TryGetValue(key, out var current) || entry.RecordedAt >= current.RecordedAt)
                effective[key] = entry;
        }
        return effective;
    }
}
