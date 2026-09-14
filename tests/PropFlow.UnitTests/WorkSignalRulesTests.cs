using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class WorkSignalRulesTests
{
    [Theory]
    [InlineData(AttentionReason.UnassignedEmergency, SignalTypes.SlaRisk)]
    [InlineData(AttentionReason.SlaBreach, SignalTypes.SlaRisk)]
    [InlineData(AttentionReason.Overdue, SignalTypes.SlaRisk)]
    [InlineData(AttentionReason.WaitingOnVendor, SignalTypes.VendorFollowUp)]
    [InlineData(AttentionReason.WaitingOnResident, SignalTypes.StalledWork)]
    [InlineData(AttentionReason.UnitTurnAtRisk, SignalTypes.TurnRisk)]
    public void Each_reused_attention_reason_maps_to_its_autopilot_signal_type(AttentionReason reason, string expected)
    {
        Assert.Equal(expected, WorkSignalRules.MapSignalType(reason));
    }

    [Fact]
    public void Repeat_repair_maps_to_nothing_because_the_tenant_wide_sweep_owns_that_signal_instead()
    {
        Assert.Null(WorkSignalRules.MapSignalType(AttentionReason.RepeatRepair));
    }
}
