using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotFeedbackTests
{
    private static AutopilotFeedback Create(FeedbackSentiment sentiment = FeedbackSentiment.Helpful, string? comment = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sentiment, Guid.NewGuid(), DateTimeOffset.UtcNow, comment);

    [Fact]
    public void A_new_feedback_row_carries_the_sentiment_it_was_created_with()
    {
        var helpful = Create(FeedbackSentiment.Helpful);
        Assert.Equal(FeedbackSentiment.Helpful, helpful.Sentiment);
        var notHelpful = Create(FeedbackSentiment.NotHelpful);
        Assert.Equal(FeedbackSentiment.NotHelpful, notHelpful.Sentiment);
    }

    [Fact]
    public void An_undefined_sentiment_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (FeedbackSentiment)999, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void An_empty_finding_id_or_recorder_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, FeedbackSentiment.Helpful, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), FeedbackSentiment.Helpful, Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_comment_is_optional()
    {
        var feedback = Create(comment: null);
        Assert.Null(feedback.Comment);
        var withComment = Create(comment: "The threshold felt too aggressive");
        Assert.Equal("The threshold felt too aggressive", withComment.Comment);
    }

    [Fact]
    public void Effective_keeps_the_latest_row_per_finding_and_recorder()
    {
        var findingId = Guid.NewGuid();
        var recordedBy = Guid.NewGuid();
        var earlier = new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), findingId, FeedbackSentiment.Helpful, recordedBy, DateTimeOffset.UtcNow.AddMinutes(-5));
        var later = new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), findingId, FeedbackSentiment.NotHelpful, recordedBy, DateTimeOffset.UtcNow);

        var effective = AutopilotFeedback.Effective([earlier, later]);

        Assert.Single(effective);
        Assert.Equal(FeedbackSentiment.NotHelpful, effective[(findingId, recordedBy)].Sentiment);
    }

    [Fact]
    public void Effective_keeps_separate_entries_per_recorder()
    {
        var findingId = Guid.NewGuid();
        var a = new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), findingId, FeedbackSentiment.Helpful, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var b = new AutopilotFeedback(Guid.NewGuid(), Guid.NewGuid(), findingId, FeedbackSentiment.NotHelpful, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var effective = AutopilotFeedback.Effective([a, b]);

        Assert.Equal(2, effective.Count);
    }
}
