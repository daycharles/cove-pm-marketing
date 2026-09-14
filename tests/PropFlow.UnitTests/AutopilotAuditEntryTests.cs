using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotAuditEntryTests
{
    [Fact]
    public void Record_captures_every_field()
    {
        var organizationId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entry = AutopilotAuditEntry.Record(organizationId, Guid.NewGuid(), "RecommendationApproved",
            "AutopilotRecommendation", subjectId, actorId, now, "Approved: reassign to a technician with capacity");

        Assert.Equal(organizationId, entry.OrganizationId);
        Assert.Equal("RecommendationApproved", entry.EventType);
        Assert.Equal("AutopilotRecommendation", entry.SubjectType);
        Assert.Equal(subjectId, entry.SubjectId);
        Assert.Equal(actorId, entry.ActorId);
        Assert.Equal(now, entry.OccurredAt);
        Assert.Equal("Approved: reassign to a technician with capacity", entry.Detail);
    }

    [Fact]
    public void A_system_attributed_event_records_a_null_actor()
    {
        var entry = AutopilotAuditEntry.Record(Guid.NewGuid(), Guid.NewGuid(), "RunCompleted",
            "AutopilotRun", Guid.NewGuid(), actorId: null, DateTimeOffset.UtcNow, "Completed with 3 findings");
        Assert.Null(entry.ActorId);
    }

    [Fact]
    public void An_explicit_empty_actor_is_rejected_rather_than_silently_becoming_system_attributed()
    {
        Assert.Throws<ArgumentException>(() => AutopilotAuditEntry.Record(Guid.NewGuid(), Guid.NewGuid(), "RunCompleted",
            "AutopilotRun", Guid.NewGuid(), actorId: Guid.Empty, DateTimeOffset.UtcNow, "detail"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_event_type_subject_type_or_detail_is_rejected(string blank)
    {
        Assert.Throws<ArgumentException>(() => AutopilotAuditEntry.Record(Guid.NewGuid(), Guid.NewGuid(), blank,
            "AutopilotRun", Guid.NewGuid(), null, DateTimeOffset.UtcNow, "detail"));
        Assert.Throws<ArgumentException>(() => AutopilotAuditEntry.Record(Guid.NewGuid(), Guid.NewGuid(), "RunCompleted",
            blank, Guid.NewGuid(), null, DateTimeOffset.UtcNow, "detail"));
        Assert.Throws<ArgumentException>(() => AutopilotAuditEntry.Record(Guid.NewGuid(), Guid.NewGuid(), "RunCompleted",
            "AutopilotRun", Guid.NewGuid(), null, DateTimeOffset.UtcNow, blank));
    }
}
