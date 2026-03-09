namespace Models;

/// <summary>
/// Provider-normalized weather and marine metrics for a single fetch call.
/// </summary>
public sealed class WeatherProviderSnapshot
{
    private WeatherProviderSnapshot(
        string providerName,
        int priority,
        bool supportsMarineData,
        bool isSuccess,
        double? airTemperatureC,
        double? waterTemperatureC,
        double? windSpeedMps,
        int? windDirectionDeg,
        double? waveHeightM,
        string? seaStateText,
        DateTimeOffset? observationTimeUtc,
        DateTimeOffset retrievedAtUtc,
        string rawPayloadJson,
        double qualityScore,
        string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(rawPayloadJson);

        if (priority <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                "Provider priority must be positive.");
        }

        if (qualityScore is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(qualityScore),
                "Quality score must be in range [0, 1].");
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

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException(
                "Failure snapshots must define an error message.",
                nameof(error));
        }

        ProviderName = providerName.Trim();
        Priority = priority;
        SupportsMarineData = supportsMarineData;
        IsSuccess = isSuccess;
        AirTemperatureC = airTemperatureC;
        WaterTemperatureC = waterTemperatureC;
        WindSpeedMps = windSpeedMps;
        WindDirectionDeg = windDirectionDeg;
        WaveHeightM = waveHeightM;
        SeaStateText = string.IsNullOrWhiteSpace(seaStateText)
            ? null
            : seaStateText.Trim();
        ObservationTimeUtc = observationTimeUtc;
        RetrievedAtUtc = retrievedAtUtc;
        RawPayloadJson = rawPayloadJson;
        QualityScore = qualityScore;
        Error = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Trim();
    }

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this provider can return marine data.
    /// </summary>
    public bool SupportsMarineData { get; }

    /// <summary>
    /// Gets a value indicating whether this provider call succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets air temperature in Celsius.
    /// </summary>
    public double? AirTemperatureC { get; }

    /// <summary>
    /// Gets sea surface temperature in Celsius.
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
    /// Gets textual sea-state summary from provider or normalization.
    /// </summary>
    public string? SeaStateText { get; }

    /// <summary>
    /// Gets the provider observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; }

    /// <summary>
    /// Gets the fetch completion timestamp in UTC.
    /// </summary>
    public DateTimeOffset RetrievedAtUtc { get; }

    /// <summary>
    /// Gets provider payload used for debug and diagnostics.
    /// </summary>
    public string RawPayloadJson { get; }

    /// <summary>
    /// Gets normalized confidence score in range [0,1].
    /// </summary>
    public double QualityScore { get; }

    /// <summary>
    /// Gets failure detail when <see cref="IsSuccess"/> is false.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful snapshot.
    /// </summary>
    /// <param name="providerName">Provider display name.</param>
    /// <param name="priority">Provider priority where lower is better.</param>
    /// <param name="supportsMarineData">Whether provider supports marine data.</param>
    /// <param name="airTemperatureC">Air temperature in Celsius.</param>
    /// <param name="waterTemperatureC">Water temperature in Celsius.</param>
    /// <param name="windSpeedMps">Wind speed in meters per second.</param>
    /// <param name="windDirectionDeg">Wind direction in degrees.</param>
    /// <param name="waveHeightM">Wave height in meters.</param>
    /// <param name="seaStateText">Sea state summary text.</param>
    /// <param name="observationTimeUtc">Observation timestamp in UTC.</param>
    /// <param name="retrievedAtUtc">Fetch completion timestamp in UTC.</param>
    /// <param name="rawPayloadJson">Raw response payload JSON.</param>
    /// <param name="qualityScore">Quality score in range [0,1].</param>
    /// <returns>Successful snapshot.</returns>
    public static WeatherProviderSnapshot CreateSuccess(
        string providerName,
        int priority,
        bool supportsMarineData,
        double? airTemperatureC,
        double? waterTemperatureC,
        double? windSpeedMps,
        int? windDirectionDeg,
        double? waveHeightM,
        string? seaStateText,
        DateTimeOffset? observationTimeUtc,
        DateTimeOffset retrievedAtUtc,
        string rawPayloadJson,
        double qualityScore)
    {
        return new WeatherProviderSnapshot(
            providerName,
            priority,
            supportsMarineData,
            true,
            airTemperatureC,
            waterTemperatureC,
            windSpeedMps,
            windDirectionDeg,
            waveHeightM,
            seaStateText,
            observationTimeUtc,
            retrievedAtUtc,
            rawPayloadJson,
            qualityScore,
            null);
    }

    /// <summary>
    /// Creates a failed snapshot.
    /// </summary>
    /// <param name="providerName">Provider display name.</param>
    /// <param name="priority">Provider priority where lower is better.</param>
    /// <param name="supportsMarineData">Whether provider supports marine data.</param>
    /// <param name="retrievedAtUtc">Fetch completion timestamp in UTC.</param>
    /// <param name="rawPayloadJson">Raw response payload JSON.</param>
    /// <param name="error">Failure details.</param>
    /// <returns>Failed snapshot.</returns>
    public static WeatherProviderSnapshot CreateFailure(
        string providerName,
        int priority,
        bool supportsMarineData,
        DateTimeOffset retrievedAtUtc,
        string rawPayloadJson,
        string error)
    {
        return new WeatherProviderSnapshot(
            providerName,
            priority,
            supportsMarineData,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            retrievedAtUtc,
            rawPayloadJson,
            0D,
            error);
    }
}
