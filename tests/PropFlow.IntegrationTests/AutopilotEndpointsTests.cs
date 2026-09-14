using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.Properties;
using Xunit;

namespace PropFlow.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class AutopilotEndpointsTests(DatabaseFixture fixture)
{
    private static AutopilotFinding Finding(Guid org, Guid runId, Guid id, string signalType = "ComplianceDeadline",
        PropFlow.Domain.Attention.AttentionSeverity severity = PropFlow.Domain.Attention.AttentionSeverity.Warning,
        Guid? propertyId = null, DateTimeOffset? detectedAt = null)
    {
        var now = detectedAt ?? DateTimeOffset.UtcNow;
        return new AutopilotFinding(org, id, runId, signalType, severity, "ComplianceObligation", Guid.NewGuid(),
            "Fire extinguisher inspection is past due.", now, now, propertyId);
    }

    private static AutopilotEvidence Evidence(Guid org, Guid findingId, decimal? impactAmount = null) =>
        AutopilotEvidence.Build(org, Guid.NewGuid(), findingId,
            [new CalculationInput("Due on", "2026-09-01")],
            [new SourceLink("ComplianceObligation", Guid.NewGuid())],
            impactAmount is { } amount ? new ImpactEstimate(ImpactCategory.Financial, "impact", amount) : null,
            1.0, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Triggering_a_run_persists_findings_with_evidence_that_the_brief_then_lists()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();

        await using (var store = s.AdminStore(s.OrganizationA))
        {
            store.ComplianceObligations.Add(new ComplianceObligation(s.OrganizationA, Guid.NewGuid(), s.PropertyA,
                "Fire extinguisher inspection", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30), escalationDays: 7));
            await store.SaveChangesAsync();
        }

        var runResponse = await s.Client.PostAsync("/api/autopilot/runs", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.OK, runResponse.StatusCode);
        var run = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(run.GetProperty("findingCount").GetInt32() > 0);

