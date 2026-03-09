using System.ComponentModel.DataAnnotations;

namespace Storage.Configuration;

/// <summary>
/// Weather provider and transport configuration.
/// </summary>
public sealed class WeatherRefreshOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionName = "WeatherRefresh";

    /// <summary>
    /// Gets a value indicating whether demo mode is enabled.
    /// When enabled, mock weather data is used and external transport is skipped.
    /// </summary>
    public bool DemoMode { get; init; }

    /// <summary>
    /// Gets HTTP timeout in seconds for provider requests.
    /// </summary>
    [Range(5, 120)]
    public int HttpTimeoutSeconds { get; init; } = 20;

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

