namespace Models;

/// <summary>
/// Represents validated human-readable sea-state text.
/// </summary>
public sealed record SeaStateText
{
    private SeaStateText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets sea-state text value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated sea-state text value object.
    /// </summary>
    /// <param name="value">Raw sea-state text.</param>
    /// <returns>Validated sea-state text value object.</returns>
    public static SeaStateText From(string value)
    {
        return new SeaStateText(value);
    }

    /// <summary>
    /// Gets a value indicating whether text hints rough conditions.
    /// </summary>
    /// <returns>True when rough-condition keywords are detected.</returns>
    public bool IndicatesRoughConditions()
    {
        return RoughKeywords.Any(
            keyword => Value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly string[] RoughKeywords =
    [
        "rough",
        "very rough",
        "strong",
        "chop",
    ];
}
