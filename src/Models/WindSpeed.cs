namespace Models;

/// <summary>
/// Represents wind speed in meters per second.
/// </summary>
public sealed record WindSpeed
{
    private const double STRONG_WIND_THRESHOLD_MPS = 9D;
    private const double MODERATE_WIND_THRESHOLD_MPS = 5D;

    private WindSpeed(double metersPerSecond)
    {
        if (metersPerSecond < 0D)
        {
            throw new ArgumentOutOfRangeException(
                nameof(metersPerSecond),
                "Wind speed must be non-negative.");
        }

        MetersPerSecond = metersPerSecond;
    }

    /// <summary>
    /// Gets wind speed in meters per second.
    /// </summary>
    public double MetersPerSecond { get; }

    /// <summary>
    /// Creates a validated wind-speed value from meters per second.
    /// </summary>
    /// <param name="metersPerSecond">Wind speed in meters per second.</param>
    /// <returns>Validated wind-speed value object.</returns>
    public static WindSpeed FromMetersPerSecond(double metersPerSecond)
    {
        return new WindSpeed(metersPerSecond);
    }

    /// <summary>
    /// Gets a value indicating whether wind is strong.
    /// </summary>
    /// <returns>
    /// True when wind speed is greater than the strong-wind threshold.
    /// </returns>
    public bool IsStrongWind()
    {
        return MetersPerSecond > STRONG_WIND_THRESHOLD_MPS;
    }

    /// <summary>
    /// Gets a value indicating whether wind is at least moderate.
    /// </summary>
    /// <returns>
    /// True when wind speed is greater than or equal to moderate threshold.
    /// </returns>
    public bool IsModerateWind()
    {
        return MetersPerSecond >= MODERATE_WIND_THRESHOLD_MPS;
    }
}