        var brief = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief");
        var items = brief.GetProperty("items").EnumerateArray().ToArray();
        var complianceFinding = items.Single(i => i.GetProperty("signalType").GetString() == "ComplianceDeadline");
        Assert.Equal("Critical", complianceFinding.GetProperty("severity").GetString()); // escalated (30d overdue, 7d window)
        Assert.False(complianceFinding.GetProperty("isRead").GetBoolean());
        Assert.NotEmpty(complianceFinding.GetProperty("inputs").EnumerateArray());
        Assert.NotEmpty(complianceFinding.GetProperty("sourceLinks").EnumerateArray());
    }

    [Fact]
    public async Task The_brief_defaults_to_hiding_dismissed_and_resolved_findings()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var dismissedId = Guid.NewGuid();
        var activeId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(2, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            var dismissed = Finding(s.OrganizationA, runId, dismissedId);
            dismissed.Dismiss(Guid.NewGuid(), DateTimeOffset.UtcNow);
            store.Findings.Add(dismissed);
            store.Evidence.Add(Evidence(s.OrganizationA, dismissedId));
            store.Findings.Add(Finding(s.OrganizationA, runId, activeId));
            store.Evidence.Add(Evidence(s.OrganizationA, activeId));
            await store.SaveChangesAsync();
        }

        var brief = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief");
        var ids = brief.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToArray();
        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(dismissedId, ids);

        var dismissedOnly = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief?status=Dismissed");
        var dismissedIds = dismissedOnly.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToArray();
        Assert.Contains(dismissedId, dismissedIds);
    }

    [Fact]
    public async Task The_brief_orders_critical_before_warning_regardless_of_insertion_order()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var warningId = Guid.NewGuid();
        var criticalId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(2, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            // Inserted warning-first, on purpose — the brief's own ordering has to sort them, not the write order.
            store.Findings.Add(Finding(s.OrganizationA, runId, warningId, severity: PropFlow.Domain.Attention.AttentionSeverity.Warning));
            store.Evidence.Add(Evidence(s.OrganizationA, warningId));
            store.Findings.Add(Finding(s.OrganizationA, runId, criticalId, severity: PropFlow.Domain.Attention.AttentionSeverity.Critical));
            store.Evidence.Add(Evidence(s.OrganizationA, criticalId));
            await store.SaveChangesAsync();
        }

        var brief = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief");
        var items = brief.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(criticalId, items[0].GetProperty("id").GetGuid());
        Assert.Equal(warningId, items[1].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Property_filter_narrows_the_brief_to_that_property_only()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var scopedId = Guid.NewGuid();
        var unscopedId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(2, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, runId, scopedId, propertyId: s.PropertyA));
            store.Evidence.Add(Evidence(s.OrganizationA, scopedId));
            store.Findings.Add(Finding(s.OrganizationA, runId, unscopedId, propertyId: null));
            store.Evidence.Add(Evidence(s.OrganizationA, unscopedId));
            await store.SaveChangesAsync();
        }

        var brief = await s.Client.GetFromJsonAsync<JsonElement>($"/api/autopilot/brief?propertyId={s.PropertyA}");
        var ids = brief.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToArray();
        Assert.Contains(scopedId, ids);
        Assert.DoesNotContain(unscopedId, ids);
    }

    [Fact]
    public async Task Review_dismiss_resolve_snooze_and_reopen_all_work_through_the_api()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var findingId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, runId, findingId));
            store.Evidence.Add(Evidence(s.OrganizationA, findingId));
            await store.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await s.Client.PostAsync($"/api/autopilot/findings/{findingId}/review", null)).StatusCode);

        var snooze = await s.Client.PostAsJsonAsync($"/api/autopilot/findings/{findingId}/snooze",
            new { until = DateTimeOffset.UtcNow.AddDays(3) });
        Assert.Equal(HttpStatusCode.OK, snooze.StatusCode);
        var snoozed = await snooze.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Snoozed", snoozed.GetProperty("status").GetString());

        var reopen = await s.Client.PostAsync($"/api/autopilot/findings/{findingId}/reopen", null);
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
        var reopened = await reopen.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New", reopened.GetProperty("status").GetString());

        var dismiss = await s.Client.PostAsJsonAsync($"/api/autopilot/findings/{findingId}/dismiss", new { reason = "Not applicable" });
        Assert.Equal(HttpStatusCode.OK, dismiss.StatusCode);

        // Terminal now — a second decision is a real conflict, not a bad request.
        var secondDismiss = await s.Client.PostAsync($"/api/autopilot/findings/{findingId}/resolve", null);
        Assert.Equal(HttpStatusCode.Conflict, secondDismiss.StatusCode);
    }

    [Fact]
    public async Task Marking_a_finding_read_is_reflected_in_the_brief()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var findingId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, runId, findingId));
            store.Evidence.Add(Evidence(s.OrganizationA, findingId));
            await store.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.NoContent, (await s.Client.PostAsync($"/api/autopilot/findings/{findingId}/read", null)).StatusCode);

        var brief = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief");
        var item = brief.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == findingId);
        Assert.True(item.GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task Feedback_can_be_recorded_against_a_finding()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var runId = Guid.NewGuid();
        var findingId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, runId, findingId));
            store.Evidence.Add(Evidence(s.OrganizationA, findingId));
            await store.SaveChangesAsync();
        }

        var response = await s.Client.PostAsJsonAsync($"/api/autopilot/findings/{findingId}/feedback",
            new { sentiment = "Helpful", comment = "Correctly flagged" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var admin = s.AutopilotAsAdmin(s.OrganizationA);
        Assert.Single(admin.Feedback, f => f.FindingId == findingId);
    }

    [Fact]
    public async Task The_brief_is_tenant_scoped()
    {
        await using var s = await fixture.CreateScenarioAsync();
        var runId = Guid.NewGuid();
        var orgBFindingId = Guid.NewGuid();

        await using (var store = s.AutopilotAsAdmin(s.OrganizationB))
        {
            var run = new AutopilotRun(s.OrganizationB, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationB, runId, orgBFindingId));
            store.Evidence.Add(Evidence(s.OrganizationB, orgBFindingId));
            await store.SaveChangesAsync();
        }

        await s.LoginAsync();
        var brief = await s.Client.GetFromJsonAsync<JsonElement>("/api/autopilot/brief");
        var ids = brief.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToArray();
        Assert.DoesNotContain(orgBFindingId, ids);
    }

    [Fact]
    public async Task The_group_needs_authentication_and_the_ManageAutopilot_capability()
    {
        await using var s = await fixture.CreateScenarioAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Client.GetAsync("/api/autopilot/brief")).StatusCode);

        await s.LoginAsync(reader: true);
        Assert.Equal(HttpStatusCode.Forbidden, (await s.Client.GetAsync("/api/autopilot/brief")).StatusCode);
    }
}
