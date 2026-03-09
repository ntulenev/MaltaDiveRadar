namespace Storage.Configuration;

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
