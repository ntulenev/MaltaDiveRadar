namespace Models;

/// <summary>
/// Represents provider priority where lower value means higher preference.
/// </summary>
public sealed record ProviderPriority
{
    private ProviderPriority(int value)
    {
        if (value is < MIN_VALUE or > MAX_VALUE)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Provider priority must be in range [1, 99].");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the numeric priority value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a validated provider-priority value.
    /// </summary>
    /// <param name="value">Raw provider-priority value.</param>
    /// <returns>Validated provider-priority value object.</returns>
    public static ProviderPriority From(int value)
    {
        return new ProviderPriority(value);
    }

    /// <summary>
    /// Gets a value indicating whether this priority is higher preference.
    /// </summary>
    /// <param name="other">Other priority value.</param>
    /// <returns>True when this priority has higher preference.</returns>
    public bool IsHigherPreferenceThan(ProviderPriority other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Value < other.Value;
    }

    private const int MIN_VALUE = 1;
    private const int MAX_VALUE = 99;
}
