namespace Models;

/// <summary>
/// Aggregated normalized weather snapshot for a dive site.
/// </summary>
public sealed class WeatherSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSnapshot"/> class.
    /// </summary>
    /// <param name="diveSiteId">Dive-site identifier.</param>
    /// <param name="diveSiteName">Dive-site display name.</param>
    /// <param name="island">Island display name.</param>
    /// <param name="airTemperatureC">Air temperature in Celsius.</param>
    /// <param name="waterTemperatureC">Water temperature in Celsius.</param>
    /// <param name="windSpeedMps">Wind speed in meters per second.</param>
    /// <param name="windDirectionDeg">Wind direction in degrees.</param>
    /// <param name="waveHeightM">Wave height in meters.</param>
    /// <param name="seaStateText">Sea state text.</param>
    /// <param name="conditionStatus">Classified condition status.</param>
    /// <param name="conditionSummary">Short summary text.</param>
    /// <param name="observationTimeUtc">Observation time in UTC.</param>
    /// <param name="lastUpdatedUtc">Last successful update timestamp in UTC.</param>
    /// <param name="lastRefreshAttemptUtc">Last refresh attempt timestamp in UTC.</param>
    /// <param name="sourceProvider">Source provider label.</param>
    /// <param name="isStale">Whether snapshot is stale.</param>
    /// <param name="providerSnapshots">Provider snapshots from refresh cycle.</param>
    public WeatherSnapshot(
        DiveSiteId diveSiteId,
        string diveSiteName,
        string island,
        double? airTemperatureC,
        double? waterTemperatureC,
        double? windSpeedMps,
        int? windDirectionDeg,
        double? waveHeightM,
        string? seaStateText,
        SeaConditionStatus conditionStatus,
        string conditionSummary,
        DateTimeOffset? observationTimeUtc,
        DateTimeOffset lastUpdatedUtc,
        DateTimeOffset lastRefreshAttemptUtc,
        string sourceProvider,
        bool isStale,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots)
    {
        ArgumentNullException.ThrowIfNull(diveSiteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(diveSiteName);
        ArgumentException.ThrowIfNullOrWhiteSpace(island);
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProvider);
        ArgumentNullException.ThrowIfNull(providerSnapshots);

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

        if (windDirectionDeg is < 0 or > 359)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windDirectionDeg),
                "Wind direction must be in range [0, 359].");
        }

        if (windSpeedMps < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windSpeedMps),
                "Wind speed must be non-negative.");
        }

        if (waveHeightM < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waveHeightM),
                "Wave height must be non-negative.");
        }

        DiveSiteId = diveSiteId;
        DiveSiteName = diveSiteName.Trim();
        Island = island.Trim();
        AirTemperatureC = airTemperatureC;
        WaterTemperatureC = waterTemperatureC;
        WindSpeedMps = windSpeedMps;
        WindDirectionDeg = windDirectionDeg;
        WaveHeightM = waveHeightM;
        SeaStateText = string.IsNullOrWhiteSpace(seaStateText)
            ? null
            : seaStateText.Trim();
        ConditionStatus = conditionStatus;
        ConditionSummary = conditionSummary.Trim();
        ObservationTimeUtc = observationTimeUtc;
        LastUpdatedUtc = lastUpdatedUtc;
        LastRefreshAttemptUtc = lastRefreshAttemptUtc;
        SourceProvider = sourceProvider.Trim();
        IsStale = isStale;
        ProviderSnapshots = providerSnapshots.ToArray();
    }

    /// <summary>
    /// Gets dive-site ID.
    /// </summary>
    public DiveSiteId DiveSiteId { get; }

    /// <summary>
    /// Gets dive-site display name.
    /// </summary>
    public string DiveSiteName { get; }

    /// <summary>
    /// Gets island display name.
    /// </summary>
    public string Island { get; }

    /// <summary>
    /// Gets air temperature in Celsius.
    /// </summary>
    public double? AirTemperatureC { get; }

    /// <summary>
    /// Gets water temperature in Celsius.
    /// </summary>
    public double? WaterTemperatureC { get; }

    /// <summary>
    /// Gets wind speed in meters per second.
    /// </summary>
    public double? WindSpeedMps { get; }

    /// <summary>
    /// Gets wind direction in degrees.
    /// </summary>
    public int? WindDirectionDeg { get; }

    /// <summary>
    /// Gets wave height in meters.
    /// </summary>
    public double? WaveHeightM { get; }

    /// <summary>
    /// Gets sea state text.
    /// </summary>
    public string? SeaStateText { get; }

    /// <summary>
    /// Gets summarized condition status.
    /// </summary>
    public SeaConditionStatus ConditionStatus { get; }

    /// <summary>
    /// Gets short summary text.
    /// </summary>
    public string ConditionSummary { get; }

    /// <summary>
    /// Gets the latest observation time in UTC used in this snapshot.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; }

    /// <summary>
    /// Gets the timestamp when this snapshot was last refreshed in UTC.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; }

    /// <summary>
    /// Gets timestamp of the last refresh attempt in UTC.
    /// </summary>
    public DateTimeOffset LastRefreshAttemptUtc { get; }

    /// <summary>
    /// Gets provider name(s) that supplied selected values.
    /// </summary>
    public string SourceProvider { get; }

    /// <summary>
    /// Gets a value indicating whether this snapshot is stale.
    /// </summary>
    public bool IsStale { get; }

    /// <summary>
    /// Gets provider snapshots captured during the refresh cycle.
    /// </summary>
    public IReadOnlyList<WeatherProviderSnapshot> ProviderSnapshots { get; }

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

        return new WeatherSnapshot(
            site.Id,
            site.Name,
            site.Island,
            null,
            null,
            null,
            null,
            null,
            null,
            SeaConditionStatus.Unknown,
            "No provider data available",
            null,
            refreshAttemptUtc,
            refreshAttemptUtc,
            "Unavailable",
            true,
            providerSnapshots);
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
            DiveSiteId,
            DiveSiteName,
            Island,
            AirTemperatureC,
            WaterTemperatureC,
            WindSpeedMps,
            WindDirectionDeg,
            WaveHeightM,
            SeaStateText,
            ConditionStatus,
            ConditionSummary,
            ObservationTimeUtc,
            LastUpdatedUtc,
            refreshAttemptUtc,
            SourceProvider,
            true,
            ProviderSnapshots);
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
            DiveSiteId,
            DiveSiteName,
            Island,
            AirTemperatureC,
            WaterTemperatureC,
            WindSpeedMps,
            WindDirectionDeg,
            WaveHeightM,
            SeaStateText,
            ConditionStatus,
            ConditionSummary,
            ObservationTimeUtc,
            LastUpdatedUtc,
            LastRefreshAttemptUtc,
            SourceProvider,
            IsStale,
            providerSnapshots);
    }
}
