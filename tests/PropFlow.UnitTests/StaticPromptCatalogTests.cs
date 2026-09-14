using PropFlow.Application.Autopilot;
using PropFlow.Infrastructure.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class StaticPromptCatalogTests
{
    private readonly StaticPromptCatalog catalog = new();

    [Fact]
    public void A_known_prompt_id_resolves()
    {
        var result = catalog.Resolve(StaticPromptCatalog.ExplainFindingId);
        Assert.Equal(PromptLookupOutcome.Found, result.Outcome);
        Assert.NotNull(result.Prompt);
        Assert.Equal(StaticPromptCatalog.ExplainFindingId, result.Prompt.Id);
        Assert.Equal(1, result.Prompt.Version);
        Assert.False(string.IsNullOrWhiteSpace(result.Prompt.Instructions));
    }

    [Fact]
    public void An_unknown_prompt_id_reports_not_found_rather_than_throwing()
    {
        var result = catalog.Resolve("does-not-exist");
        Assert.Equal(PromptLookupOutcome.NotFound, result.Outcome);
        Assert.Null(result.Prompt);
    }
}
