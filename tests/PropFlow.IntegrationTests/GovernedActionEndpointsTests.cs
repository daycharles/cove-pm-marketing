using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropFlow.Domain.Attention;
using PropFlow.Domain.Autopilot;
using PropFlow.Domain.People;
using Xunit;

namespace PropFlow.IntegrationTests;

// CPM-8.08: the governed action framework's HTTP surface — Recommendation persistence CPM-8.07's
// domain-only slice left out, plus the seven action-type adapters and their execution.
[Collection("PostgreSQL")]
public sealed class GovernedActionEndpointsTests(DatabaseFixture fixture)
{
    private static AutopilotFinding Finding(Guid org, Guid runId, Guid id) =>
        new(org, id, runId, "SlaRisk", AttentionSeverity.Warning, "WorkItem", Guid.NewGuid(),
            "Critical priority with no vendor or staff assigned.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    // Seeds a run + finding, then creates and approves a recommendation for it through the real
    // HTTP surface — every action-proposal route requires an Approved recommendation to exist,
    // so this is the fixture every test in this file starts from.
    private static async Task<Guid> ApprovedRecommendationAsync(Scenario s)
    {
        var runId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, runId, "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, runId, findingId));
            await store.SaveChangesAsync();
        }

        var createResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/findings/{findingId}/recommendations",
            new { description = "Assign a vendor to unblock this emergency." });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recommendationId = created.GetProperty("id").GetGuid();

