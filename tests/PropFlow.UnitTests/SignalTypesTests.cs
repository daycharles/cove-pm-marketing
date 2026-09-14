using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class SignalTypesTests
{
    [Fact]
    public void The_catalog_has_eleven_distinct_signal_types_matching_the_task_scope()
    {
        Assert.Equal(11, SignalTypes.All.Count);
        Assert.Equal(SignalTypes.All.Count, SignalTypes.All.Distinct().Count());
    }

    [Fact]
    public void Every_signal_type_fits_within_AutopilotFinding_SignalTypeMaxLength()
    {
        Assert.All(SignalTypes.All, signalType =>
            Assert.True(signalType.Length <= AutopilotFinding.SignalTypeMaxLength));
    }
}
