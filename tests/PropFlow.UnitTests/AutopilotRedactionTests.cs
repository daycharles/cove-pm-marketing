using PropFlow.Domain.Autopilot;
using Xunit;

namespace PropFlow.UnitTests;

public sealed class AutopilotRedactionTests
{
    [Fact]
    public void An_email_address_is_redacted()
    {
        Assert.Equal("Contact [redacted-email] for access.", AutopilotRedaction.Redact("Contact dana@example.test for access."));
    }

    [Fact]
    public void A_phone_number_is_redacted()
    {
        Assert.Equal("Call [redacted-phone] for details.", AutopilotRedaction.Redact("Call 555-010-0101 for details."));
    }

    [Fact]
    public void Text_with_neither_pattern_is_unchanged()
    {
        const string text = "Rooftop HVAC unit 3";
        Assert.Equal(text, AutopilotRedaction.Redact(text));
    }

    [Fact]
    public void Both_patterns_in_the_same_text_are_both_redacted()
    {
        var result = AutopilotRedaction.Redact("Reach dana@example.test or 555-010-0101.");
        Assert.Contains("[redacted-email]", result);
        Assert.Contains("[redacted-phone]", result);
    }
}
