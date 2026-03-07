using MaltaDiveWeather.Application.Dtos;

namespace MaltaDiveWeather.Application.Abstractions;

/// <summary>
/// Read-focused application service for API responses.
/// </summary>
public interface IWeatherQueryService
{
    /// <summary>
    /// Gets all configured dive sites.
    /// </summary>
    /// <returns>Site DTOs.</returns>
    IReadOnlyCollection<DiveSiteDto> GetSites();

    /// <summary>
    /// Resolves one site by ID.
    /// </summary>
    /// <param name="siteId">Site ID.</param>
    /// <returns>Site DTO or null.</returns>
    DiveSiteDto? GetSite(int siteId);

    /// <summary>
    /// Gets latest weather snapshot for one site.
    /// </summary>
    /// <param name="siteId">Site ID.</param>
    /// <returns>Weather DTO or null.</returns>
    WeatherSnapshotDto? GetSiteWeather(int siteId);

    /// <summary>
    /// Gets latest weather snapshots for all sites.
    /// </summary>
    /// <returns>Dashboard response DTO.</returns>
    LatestWeatherResponseDto GetLatestWeather();
}
