using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotActionProposalTests
{
    private static readonly Guid Proposer = Guid.NewGuid();

    private static ActionPayload Payload() => new([new ActionField("VendorId", Guid.NewGuid().ToString())]);
    private static ActionPreview Preview() => new(
        "Assign vendor Acme Plumbing to WO-00042",
        [new ActionFieldChange("VendorId", Before: null, After: "acme-plumbing")]);

    private static AutopilotActionProposal Create(
        ActionPayload? payload = null,
        ActionPreview? preview = null,
        string requiredApprovalCapability = "Autopilot.Manage",
        string requiredExecutionCapability = "Work.AssignVendor",
        string idempotencyKey = "idem-key-1",
        string? requiredConsentType = null,
        string? concurrencyToken = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "AssignVendor",
        "Assign vendor Acme Plumbing to WO-00042", Proposer, DateTimeOffset.UtcNow,
        payload ?? Payload(), preview ?? Preview(),
        requiredApprovalCapability, requiredExecutionCapability, idempotencyKey,
        requiredConsentType, concurrencyToken);

    [Fact]
    public void A_new_proposal_is_proposed_with_no_execution_state()
    {
        var proposal = Create();
        Assert.Equal(ActionProposalStatus.Proposed, proposal.Status);
        Assert.Null(proposal.ExecutedAt);
        Assert.False(proposal.IsTerminal);
    }

    [Fact]
    public void An_empty_action_type_or_payload_summary_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AutopilotActionProposal(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "x", Proposer, DateTimeOffset.UtcNow,
            Payload(), Preview(), "Autopilot.Manage", "Work.AssignVendor", "idem-key-1"));
        Assert.Throws<ArgumentException>(() => new AutopilotActionProposal(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "AssignVendor", "", Proposer, DateTimeOffset.UtcNow,
            Payload(), Preview(), "Autopilot.Manage", "Work.AssignVendor", "idem-key-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_required_capability_is_rejected(string blank)
    {
        Assert.Throws<ArgumentException>(() => Create(requiredApprovalCapability: blank));
        Assert.Throws<ArgumentException>(() => Create(requiredExecutionCapability: blank));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_idempotency_key_is_rejected(string blank)
    {
        Assert.Throws<ArgumentException>(() => Create(idempotencyKey: blank));
    }

    [Fact]
    public void RequiredConsentType_and_ConcurrencyToken_are_optional_and_default_to_null()
    {
        var proposal = Create();
        Assert.Null(proposal.RequiredConsentType);
        Assert.Null(proposal.ConcurrencyToken);
    }

    [Fact]
    public void RequiredConsentType_and_ConcurrencyToken_are_carried_when_given()
    {
        var proposal = Create(requiredConsentType: "ResidentSms", concurrencyToken: "xmin:12345");
        Assert.Equal("ResidentSms", proposal.RequiredConsentType);
        Assert.Equal("xmin:12345", proposal.ConcurrencyToken);
    }

    [Fact]
    public void Payload_and_preview_are_carried_as_given()
    {
        var payload = Payload();
        var preview = Preview();
        var proposal = Create(payload, preview);
        Assert.Same(payload, proposal.Payload);
        Assert.Same(preview, proposal.Preview);
    }

    [Fact]
    public void Approve_then_MarkExecuted_records_the_outcome()
    {
        var proposal = Create();
        var decider = Guid.NewGuid();
        proposal.Approve(decider, DateTimeOffset.UtcNow);
        proposal.MarkExecuted("Vendor assigned", DateTimeOffset.UtcNow);
        Assert.Equal(ActionProposalStatus.Executed, proposal.Status);
        Assert.Equal("Vendor assigned", proposal.ExecutionOutcome);
        Assert.True(proposal.IsTerminal);
    }

    [Fact]
    public void Approve_then_MarkFailed_records_the_reason()
    {
        var proposal = Create();
        proposal.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        proposal.MarkFailed("Adapter rejected the payload", DateTimeOffset.UtcNow);
        Assert.Equal(ActionProposalStatus.Failed, proposal.Status);
        Assert.True(proposal.IsTerminal);
    }

    [Fact]
    public void MarkExecuted_and_MarkFailed_require_prior_approval()
    {
        var proposal = Create();
        Assert.Throws<InvalidOperationException>(() => proposal.MarkExecuted("x", DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => proposal.MarkFailed("x", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void The_proposer_cannot_decide_their_own_action()
    {
        var proposal = Create();
        Assert.Throws<InvalidOperationException>(() => proposal.Approve(Proposer, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => proposal.Reject(Proposer, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_rejected_proposal_cannot_be_executed()
    {
        var proposal = Create();
        proposal.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => proposal.MarkExecuted("x", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_decided_proposal_cannot_be_decided_again()
    {
        var proposal = Create();
        proposal.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => proposal.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => proposal.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void An_executed_proposal_cannot_be_marked_failed_afterward()
    {
        var proposal = Create();
        proposal.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        proposal.MarkExecuted("Done", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => proposal.MarkFailed("too late", DateTimeOffset.UtcNow));
    }
}
