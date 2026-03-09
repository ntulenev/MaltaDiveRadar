namespace Models;

/// <summary>
/// Identifies a dive site.
/// </summary>
public sealed record DiveSiteId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiveSiteId"/> class.
    /// </summary>
    /// <param name="value">Numeric identifier value.</param>
    public DiveSiteId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Dive site ID must be positive.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the numeric identifier value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a validated identifier from a raw numeric value.
    /// </summary>
    /// <param name="value">Numeric identifier value.</param>
    /// <returns>Validated identifier.</returns>
    public static DiveSiteId FromInt(int value)
    {
        return new DiveSiteId(value);
    }
}
