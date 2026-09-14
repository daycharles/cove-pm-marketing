using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class ActionPayloadTests
{
    [Fact]
    public void Fields_are_carried_and_readable_by_name()
    {
        var payload = new ActionPayload([new ActionField("VendorId", "acme-plumbing"), new ActionField("Note", "urgent")]);
        Assert.Equal("acme-plumbing", payload["VendorId"]);
        Assert.Equal("urgent", payload["Note"]);
        Assert.Equal(2, payload.Fields.Count);
    }

    [Fact]
    public void Field_lookup_is_case_insensitive_and_missing_fields_return_null()
    {
        var payload = new ActionPayload([new ActionField("VendorId", "acme-plumbing")]);
        Assert.Equal("acme-plumbing", payload["vendorid"]);
        Assert.Null(payload["NotAField"]);
    }

    [Fact]
    public void An_empty_field_list_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ActionPayload([]));
    }

    [Fact]
    public void More_than_MaxFields_is_rejected()
    {
        var fields = Enumerable.Range(0, ActionPayload.MaxFields + 1)
            .Select(i => new ActionField($"Field{i}", "value"))
            .ToList();
        Assert.Throws<ArgumentException>(() => new ActionPayload(fields));
    }

    [Fact]
    public void Exactly_MaxFields_is_accepted()
    {
        var fields = Enumerable.Range(0, ActionPayload.MaxFields)
            .Select(i => new ActionField($"Field{i}", "value"))
            .ToList();
        var payload = new ActionPayload(fields);
        Assert.Equal(ActionPayload.MaxFields, payload.Fields.Count);
    }

    [Fact]
    public void Duplicate_field_names_are_rejected_case_insensitively()
    {
        Assert.Throws<ArgumentException>(() => new ActionPayload(
            [new ActionField("VendorId", "a"), new ActionField("vendorid", "b")]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_field_name_or_value_is_rejected(string blank)
    {
        Assert.Throws<ArgumentException>(() => new ActionPayload([new ActionField(blank, "value")]));
        Assert.Throws<ArgumentException>(() => new ActionPayload([new ActionField("Name", blank)]));
    }

    [Fact]
    public void A_null_field_list_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ActionPayload(null!));
    }

    [Fact]
    public void ActionPreview_carries_a_description_and_ordered_field_changes()
    {
        var preview = new ActionPreview(
            "Assign vendor Acme Plumbing to WO-00042",
            [
                new ActionFieldChange("VendorId", Before: null, After: "acme-plumbing"),
                new ActionFieldChange("Status", Before: "Unassigned", After: "Assigned"),
            ]);
        Assert.Equal(2, preview.Changes.Count);
        Assert.Null(preview.Changes[0].Before);
        Assert.Equal("Unassigned", preview.Changes[1].Before);
    }
}
