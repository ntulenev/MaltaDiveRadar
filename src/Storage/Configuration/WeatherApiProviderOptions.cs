using System.ComponentModel.DataAnnotations;

namespace Storage.Configuration;

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
