using System.Globalization;

namespace PropFlow.Domain.Autopilot;

// A small shared humanizer for the day-count and money summaries every CPM-8.02 rule file
// writes ("due in 3 days", "14 days overdue", "$1,204.50"). Mirrors AttentionRules.Humanize's
// shape but works in whole days — every signal in this namespace is computed from DateOnly
// deadlines, not the hour-granularity DateTimeOffset spans Attention's SLA-budget rule needs.
// Invariant culture throughout: a finding summary is stored text (AutopilotFinding.Summary),
// read back on whatever machine renders the daily brief later, not formatted for the culture of
// the machine that happened to run the analyzer.
internal static class AutopilotFormatting
{
    internal static string Days(int count) => $"{count} day{(count == 1 ? "" : "s")}";
    internal static string Money(decimal amount) => $"${amount.ToString("N2", CultureInfo.InvariantCulture)}";
    internal static string Percent(decimal fraction) => $"{(fraction * 100m).ToString("F0", CultureInfo.InvariantCulture)}%";
}
