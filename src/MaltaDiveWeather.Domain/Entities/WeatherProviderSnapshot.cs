namespace MaltaDiveWeather.Domain.Entities;

/// <summary>
/// Provider-normalized weather and marine metrics for a single fetch call.
/// </summary>
public sealed record WeatherProviderSnapshot
{
    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Gets a value indicating whether this provider can return marine data.
    /// </summary>
    public required bool SupportsMarineData { get; init; }

    /// <summary>
    /// Gets a value indicating whether this provider call succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets air temperature in Celsius.
    /// </summary>
    public double? AirTemperatureC { get; init; }

    /// <summary>
    /// Gets sea surface temperature in Celsius.
    /// </summary>
    public double? WaterTemperatureC { get; init; }

    /// <summary>
    /// Gets wind speed in meters per second.
    /// </summary>
    public double? WindSpeedMps { get; init; }

    /// <summary>
    /// Gets wind direction in degrees.
    /// </summary>
    public int? WindDirectionDeg { get; init; }

    /// <summary>
    /// Gets wave height in meters.
    /// </summary>
    public double? WaveHeightM { get; init; }

    /// <summary>
    /// Gets textual sea-state summary from provider or normalization.
    /// </summary>
    public string? SeaStateText { get; init; }

    /// <summary>
    /// Gets the provider observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; init; }

    /// <summary>
    /// Gets the fetch completion timestamp in UTC.
    /// </summary>
    public required DateTimeOffset RetrievedAtUtc { get; init; }

    /// <summary>
    /// Gets provider payload used for debug and diagnostics.
    /// </summary>
    public required string RawPayloadJson { get; init; }

    /// <summary>
    /// Gets normalized confidence score in range [0,1].
    /// </summary>
    public required double QualityScore { get; init; }

    /// <summary>
    /// Gets failure detail when <see cref="IsSuccess"/> is false.
    /// </summary>
    public string? Error { get; init; }
}
