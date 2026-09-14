namespace PropFlow.Domain.Autopilot;

/// <summary>One named field of a governed action's typed payload — "VendorId", "a1b2c3...".
/// Mirrors <see cref="CalculationInput"/>'s shape: a name/value pair is enough structure for a
/// payload nothing executes yet (CPM-8.08 supplies the first adapters), the same reason
/// <c>CalculationInput</c> stays a string, not a typed union, in <see cref="EvidenceCandidate"/>.</summary>
public sealed record ActionField(string Name, string Value);

/// <summary>One line of a preview/diff — the value a field would move from and to if the action
/// executes. <see cref="Before"/> is null when the field has no current value (the action creates
/// something rather than changing it).</summary>
public sealed record ActionFieldChange(string Field, string? Before, string After);

/// <summary>
/// The typed, executable payload of a governed action — <see cref="AutopilotActionProposal.PayloadSummary"/>
/// (CPM-8.01's one-sentence description for a list row) made structured. A hand-rolled shape
/// check (unique, bounded field names — the same "no JSON-Schema library" call
/// <c>JsonStructuredOutputValidator</c>'s own comment explains), not a closed field catalog:
/// CPM-8.08's adapter catalog does not exist yet, so this cannot know which field names are legal
/// for "AssignVendor" versus "ScheduleWork" — that belongs to whichever adapter builds the
/// payload, the same way <c>SignalType</c>/<c>ActionType</c> stay open text until their own
/// catalogs exist.
/// </summary>
public sealed class ActionPayload
{
    public const int MaxFields = 20;
    public const int FieldNameMaxLength = 100;
    public const int FieldValueMaxLength = 1000;

    public ActionPayload(IReadOnlyList<ActionField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Count == 0)
            throw new ArgumentException("A payload must carry at least one field.", nameof(fields));
        if (fields.Count > MaxFields)
            throw new ArgumentException($"A payload may carry at most {MaxFields} fields.", nameof(fields));

        var validated = new List<ActionField>(fields.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var name = AutopilotText.RequireSingleLine(field.Name, nameof(fields), FieldNameMaxLength);
            var value = AutopilotText.RequireSingleLine(field.Value, nameof(fields), FieldValueMaxLength);
            if (!seen.Add(name))
                throw new ArgumentException($"Duplicate field name '{name}'.", nameof(fields));
            validated.Add(new ActionField(name, value));
        }
        Fields = validated;
    }

    public IReadOnlyList<ActionField> Fields { get; }

    /// <summary>The value of the named field, or null if the payload does not carry one — never
    /// throws, so a caller can probe an optional field without a defensive TryGet.</summary>
    public string? this[string name] =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
}

/// <summary>
/// What a reviewer sees before deciding: a human-readable description plus the field-level
/// before/after this action would apply. Distinct from <see cref="AutopilotActionProposal.PayloadSummary"/>
/// (one sentence for a list row) — this is the itemized version for the moment someone is
/// actually deciding, and it is why "preview/diff" is its own line in CPM-8.07's scope rather
/// than folded into the summary CPM-8.01 already had.
/// </summary>
public sealed record ActionPreview(string Description, IReadOnlyList<ActionFieldChange> Changes);
