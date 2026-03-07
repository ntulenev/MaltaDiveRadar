namespace MaltaDiveWeather.Application.Dtos;

/// <summary>
/// API DTO for latest weather dashboard data.
/// </summary>
public sealed record LatestWeatherResponseDto
{
    /// <summary>
    /// Gets timestamp of the last completed refresh cycle in UTC.
    /// </summary>
    public DateTimeOffset? LastRefreshUtc { get; init; }

    /// <summary>
    /// Gets latest snapshots for all sites.
    /// </summary>
    public required IReadOnlyCollection<WeatherSnapshotDto> Snapshots { get; init; }
}
