using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace MaltaDiveWeather.Web.Configuration;

/// <summary>
/// Background refresh scheduling configuration.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by options binding in dependency injection.")]
internal sealed class WeatherRefreshScheduleOptions
{
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
}
