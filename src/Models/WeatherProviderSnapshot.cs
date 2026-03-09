namespace Models;

/// <summary>
/// Provider-normalized weather and marine metrics for a single fetch call.
/// </summary>
public sealed class WeatherProviderSnapshot
{
    private WeatherProviderSnapshot(
        WeatherProviderMetadata provider,
        bool isSuccess,
        WeatherMetrics metrics,
        ProviderFetchInfo fetchInfo,
        string? error)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(fetchInfo);

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException(
                "Failure snapshots must define an error message.",
                nameof(error));
        }

        Provider = provider;
        IsSuccess = isSuccess;
        Metrics = metrics;
        FetchInfo = fetchInfo;
        Error = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Trim();
    }

    /// <summary>
    /// Gets grouped provider metadata.
    /// </summary>
    public WeatherProviderMetadata Provider { get; }

    /// <summary>
    /// Gets grouped weather metrics.
    /// </summary>
    public WeatherMetrics Metrics { get; }

    /// <summary>
    /// Gets grouped provider fetch metadata.
    /// </summary>
    public ProviderFetchInfo FetchInfo { get; }

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public ProviderName ProviderName => Provider.ProviderName;

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    public ProviderPriority Priority => Provider.Priority;

    /// <summary>
    /// Gets a value indicating whether this provider can return marine data.
    /// </summary>
    public bool SupportsMarineData => Provider.SupportsMarineData;

    /// <summary>
    /// Gets a value indicating whether this provider call succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets air-temperature value.
    /// </summary>
    public AirTemperature? AirTemperatureC => Metrics.AirTemperatureC;

    /// <summary>
    /// Gets sea-surface temperature value.
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
    /// Gets textual sea-state summary from provider or normalization.
    /// </summary>
    public SeaStateText? SeaStateText => Metrics.SeaStateText;

    /// <summary>
    /// Gets the provider observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc => FetchInfo.ObservationTimeUtc;

    /// <summary>
    /// Gets the fetch completion timestamp in UTC.
    /// </summary>
    public DateTimeOffset RetrievedAtUtc => FetchInfo.RetrievedAtUtc;

    /// <summary>
    /// Gets normalized confidence score in range [0,1].
    /// </summary>
    public QualityScore QualityScore => FetchInfo.QualityScore;

    /// <summary>
    /// Gets failure detail when <see cref="IsSuccess"/> is false.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful snapshot.
    /// </summary>
    /// <param name="provider">Grouped provider metadata.</param>
    /// <param name="metrics">Grouped weather metrics.</param>
    /// <param name="fetchInfo">Grouped provider fetch metadata.</param>
    /// <returns>Successful snapshot.</returns>
    public static WeatherProviderSnapshot CreateSuccess(
        WeatherProviderMetadata provider,
        WeatherMetrics metrics,
        ProviderFetchInfo fetchInfo)
    {
        return new WeatherProviderSnapshot(
            provider,
            true,
            metrics,
            fetchInfo,
            null);
    }

    /// <summary>
    /// Creates a failed snapshot.
    /// </summary>
    /// <param name="provider">Grouped provider metadata.</param>
    /// <param name="retrievedAtUtc">Fetch completion timestamp in UTC.</param>
    /// <param name="error">Failure details.</param>
    /// <returns>Failed snapshot.</returns>
    public static WeatherProviderSnapshot CreateFailure(
        WeatherProviderMetadata provider,
        DateTimeOffset retrievedAtUtc,
        string error)
    {
        return CreateFailureWithoutMetrics(
            provider,
            retrievedAtUtc,
            error);
    }

    private static WeatherProviderSnapshot CreateFailureWithoutMetrics(
        WeatherProviderMetadata provider,
        DateTimeOffset retrievedAtUtc,
        string error)
    {
        var fetchInfo = new ProviderFetchInfo(
            null,
            retrievedAtUtc,
            QualityScore.Zero);

        return new WeatherProviderSnapshot(
            provider,
            false,
            WeatherMetrics.Empty,
            fetchInfo,
            error);
    }
}
