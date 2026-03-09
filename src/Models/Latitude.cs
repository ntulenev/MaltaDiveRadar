namespace Models;

/// <summary>
/// Represents a validated latitude in decimal degrees.
/// </summary>
public sealed record Latitude
{
    private Latitude(double degrees)
    {
        if (degrees is < -90D or > 90D)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                "Latitude must be in range [-90, 90].");
        }

        Degrees = degrees;
    }

    /// <summary>
    /// Gets latitude value in decimal degrees.
    /// </summary>
    public double Degrees { get; }

    /// <summary>
    /// Creates a validated latitude value.
    /// </summary>
    /// <param name="degrees">Latitude in decimal degrees.</param>
    /// <returns>Validated latitude value object.</returns>
    public static Latitude FromDegrees(double degrees)
    {
        return new Latitude(degrees);
    }
}
