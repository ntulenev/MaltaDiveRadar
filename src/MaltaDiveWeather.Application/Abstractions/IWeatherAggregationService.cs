using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Application.Abstractions;

/// <summary>
/// Performs cross-provider weather refresh and aggregation.
/// </summary>
public interface IWeatherAggregationService
{
    /// <summary>
    /// Gets timestamp of the last completed global refresh cycle.
    /// </summary>
    DateTimeOffset? LastRefreshCompletedUtc { get; }

    /// <summary>
    /// Refreshes weather for all active sites.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest snapshots after refresh.</returns>
    Task<IReadOnlyCollection<WeatherSnapshot>> RefreshAllAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes weather for a specific site.
    /// </summary>
    /// <param name="site">Dive site.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest snapshot for the site.</returns>
    Task<WeatherSnapshot?> RefreshSiteAsync(
        DiveSite site,
        CancellationToken cancellationToken);
}
