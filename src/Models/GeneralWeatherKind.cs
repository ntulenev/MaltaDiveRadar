namespace Models;

/// <summary>
/// Represents broad atmospheric weather categories for display.
/// </summary>
public enum GeneralWeatherKind
{
    /// <summary>
    /// Weather category is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Clear and sunny conditions.
    /// </summary>
    Sunny = 1,

    /// <summary>
    /// Partial cloud cover.
    /// </summary>
    PartlyCloudy = 2,

    /// <summary>
    /// Mostly or fully cloudy conditions.
    /// </summary>
    Cloudy = 3,

    /// <summary>
    /// Fog, haze, or mist.
    /// </summary>
    Fog = 4,

    /// <summary>
    /// Light precipitation in drizzle form.
    /// </summary>
    Drizzle = 5,

    /// <summary>
    /// Rain or rain showers.
    /// </summary>
    Rain = 6,

    /// <summary>
    /// Snow conditions.
    /// </summary>
    Snow = 7,

    /// <summary>
    /// Thunderstorm conditions.
    /// </summary>
    Thunderstorm = 8,

    /// <summary>
    /// Mixed or unclassified conditions.
    /// </summary>
    Mixed = 9,
}
