namespace Models;

/// <summary>
/// Aggregated normalized weather snapshot for a dive site.
/// </summary>
public sealed class WeatherSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSnapshot"/> class.
    /// </summary>
    /// <param name="site">Grouped dive-site identity values.</param>
    /// <param name="metrics">Grouped weather metric values.</param>
    /// <param name="condition">Grouped condition classification values.</param>
    /// <param name="timing">Grouped timing metadata.</param>
    /// <param name="provenance">Grouped provider provenance metadata.</param>
    public WeatherSnapshot(
        DiveSiteSnapshotInfo site,
        WeatherMetrics metrics,
        WeatherSnapshotCondition condition,
        WeatherSnapshotTiming timing,
        WeatherSnapshotProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(timing);
        ArgumentNullException.ThrowIfNull(provenance);

        Site = site;
        Metrics = metrics;
        Condition = condition;
        Timing = timing;
        Provenance = provenance;
    }

    /// <summary>
    /// Gets grouped dive-site identity values.
    /// </summary>
    public DiveSiteSnapshotInfo Site { get; }

    /// <summary>
    /// Gets grouped weather metric values.
    /// </summary>
    public WeatherMetrics Metrics { get; }

    /// <summary>
    /// Gets grouped condition classification values.
    /// </summary>
    public WeatherSnapshotCondition Condition { get; }

    /// <summary>
    /// Gets grouped timing metadata.
    /// </summary>
    public WeatherSnapshotTiming Timing { get; }

    /// <summary>
    /// Gets grouped provider provenance metadata.
    /// </summary>
    public WeatherSnapshotProvenance Provenance { get; }

    /// <summary>
    /// Gets dive-site ID.
    /// </summary>
    public DiveSiteId DiveSiteId => Site.DiveSiteId;

    /// <summary>
    /// Gets dive-site display name.
    /// </summary>
    public DiveSiteName DiveSiteName => Site.DiveSiteName;

    /// <summary>
    /// Gets island display name.
    /// </summary>
    public IslandName Island => Site.Island;

    /// <summary>
    /// Gets air-temperature value.
    /// </summary>
    public AirTemperature? AirTemperatureC => Metrics.AirTemperatureC;

    /// <summary>
    /// Gets water-temperature value.
    /// </summary>
    public WaterTemperature? WaterTemperatureC => Metrics.WaterTemperatureC;

    /// <summary>
    /// Gets wind-speed value.
    /// </summary>
    public WindSpeed? WindSpeedMps => Metrics.WindSpeedMps;

    /// <summary>
    /// Gets wind-direction value.
    /// </summary>
    public WindDirection? WindDirectionDeg => Metrics.WindDirectionDeg;

    /// <summary>
    /// Gets wave-height value.
    /// </summary>
    public WaveHeight? WaveHeightM => Metrics.WaveHeightM;

    /// <summary>
    /// Gets sea-state text.
    /// </summary>
    public SeaStateText? SeaStateText => Metrics.SeaStateText;

    /// <summary>
    /// Gets summarized condition status.
    /// </summary>
    public SeaConditionStatus ConditionStatus => Condition.Status;

    /// <summary>
    /// Gets short summary value.
    /// </summary>
    public SeaConditionSummary ConditionSummary => Condition.Summary;

    /// <summary>
    /// Gets the latest observation time in UTC used in this snapshot.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc => Timing.ObservationTimeUtc;

    /// <summary>
    /// Gets the timestamp when this snapshot was last refreshed in UTC.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc => Timing.LastUpdatedUtc;

    /// <summary>
    /// Gets timestamp of the last refresh attempt in UTC.
    /// </summary>
    public DateTimeOffset LastRefreshAttemptUtc => Timing.LastRefreshAttemptUtc;

    /// <summary>
    /// Gets provider name(s) that supplied selected values.
    /// </summary>
    public SourceProvider SourceProvider => Provenance.SourceProvider;

    /// <summary>
    /// Gets a value indicating whether this snapshot is stale.
    /// </summary>
    public bool IsStale => Provenance.IsStale;

    /// <summary>
    /// Gets provider snapshots captured during the refresh cycle.
    /// </summary>
    public IReadOnlyList<WeatherProviderSnapshot> ProviderSnapshots =>
        Provenance.ProviderSnapshots;

    /// <summary>
    /// Creates an unavailable snapshot when all providers fail.
    /// </summary>
    /// <param name="site">Dive site.</param>
    /// <param name="refreshAttemptUtc">Refresh attempt timestamp in UTC.</param>
    /// <param name="providerSnapshots">Provider snapshots from the failed cycle.</param>
    /// <returns>Unavailable stale snapshot for the site.</returns>
    public static WeatherSnapshot CreateUnavailable(
        DiveSite site,
        DateTimeOffset refreshAttemptUtc,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(providerSnapshots);

        if (refreshAttemptUtc == default)
        {
            throw new ArgumentException(
                "Refresh attempt timestamp must be defined.",
                nameof(refreshAttemptUtc));
        }

        var siteInfo = DiveSiteSnapshotInfo.FromDiveSite(site);
        var condition = new WeatherSnapshotCondition(
            SeaConditionStatus.Unknown,
            SeaConditionSummary.From("No provider data available"));
        var timing = new WeatherSnapshotTiming(
            null,
            refreshAttemptUtc,
            refreshAttemptUtc);
        var provenance = new WeatherSnapshotProvenance(
            SourceProvider.FromLabel("Unavailable"),
            true,
            providerSnapshots);

        return new WeatherSnapshot(
            siteInfo,
            WeatherMetrics.Empty,
            condition,
            timing,
            provenance);
    }

    /// <summary>
    /// Creates a copy of this snapshot with stale status enabled.
    /// </summary>
    /// <param name="refreshAttemptUtc">Refresh attempt timestamp in UTC.</param>
    /// <returns>Updated stale snapshot.</returns>
    public WeatherSnapshot MarkAsStale(DateTimeOffset refreshAttemptUtc)
    {
        if (refreshAttemptUtc == default)
        {
            throw new ArgumentException(
                "Refresh attempt timestamp must be defined.",
                nameof(refreshAttemptUtc));
        }

        return new WeatherSnapshot(
            Site,
            Metrics,
            Condition,
            Timing.WithRefreshAttempt(refreshAttemptUtc),
            Provenance.MarkStale());
    }

    /// <summary>
    /// Creates a copy of this snapshot with provider snapshots replaced.
    /// </summary>
    /// <param name="providerSnapshots">Provider snapshots.</param>
    /// <returns>Updated snapshot.</returns>
    public WeatherSnapshot WithProviderSnapshots(
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots)
    {
        ArgumentNullException.ThrowIfNull(providerSnapshots);

        return new WeatherSnapshot(
            Site,
            Metrics,
            Condition,
            Timing,
            Provenance.WithProviderSnapshots(providerSnapshots));
    }
}