        // Approved directly through the store, not the HTTP endpoint: the endpoint is logged in
        // as AdminA, the same account that just proposed this recommendation, and
        // AutopilotRecommendation.Approve refuses that — separation of duties, the same rule
        // AutopilotActionProposal.Decide enforces and ApprovalEndpointsTests already covers for
        // this endpoint's own approve route. This fixture is about what happens once a
        // recommendation IS approved, not re-proving that rule.
        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var recommendation = await store.Recommendations.SingleAsync(x => x.Id == recommendationId);
            recommendation.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
            await store.SaveChangesAsync();
        }
        return recommendationId;
    }

    // Same reasoning as ApprovedRecommendationAsync: the client is always logged in as AdminA,
    // who proposed every action in this file (CreateProposal's Actor(user) call), so an HTTP
    // approve as that same session always hits AutopilotActionProposal's own separation-of-duties
    // guard. Approving_ones_own_action_proposal_is_refused below proves that guard holds through
    // the real endpoint; every other test approves this way to reach the state it actually wants
    // to exercise.
    private static async Task ApproveActionAsync(Scenario s, Guid proposalId)
    {
        await using var store = s.AutopilotAsAdmin(s.OrganizationA);
        var proposal = await store.ActionProposals.SingleAsync(x => x.Id == proposalId);
        proposal.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await store.SaveChangesAsync();
    }

    [Fact]
    public async Task Approving_ones_own_action_proposal_is_refused()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);
        var propose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        var proposalId = (await propose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var approveResponse = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, approveResponse.StatusCode);
    }

    [Fact]
    public async Task A_recommendation_moves_from_proposed_to_approved_and_cannot_decide_twice()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        var again = await s.Client.PostAsync($"/api/autopilot/recommendations/{recommendationId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Proposing_an_action_from_a_not_yet_approved_recommendation_is_refused()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var findingId = Guid.NewGuid();
        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var run = new AutopilotRun(s.OrganizationA, Guid.NewGuid(), "Manual", DateTimeOffset.UtcNow);
            run.Complete(1, DateTimeOffset.UtcNow);
            store.Runs.Add(run);
            store.Findings.Add(Finding(s.OrganizationA, run.Id, findingId));
            await store.SaveChangesAsync();
        }
        var createResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/findings/{findingId}/recommendations", new { description = "x" });
        var recommendationId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var proposeResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        Assert.Equal(HttpStatusCode.Conflict, proposeResponse.StatusCode);
    }

    [Fact]
    public async Task AssignVendor_proposes_approves_and_executes_a_real_vendor_assignment()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        var proposeResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        Assert.Equal(HttpStatusCode.Created, proposeResponse.StatusCode);
        var proposal = await proposeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = proposal.GetProperty("id").GetGuid();
        Assert.Equal("AssignVendor", proposal.GetProperty("actionType").GetString());
        Assert.Equal("Proposed", proposal.GetProperty("status").GetString());
        var payloadFields = proposal.GetProperty("payload").EnumerateArray()
            .ToDictionary(f => f.GetProperty("name").GetString()!, f => f.GetProperty("value").GetString());
        Assert.Equal(s.WorkA.ToString(), payloadFields["WorkId"]);
        Assert.Equal(s.VendorA.ToString(), payloadFields["VendorId"]);

        await ApproveActionAsync(s, proposalId);

        var executeResponse = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/execute", null);
        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        var executed = await executeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Executed", executed.GetProperty("status").GetString());
        Assert.Contains("assigned", executed.GetProperty("executionOutcome").GetString(), StringComparison.OrdinalIgnoreCase);

        // The real mutation happened — not just a proposal saying it would.
        await using var operations = s.AdminStore(s.OrganizationA);
        var work = await operations.WorkItems.FindAsync(s.OrganizationA, s.WorkA);
        Assert.Equal(s.VendorA, work!.VendorId);
    }

    [Fact]
    public async Task Executing_an_action_before_it_is_approved_is_refused()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);
        var proposeResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        var proposalId = (await proposeResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var executeResponse = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/execute", null);
        Assert.Equal(HttpStatusCode.Conflict, executeResponse.StatusCode);
    }

    [Fact]
    public async Task A_second_proposal_of_the_same_action_type_for_the_same_recommendation_is_rejected_as_a_duplicate()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        var first = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Executing_a_proposal_whose_required_capability_nobody_holds_is_denied_without_failing_the_proposal()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);
        Guid proposalId;
        await using (var store = s.AutopilotAsAdmin(s.OrganizationA))
        {
            var proposal = new AutopilotActionProposal(
                s.OrganizationA, Guid.NewGuid(), recommendationId, GovernedActionTypes.AssignVendor,
                "Assign a vendor.", s.AdminA, DateTimeOffset.UtcNow,
                new ActionPayload([new ActionField("WorkId", s.WorkA.ToString()), new ActionField("VendorId", s.VendorA.ToString())]),
                new ActionPreview("Assign a vendor.", [new ActionFieldChange("VendorId", null, s.VendorA.ToString())]),
                "Autopilot.Manage", "Nonexistent.Capability", $"{recommendationId}:capability-test");
            store.ActionProposals.Add(proposal);
            await store.SaveChangesAsync();
            proposalId = proposal.Id;
        }

        await ApproveActionAsync(s, proposalId);

        var executeResponse = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/execute", null);
        Assert.Equal(HttpStatusCode.Forbidden, executeResponse.StatusCode);

        // Still Approved, not Failed — a capability gap is not a verdict on the proposal itself.
        await using var check = s.AutopilotAsAdmin(s.OrganizationA);
        var reloaded = await check.ActionProposals.FindAsync(s.OrganizationA, proposalId);
        Assert.Equal(ActionProposalStatus.Approved, reloaded!.Status);
    }

    [Fact]
    public async Task DraftCommunication_denies_execution_without_the_channel_consent_and_allows_it_with()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        // Scenario seeds ResidentA with SMS consent granted, Email never granted.
        var smsPropose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/communication-draft",
            new { recipientType = "Resident", recipientId = s.ResidentA, channel = "Sms", draftText = "Your appointment is confirmed." });
        var smsProposalId = (await smsPropose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await ApproveActionAsync(s, smsProposalId);
        var smsExecute = await s.Client.PostAsync($"/api/autopilot/actions/{smsProposalId}/execute", null);
        Assert.Equal(HttpStatusCode.OK, smsExecute.StatusCode);
        Assert.Equal("Executed", (await smsExecute.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var emailRecommendationId = await ApprovedRecommendationAsync(s);
        var emailPropose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{emailRecommendationId}/actions/communication-draft",
            new { recipientType = "Resident", recipientId = s.ResidentA, channel = "Email", draftText = "Your appointment is confirmed." });
        var emailProposalId = (await emailPropose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await ApproveActionAsync(s, emailProposalId);
        var emailExecute = await s.Client.PostAsync($"/api/autopilot/actions/{emailProposalId}/execute", null);
        Assert.Equal(HttpStatusCode.Conflict, emailExecute.StatusCode);
    }

    [Fact]
    public async Task RequestApproval_execution_creates_a_real_ApprovalRequest()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        var propose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/request-approval",
            new { subjectType = "WorkItem", subjectId = s.WorkA, note = "Needs a supervisor's sign-off." });
        var proposalId = (await propose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await ApproveActionAsync(s, proposalId);
        var execute = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/execute", null);
        Assert.Equal(HttpStatusCode.OK, execute.StatusCode);

        await using var operations = s.AdminStore(s.OrganizationA);
        Assert.True(await operations.ApprovalRequests.AnyAsync(x => x.SubjectType == "WorkItem" && x.SubjectId == s.WorkA));
    }

    [Fact]
    public async Task CreatePurchaseOrderDraft_execution_creates_a_draft_purchase_order()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);

        var propose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/purchase-order-draft",
            new { vendorId = s.VendorA, propertyId = s.PropertyA, number = "PO-AUTO-0001", amount = 450.00m, approvalThreshold = 1000.00m });
        var proposalId = (await propose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await ApproveActionAsync(s, proposalId);
        var execute = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/execute", null);
        Assert.Equal(HttpStatusCode.OK, execute.StatusCode);

        await using var operations = s.AdminStore(s.OrganizationA);
        Assert.True(await operations.PurchaseOrders.AnyAsync(x => x.Number == "PO-AUTO-0001" && x.Amount == 450.00m));
    }

    [Fact]
    public async Task AssignEmployee_and_CreateFollowUp_and_ScheduleWork_routes_accept_and_persist_a_proposal()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var employeeId = Guid.NewGuid();
        await using (var operations = s.AdminStore(s.OrganizationA))
        {
            operations.Employees.Add(new Employee(s.OrganizationA, employeeId, "Jamie Rivera", "jamie@example.test", null));
            await operations.SaveChangesAsync();
        }

        var r1 = await ApprovedRecommendationAsync(s);
        var employeeResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{r1}/actions/assign-employee",
            new { workId = s.WorkA, employeeId });
        Assert.Equal(HttpStatusCode.Created, employeeResponse.StatusCode);

        var r2 = await ApprovedRecommendationAsync(s);
        var followUpResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{r2}/actions/follow-up",
            new { propertyId = s.PropertyA, title = "Confirm the repair held", description = "Follow up in a week." });
        Assert.Equal(HttpStatusCode.Created, followUpResponse.StatusCode);

        var r3 = await ApprovedRecommendationAsync(s);
        var scheduleResponse = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{r3}/actions/schedule-work",
            new { workId = s.WorkA, scheduledStart = DateTimeOffset.UtcNow.AddDays(1), scheduledEnd = DateTimeOffset.UtcNow.AddDays(1).AddHours(2) });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
    }

    [Fact]
    public async Task A_recommendation_and_its_actions_are_invisible_to_a_different_organization()
    {
        await using var s = await fixture.CreateScenarioAsync();
        await s.LoginAsync();
        var recommendationId = await ApprovedRecommendationAsync(s);
        var propose = await s.Client.PostAsJsonAsync($"/api/autopilot/recommendations/{recommendationId}/actions/assign-vendor",
            new { workId = s.WorkA, vendorId = s.VendorA });
        var proposalId = (await propose.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var otherLogin = await s.AttemptLoginAsync(s.EmailB, s.OrganizationB);
        Assert.Equal(HttpStatusCode.NoContent, otherLogin.StatusCode);
        await s.RefreshCsrfAsync();

        var recommendationLookup = await s.Client.PostAsync($"/api/autopilot/recommendations/{recommendationId}/approve", null);
        Assert.Equal(HttpStatusCode.NotFound, recommendationLookup.StatusCode);
        var approveLookup = await s.Client.PostAsync($"/api/autopilot/actions/{proposalId}/approve", null);
        Assert.Equal(HttpStatusCode.NotFound, approveLookup.StatusCode);
    }
}
