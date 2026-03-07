using System.ComponentModel.DataAnnotations;

namespace MaltaDiveWeather.Infrastructure.Configuration;

/// <summary>
/// Weather refresh and provider configuration.
/// </summary>
public sealed class WeatherRefreshOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionName = "WeatherRefresh";

    /// <summary>
    /// Gets refresh interval in minutes.
    /// </summary>
    [Range(1, 720)]
    public int RefreshIntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Gets startup delay in seconds before first refresh.
    /// </summary>
    [Range(0, 300)]
    public int StartupDelaySeconds { get; init; } = 5;

    /// <summary>
    /// Gets HTTP timeout in seconds for provider requests.
    /// </summary>
    [Range(5, 120)]
    public int HttpTimeoutSeconds { get; init; } = 20;

    /// <summary>
    /// Gets a value indicating whether demo mode is enabled.
    /// When enabled, mock weather data is used and external transport is skipped.
    /// </summary>
    public bool DemoMode { get; init; }

    /// <summary>
    /// Gets provider-specific settings.
    /// </summary>
    public WeatherProviderPoolOptions Providers { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether at least one provider is enabled.
    /// </summary>
    /// <returns>True when any provider is enabled.</returns>
    public bool HasAnyEnabledProvider()
    {
        return Providers.OpenMeteo.Enabled ||
            Providers.WeatherApi.Enabled ||
            Providers.OpenWeather.Enabled;
    }
}

/// <summary>
/// Provider pool settings.
/// </summary>
public sealed class WeatherProviderPoolOptions
{
    /// <summary>
    /// Gets Open-Meteo provider settings.
    /// </summary>
    public OpenMeteoProviderOptions OpenMeteo { get; init; } = new();

    /// <summary>
    /// Gets WeatherAPI provider settings.
    /// </summary>
    public WeatherApiProviderOptions WeatherApi { get; init; } = new();

    /// <summary>
    /// Gets OpenWeather provider settings.
    /// </summary>
    public OpenWeatherProviderOptions OpenWeather { get; init; } = new();
}

/// <summary>
/// Open-Meteo provider settings.
/// </summary>
public sealed class OpenMeteoProviderOptions
{
    /// <summary>
    /// Gets a value indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    [Range(1, 99)]
    public int Priority { get; init; } = 1;
}

/// <summary>
/// WeatherAPI provider settings.
/// </summary>
public sealed class WeatherApiProviderOptions
{
    /// <summary>
    /// Gets a value indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    [Range(1, 99)]
    public int Priority { get; init; } = 2;

    /// <summary>
    /// Gets API key for WeatherAPI.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;
}

/// <summary>
/// OpenWeather provider settings.
/// </summary>
public sealed class OpenWeatherProviderOptions
{
    /// <summary>
    /// Gets a value indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets provider priority where lower value means higher preference.
    /// </summary>
    [Range(1, 99)]
    public int Priority { get; init; } = 3;

    /// <summary>
    /// Gets API key for OpenWeather.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;
}
