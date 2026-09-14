using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotRecommendationTests
{
    private static readonly Guid Proposer = Guid.NewGuid();

    private static AutopilotRecommendation Create() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Reassign to a technician with capacity", Proposer, DateTimeOffset.UtcNow);

    [Fact]
    public void A_new_recommendation_is_proposed()
    {
        var recommendation = Create();
        Assert.Equal(RecommendationStatus.Proposed, recommendation.Status);
        Assert.Null(recommendation.DecidedBy);
        Assert.False(recommendation.IsTerminal);
    }

    [Fact]
    public void An_empty_description_or_proposer_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AutopilotRecommendation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", Proposer, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new AutopilotRecommendation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Text", Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Approve_records_the_decider_and_reason()
    {
        var recommendation = Create();
        var decider = Guid.NewGuid();
        recommendation.Approve(decider, DateTimeOffset.UtcNow, "Looks right");
        Assert.Equal(RecommendationStatus.Approved, recommendation.Status);
        Assert.Equal(decider, recommendation.DecidedBy);
        Assert.Equal("Looks right", recommendation.DecisionReason);
    }

    [Fact]
    public void Reject_records_the_decider()
    {
        var recommendation = Create();
        recommendation.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(RecommendationStatus.Rejected, recommendation.Status);
    }

    [Fact]
    public void The_proposer_cannot_decide_their_own_recommendation()
    {
        var recommendation = Create();
        Assert.Throws<InvalidOperationException>(() => recommendation.Approve(Proposer, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => recommendation.Reject(Proposer, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expire_needs_no_actor_and_is_terminal()
    {
        var recommendation = Create();
        recommendation.Expire(DateTimeOffset.UtcNow);
        Assert.Equal(RecommendationStatus.Expired, recommendation.Status);
        Assert.True(recommendation.IsTerminal);
        Assert.Null(recommendation.DecidedBy);
    }

    [Fact]
    public void A_decided_recommendation_cannot_be_decided_again()
    {
        var recommendation = Create();
        recommendation.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => recommendation.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => recommendation.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => recommendation.Expire(DateTimeOffset.UtcNow));
    }
}
