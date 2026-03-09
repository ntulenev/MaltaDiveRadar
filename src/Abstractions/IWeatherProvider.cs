using Models;

namespace Abstractions;

/// <summary>
/// Weather provider contract used by the aggregation service.
/// </summary>
public interface IWeatherProvider
{
    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    ProviderName ProviderName { get; }

    /// <summary>
    /// Gets provider priority where lower value means higher precedence.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports marine metrics.
    /// </summary>
    bool SupportsMarineData { get; }

    /// <summary>
    /// Gets the latest normalized weather snapshot for a coordinate pair.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest provider snapshot.</returns>
    Task<WeatherProviderSnapshot> GetLatestAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken);
}

