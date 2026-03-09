namespace Models;

/// <summary>
/// Represents wave height in meters.
/// </summary>
public sealed record WaveHeight
{
    private WaveHeight(double meters)
    {
        if (meters < 0D)
        {
            throw new ArgumentOutOfRangeException(
                nameof(meters),
                "Wave height must be non-negative.");
        }

        Meters = meters;
    }

    /// <summary>
    /// Gets wave height in meters.
    /// </summary>
    public double Meters { get; }

    /// <summary>
    /// Creates a validated wave-height value from meters.
    /// </summary>
    /// <param name="meters">Wave height in meters.</param>
    /// <returns>Validated wave-height value object.</returns>
    public static WaveHeight FromMeters(double meters)
    {
        return new WaveHeight(meters);
    }

    /// <summary>
    /// Gets a value indicating whether waves are high.
    /// </summary>
    /// <returns>
    /// True when wave height is greater than the high-wave threshold.
    /// </returns>
    public bool IsHighWaves()
    {
        return Meters > HIGH_WAVES_THRESHOLD_M;
    }

    /// <summary>
    /// Gets a value indicating whether waves are at least moderate.
    /// </summary>
    /// <returns>
    /// True when wave height is greater than or equal to moderate threshold.
    /// </returns>
    public bool IsModerateWaves()
    {
        return Meters >= MODERATE_WAVES_THRESHOLD_M;
    }

    private const double HIGH_WAVES_THRESHOLD_M = 1.2D;
    private const double MODERATE_WAVES_THRESHOLD_M = 0.5D;
}
