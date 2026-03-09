namespace Models;

/// <summary>
/// Represents latest weather read-model data for all dive sites.
/// </summary>
public sealed class LatestWeather
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LatestWeather"/> class.
    /// </summary>
    /// <param name="lastRefreshUtc">Last refresh completion timestamp in UTC.</param>
    /// <param name="snapshots">Latest snapshots sorted for presentation.</param>
    public LatestWeather(
        DateTimeOffset? lastRefreshUtc,
        IReadOnlyCollection<WeatherSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        LastRefreshUtc = lastRefreshUtc;
        Snapshots = snapshots.ToArray();
    }

    /// <summary>
    /// Gets last refresh completion timestamp in UTC.
    /// </summary>
    public DateTimeOffset? LastRefreshUtc { get; }

    /// <summary>
    /// Gets latest snapshots.
    /// </summary>
    public IReadOnlyCollection<WeatherSnapshot> Snapshots { get; }
}
