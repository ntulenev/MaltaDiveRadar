namespace Models;

/// <summary>
/// Groups snapshot timing metadata.
/// </summary>
public sealed class WeatherSnapshotTiming
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSnapshotTiming"/> class.
    /// </summary>
    /// <param name="observationTimeUtc">Observation timestamp in UTC.</param>
    /// <param name="lastUpdatedUtc">Last successful update timestamp in UTC.</param>
    /// <param name="lastRefreshAttemptUtc">Last refresh-attempt timestamp in UTC.</param>
    public WeatherSnapshotTiming(
        DateTimeOffset? observationTimeUtc,
        DateTimeOffset lastUpdatedUtc,
        DateTimeOffset lastRefreshAttemptUtc)
    {
        if (lastUpdatedUtc == default)
        {
            throw new ArgumentException(
                "Last updated timestamp must be defined.",
                nameof(lastUpdatedUtc));
        }

        if (lastRefreshAttemptUtc == default)
        {
            throw new ArgumentException(
                "Last refresh attempt timestamp must be defined.",
                nameof(lastRefreshAttemptUtc));
        }

        ObservationTimeUtc = observationTimeUtc;
        LastUpdatedUtc = lastUpdatedUtc;
        LastRefreshAttemptUtc = lastRefreshAttemptUtc;
    }

    /// <summary>
    /// Gets observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; }

    /// <summary>
    /// Gets last successful update timestamp in UTC.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; }

    /// <summary>
    /// Gets last refresh-attempt timestamp in UTC.
    /// </summary>
    public DateTimeOffset LastRefreshAttemptUtc { get; }

    /// <summary>
    /// Creates a copy with updated refresh-attempt timestamp.
    /// </summary>
    /// <param name="refreshAttemptUtc">Refresh-attempt timestamp in UTC.</param>
    /// <returns>Updated timing object.</returns>
    public WeatherSnapshotTiming WithRefreshAttempt(DateTimeOffset refreshAttemptUtc)
    {
        return new WeatherSnapshotTiming(
            ObservationTimeUtc,
            LastUpdatedUtc,
            refreshAttemptUtc);
    }
}
