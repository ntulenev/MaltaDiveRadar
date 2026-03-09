using Abstractions;

using Logic.Services;

using MaltaDiveWeather.Web.Configuration;
using MaltaDiveWeather.Web.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Storage.Configuration;
using Storage.Providers;
using Storage.Repositories;

namespace MaltaDiveWeather.Web.Startup;

/// <summary>
/// Registers application services in the web composition root.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds logic-layer services required by the host.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddLogic(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISeaConditionClassifier, SeaConditionClassifier>();
        services.AddSingleton<IWeatherAggregationService, WeatherAggregationService>();
        services.AddSingleton<IWeatherQueryService, WeatherQueryService>();
        services.AddSingleton<IWeatherRefreshProcessor, WeatherRefreshProcessor>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<WeatherRefreshService>();

        return services;
    }

    /// <summary>
    /// Adds storage and provider services required by the host.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var startupOptions = configuration
            .GetSection(WeatherRefreshOptions.SectionName)
            .Get<WeatherRefreshOptions>() ?? new WeatherRefreshOptions();

        services
            .AddOptions<WeatherRefreshScheduleOptions>()
            .Bind(configuration.GetSection(WeatherRefreshOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<WeatherRefreshOptions>()
            .Bind(configuration.GetSection(WeatherRefreshOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static options => options.DemoMode || options.HasAnyEnabledProvider(),
                "Enable at least one provider, or turn on demo mode.")
            .Validate(
                static options => AreEnabledProviderPrioritiesUnique(options),
                "Enabled provider priorities must be unique.")
            .Validate(
                static options => AreApiKeysValidForEnabledProviders(options),
                "Enabled providers requiring API keys must define keys.")
            .ValidateOnStart();

        services
            .AddOptions<DiveSiteCatalogOptions>()
            .Bind(configuration.GetSection(DiveSiteCatalogOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static options => options.Sites.Count > 0,
                "Configure at least one dive site.")
            .Validate(
                static options => AreDiveSiteIdsUnique(options),
                "Dive site IDs must be unique.")
            .Validate(
                static options => AreDiveSiteTextValuesValid(options),
                "Dive site names and islands must be non-empty.")
            .Validate(
                static options => AreDiveSiteNamesUnique(options),
                "Dive site names must be unique.")
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<DiveSiteSeedData>();
        services.AddSingleton<IDiveSiteCatalog, InMemoryDiveSiteCatalog>();
        services.AddSingleton<IWeatherSnapshotRepository, InMemoryWeatherSnapshotRepository>();

        if (startupOptions.DemoMode)
        {
            services.AddSingleton<DemoWeatherProvider>();
            services.AddTransient<IWeatherProvider>(
                serviceProvider => serviceProvider.GetRequiredService<DemoWeatherProvider>());
        }
        else
        {
            services.AddHttpClient<OpenMeteoProvider>(ConfigureHttpClient);
            services.AddHttpClient<WeatherApiProvider>(ConfigureHttpClient);
            services.AddHttpClient<OpenWeatherProvider>(ConfigureHttpClient);

            services.AddTransient<IWeatherProvider>(
                serviceProvider => serviceProvider.GetRequiredService<OpenMeteoProvider>());

            services.AddTransient<IWeatherProvider>(
                serviceProvider => serviceProvider.GetRequiredService<WeatherApiProvider>());

            services.AddTransient<IWeatherProvider>(
                serviceProvider => serviceProvider.GetRequiredService<OpenWeatherProvider>());
        }

        return services;
    }

    private static void ConfigureHttpClient(
        IServiceProvider serviceProvider,
        HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(client);

        var options = serviceProvider
            .GetRequiredService<IOptions<WeatherRefreshOptions>>()
            .Value;

        client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MaltaDiveWeather/1.0");
    }

    private static bool AreEnabledProviderPrioritiesUnique(
        WeatherRefreshOptions options)
    {
        if (options.DemoMode)
        {
            return true;
        }

        var priorities = new List<int>();

        if (options.Providers.OpenMeteo.Enabled)
        {
            priorities.Add(options.Providers.OpenMeteo.Priority);
        }

        if (options.Providers.WeatherApi.Enabled)
        {
            priorities.Add(options.Providers.WeatherApi.Priority);
        }

        if (options.Providers.OpenWeather.Enabled)
        {
            priorities.Add(options.Providers.OpenWeather.Priority);
        }

        return priorities.Count == priorities.Distinct().Count();
    }

    private static bool AreApiKeysValidForEnabledProviders(
        WeatherRefreshOptions options)
    {
        if (options.DemoMode)
        {
            return true;
        }

        if (options.Providers.WeatherApi.Enabled &&
            string.IsNullOrWhiteSpace(options.Providers.WeatherApi.ApiKey))
        {
            return false;
        }

        if (options.Providers.OpenWeather.Enabled &&
            string.IsNullOrWhiteSpace(options.Providers.OpenWeather.ApiKey))
        {
            return false;
        }

        return true;
    }

    private static bool AreDiveSiteIdsUnique(DiveSiteCatalogOptions options)
    {
        var ids = options.Sites
            .Select(static site => site.Id)
            .ToArray();

        return ids.Length == ids.Distinct().Count();
    }

    private static bool AreDiveSiteNamesUnique(DiveSiteCatalogOptions options)
    {
        var names = options.Sites
            .Select(static site => site.Name?.Trim() ?? string.Empty)
            .ToArray();

        return names.Length == names.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    private static bool AreDiveSiteTextValuesValid(DiveSiteCatalogOptions options)
    {
        return options.Sites.All(static site =>
            !string.IsNullOrWhiteSpace(site.Name) &&
            !string.IsNullOrWhiteSpace(site.Island));
    }
}
