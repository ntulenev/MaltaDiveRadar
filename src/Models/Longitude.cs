namespace Models;

/// <summary>
/// Represents a validated longitude in decimal degrees.
/// </summary>
public sealed record Longitude
{
    private Longitude(double degrees)
    {
        if (degrees is < -180D or > 180D)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                "Longitude must be in range [-180, 180].");
        }

        Degrees = degrees;
    }

    /// <summary>
    /// Gets longitude value in decimal degrees.
    /// </summary>
    public double Degrees { get; }

    /// <summary>
    /// Creates a validated longitude value.
    /// </summary>
    /// <param name="degrees">Longitude in decimal degrees.</param>
    /// <returns>Validated longitude value object.</returns>
    public static Longitude FromDegrees(double degrees)
    {
        return new Longitude(degrees);
    }
}
