using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class ActionAuditEventTypesTests
{
    [Fact]
    public void The_catalog_has_five_distinct_event_types_matching_the_proposal_lifecycle()
    {
        Assert.Equal(5, ActionAuditEventTypes.All.Count);
        Assert.Equal(ActionAuditEventTypes.All.Count, ActionAuditEventTypes.All.Distinct().Count());
    }

    [Fact]
    public void Every_event_type_fits_within_AutopilotAuditEntry_EventTypeMaxLength()
    {
        Assert.All(ActionAuditEventTypes.All, eventType =>
            Assert.True(eventType.Length <= AutopilotAuditEntry.EventTypeMaxLength));
    }
}
