using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotFindingReadTests
{
    [Fact]
    public void Construction_captures_every_field()
    {
        var organizationId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var readAt = DateTimeOffset.UtcNow;

        var read = new AutopilotFindingRead(organizationId, Guid.NewGuid(), findingId, viewerId, readAt);

        Assert.Equal(organizationId, read.OrganizationId);
        Assert.Equal(findingId, read.FindingId);
        Assert.Equal(viewerId, read.ViewerId);
        Assert.Equal(readAt, read.ReadAt);
    }

    [Theory]
    [MemberData(nameof(EmptyIdCases))]
    public void An_empty_finding_id_or_viewer_id_is_rejected(Guid findingId, Guid viewerId)
    {
        Assert.Throws<ArgumentException>(() =>
            new AutopilotFindingRead(Guid.NewGuid(), Guid.NewGuid(), findingId, viewerId, DateTimeOffset.UtcNow));
    }

    public static IEnumerable<object[]> EmptyIdCases()
    {
        yield return [Guid.Empty, Guid.NewGuid()];
        yield return [Guid.NewGuid(), Guid.Empty];
    }

    [Fact]
    public void Touch_moves_the_marker_forward()
    {
        var read = new AutopilotFindingRead(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var later = read.ReadAt.AddMinutes(10);
        read.Touch(later);
        Assert.Equal(later, read.ReadAt);
    }

    [Fact]
    public void Touch_never_moves_the_marker_backward()
    {
        var now = DateTimeOffset.UtcNow;
        var read = new AutopilotFindingRead(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);
        read.Touch(now.AddMinutes(-10));
        Assert.Equal(now, read.ReadAt);
    }
}
