namespace Models;

/// <summary>
/// Represents a validated wind direction in degrees.
/// </summary>
public sealed record WindDirection
{
    private WindDirection(int degrees)
    {
        if (degrees is < 0 or > 359)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                "Wind direction must be in range [0, 359].");
        }

        Degrees = degrees;
    }

    /// <summary>
    /// Gets wind direction in degrees.
    /// </summary>
    public int Degrees { get; }

    /// <summary>
    /// Creates a validated wind-direction value.
    /// </summary>
    /// <param name="degrees">Wind direction in degrees.</param>
    /// <returns>Validated wind-direction value object.</returns>
    public static WindDirection FromDegrees(int degrees)
    {
        return new WindDirection(degrees);
    }
}
