namespace Transport;

/// <summary>
/// API DTO for normalized weather snapshots.
/// </summary>
public sealed record WeatherSnapshotDto
{
    /// <summary>
    /// Gets dive-site ID.
    /// </summary>
    public required int DiveSiteId { get; init; }

    /// <summary>
    /// Gets site name.
    /// </summary>
    public required string DiveSiteName { get; init; }

    /// <summary>
    /// Gets island name.
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
    /// Gets wind direction as cardinal text.
    /// </summary>
    public string? WindDirectionCardinal { get; init; }

    /// <summary>
    /// Gets wave height in meters.
    /// </summary>
    public double? WaveHeightM { get; init; }

    /// <summary>
    /// Gets sea-state text.
    /// </summary>
    public string? SeaStateText { get; init; }

    /// <summary>
    /// Gets condition badge value.
    /// </summary>
    public required string ConditionStatus { get; init; }

    /// <summary>
    /// Gets short condition summary.
    /// </summary>
    public required string ConditionSummary { get; init; }

    /// <summary>
    /// Gets chosen observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; init; }

    /// <summary>
    /// Gets last successful update timestamp in UTC.
    /// </summary>
    public required DateTimeOffset LastUpdatedUtc { get; init; }

    /// <summary>
    /// Gets last refresh attempt timestamp in UTC.
    /// </summary>
    public required DateTimeOffset LastRefreshAttemptUtc { get; init; }

    /// <summary>
    /// Gets provider source label.
    /// </summary>
    public required string SourceProvider { get; init; }

    /// <summary>
    /// Gets stale flag.
    /// </summary>
    public required bool IsStale { get; init; }

    /// <summary>
    /// Gets provider snapshots for diagnostics.
    /// </summary>
    public IReadOnlyCollection<ProviderSnapshotDto> ProviderSnapshots { get; init; } =
        [];
}

