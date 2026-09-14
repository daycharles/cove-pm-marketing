using PropFlow.Application.Autopilot;
using PropFlow.Infrastructure.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class NoOpModelGatewayTests
{
    [Fact]
    public async Task It_always_reports_unavailable_with_no_output()
    {
        var gateway = new NoOpModelGateway();
        var request = new ModelGatewayRequest(Guid.NewGuid(), "any-prompt", 1,
            new Dictionary<string, string>(), IdempotencyKey: Guid.NewGuid().ToString());

        var result = await gateway.CompleteAsync(request, CancellationToken.None);

        Assert.Equal(ModelGatewayOutcome.Unavailable, result.Outcome);
        Assert.Null(result.Output);
        Assert.NotNull(result.FailureReason);
        Assert.Equal(NoOpModelGateway.Name, result.ProviderName);
    }
}
