using PropFlow.Infrastructure.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class JsonStructuredOutputValidatorTests
{
    private readonly JsonStructuredOutputValidator validator = new();

    [Fact]
    public void Valid_json_with_every_required_field_passes()
    {
        var result = validator.Validate("""{"summary": "Payment is overdue.", "confidence": "high"}""", ["summary", "confidence"]);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Empty_output_fails()
    {
        var result = validator.Validate("", ["summary"]);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Malformed_json_fails_with_a_parse_error()
    {
        var result = validator.Validate("{not json", ["summary"]);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not valid JSON"));
    }

    [Fact]
    public void A_json_array_instead_of_an_object_fails()
    {
        var result = validator.Validate("""["summary"]""", ["summary"]);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_missing_required_field_fails()
    {
        var result = validator.Validate("""{"summary": "Payment is overdue."}""", ["summary", "confidence"]);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("confidence"));
    }

    [Fact]
    public void An_empty_string_value_for_a_required_field_fails()
    {
        var result = validator.Validate("""{"summary": "   "}""", ["summary"]);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("summary"));
    }

    [Fact]
    public void A_null_value_for_a_required_field_fails()
    {
        var result = validator.Validate("""{"summary": null}""", ["summary"]);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_non_string_non_null_value_is_accepted_as_present()
    {
        var result = validator.Validate("""{"confidence": 0.9}""", ["confidence"]);
        Assert.True(result.IsValid);
    }
}
