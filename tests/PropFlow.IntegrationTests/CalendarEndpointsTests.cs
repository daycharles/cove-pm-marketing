using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PropFlow.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class CalendarEndpointsTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Calendar_returns_lifecycle_events_for_the_selected_tenant_and_property()
    {
        await using var scenario = await fixture.CreateScenarioAsync();
        await scenario.LoginAsync();

        using var response = await scenario.Client.GetAsync(
            $"/api/calendar?from=2026-01-01&to=2026-01-31&propertyId={scenario.PropertyA}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Contains(events!, item => item.GetProperty("type").GetString() == "MoveIn");
        Assert.All(events!, item => Assert.Equal(scenario.PropertyA, item.GetProperty("propertyId").GetGuid()));
        Assert.DoesNotContain(events!, item => item.GetProperty("residentName").GetString() == "Sam Okafor");
    }

    [Fact]
    public async Task Calendar_rejects_an_overly_large_date_range()
    {
        await using var scenario = await fixture.CreateScenarioAsync();
        await scenario.LoginAsync();

        using var response = await scenario.Client.GetAsync("/api/calendar?from=2026-01-01&to=2028-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
