using MaltaDiveWeather.Domain.Enums;

namespace MaltaDiveWeather.Domain.Entities;

/// <summary>
/// Aggregated normalized weather snapshot for a dive site.
/// </summary>
public sealed record WeatherSnapshot
{
    /// <summary>
    /// Gets dive site ID.
    /// </summary>
    public required int DiveSiteId { get; init; }

    /// <summary>
    /// Gets dive site display name.
    /// </summary>
    public required string DiveSiteName { get; init; }

    /// <summary>
    /// Gets island display name.
    /// </summary>
    public required string Island { get; init; }

    /// <summary>
    /// Gets air temperature in Celsius.
    /// </summary>
    public double? AirTemperatureC { get; init; }

    /// <summary>
    /// Gets water temperature in Celsius.
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
    /// Gets sea state text.
    /// </summary>
    public string? SeaStateText { get; init; }

    /// <summary>
    /// Gets summarized condition status.
    /// </summary>
    public required SeaConditionStatus ConditionStatus { get; init; }

    /// <summary>
    /// Gets short summary text.
    /// </summary>
    public required string ConditionSummary { get; init; }

    /// <summary>
    /// Gets the latest observation time in UTC used in this snapshot.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when this snapshot was last refreshed in UTC.
    /// </summary>
    public required DateTimeOffset LastUpdatedUtc { get; init; }

    /// <summary>
    /// Gets timestamp of the last refresh attempt in UTC.
    /// </summary>
    public required DateTimeOffset LastRefreshAttemptUtc { get; init; }

    /// <summary>
    /// Gets provider name(s) that supplied selected values.
    /// </summary>
    public required string SourceProvider { get; init; }

    /// <summary>
    /// Gets a value indicating whether this snapshot is stale.
    /// </summary>
    public required bool IsStale { get; init; }

    /// <summary>
    /// Gets provider snapshots captured during the refresh cycle.
    /// </summary>
    public IReadOnlyList<WeatherProviderSnapshot> ProviderSnapshots { get; init; } =
        Array.Empty<WeatherProviderSnapshot>();
}
