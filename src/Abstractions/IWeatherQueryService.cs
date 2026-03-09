using Models;

namespace Abstractions;

/// <summary>
/// Read-focused application service for weather domain read models.
/// </summary>
public interface IWeatherQueryService
{
    /// <summary>
    /// Gets all configured dive sites.
    /// </summary>
    /// <returns>Configured domain sites.</returns>
    IReadOnlyCollection<DiveSite> GetSites();

    /// <summary>
    /// Resolves one site by ID.
    /// </summary>
    /// <param name="siteId">Site ID.</param>
    /// <returns>Domain site or null.</returns>
    DiveSite? GetSite(DiveSiteId siteId);

    /// <summary>
    /// Gets latest weather snapshot for one site.
    /// </summary>
    /// <param name="siteId">Site ID.</param>
    /// <returns>Domain weather snapshot or null.</returns>
    WeatherSnapshot? GetSiteWeather(DiveSiteId siteId);

    /// <summary>
    /// Gets latest weather snapshots for all sites.
    /// </summary>
    /// <returns>Domain dashboard read model.</returns>
    LatestWeather GetLatestWeather();
}

