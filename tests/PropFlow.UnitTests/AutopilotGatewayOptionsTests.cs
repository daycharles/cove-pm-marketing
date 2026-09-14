using PropFlow.Application.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotGatewayOptionsTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        new AutopilotGatewayOptions().Validate();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_timeout_is_rejected(int timeoutMilliseconds)
    {
        var options = new AutopilotGatewayOptions { TimeoutMilliseconds = timeoutMilliseconds };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
