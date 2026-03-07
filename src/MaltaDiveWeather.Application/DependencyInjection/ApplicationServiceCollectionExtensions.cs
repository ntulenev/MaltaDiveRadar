using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Application.Services;
using MaltaDiveWeather.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MaltaDiveWeather.Application.DependencyInjection;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services required by the web host.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISeaConditionClassifier, SeaConditionClassifier>();
        services.AddSingleton<IWeatherAggregationService, WeatherAggregationService>();
        services.AddSingleton<IWeatherQueryService, WeatherQueryService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
