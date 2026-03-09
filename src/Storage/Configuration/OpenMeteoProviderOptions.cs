using System.ComponentModel.DataAnnotations;

namespace Storage.Configuration;

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
