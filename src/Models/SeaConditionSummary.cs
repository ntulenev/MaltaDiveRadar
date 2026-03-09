namespace Models;

/// <summary>
/// Represents a validated sea-condition summary for dive guidance.
/// </summary>
public sealed record SeaConditionSummary
{
    private SeaConditionSummary(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets summary text value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated sea-condition summary value object.
    /// </summary>
    /// <param name="value">Raw summary text.</param>
    /// <returns>Validated sea-condition summary value object.</returns>
    public static SeaConditionSummary From(string value)
    {
        return new SeaConditionSummary(value);
    }

    /// <summary>
    /// Gets a value indicating whether summary suggests caution or rough conditions.
    /// </summary>
    /// <returns>True when caution/rough keywords are detected.</returns>
    public bool RequiresCaution()
    {
        return CautionKeywords.Any(
            keyword => Value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly string[] CautionKeywords =
    [
        "caution",
        "rough",
        "chop",
        "strong",
    ];
}
