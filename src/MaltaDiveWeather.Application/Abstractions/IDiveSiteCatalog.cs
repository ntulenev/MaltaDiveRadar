using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Application.Abstractions;

/// <summary>
/// Provides access to static dive-site metadata.
/// </summary>
public interface IDiveSiteCatalog
{
    /// <summary>
    /// Gets all configured dive sites.
    /// </summary>
    /// <returns>Configured sites.</returns>
    IReadOnlyCollection<DiveSite> GetAllSites();

    /// <summary>
    /// Gets all active dive sites.
    /// </summary>
    /// <returns>Active sites.</returns>
    IReadOnlyCollection<DiveSite> GetActiveSites();

    /// <summary>
    /// Resolves a site by ID.
    /// </summary>
    /// <param name="id">Site ID.</param>
    /// <returns>The matching site, or null when missing.</returns>
    DiveSite? GetById(int id);
}
