using Models;

namespace Transport;

/// <summary>
/// Maps domain read models to API DTO responses.
/// </summary>
public static class ApiDtoMapper
{
    public static DiveSiteDto MapDiveSite(DiveSite site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return new DiveSiteDto
        {
            Id = site.Id.Value,
            Name = site.Name.Value,
            Description = site.Description,
            Island = site.Island.Value,
            Latitude = site.Latitude.Degrees,
            Longitude = site.Longitude.Degrees,
            DisplayX = site.DisplayX,
            DisplayY = site.DisplayY,
            IsActive = site.IsActive,
        };
    }

    public static WeatherSnapshotDto MapSnapshot(WeatherSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new WeatherSnapshotDto
        {
            DiveSiteId = snapshot.DiveSiteId.Value,
            DiveSiteName = snapshot.DiveSiteName.Value,
            Island = snapshot.Island.Value,
            AirTemperatureC = snapshot.AirTemperatureC?.Celsius,
            WaterTemperatureC = snapshot.WaterTemperatureC?.Celsius,
            WindSpeedMps = snapshot.WindSpeedMps?.MetersPerSecond,
            WindDirectionDeg = snapshot.WindDirectionDeg?.Degrees,
            WindDirectionCardinal = ToCardinalDirection(
                snapshot.WindDirectionDeg?.Degrees),
            WaveHeightM = snapshot.WaveHeightM?.Meters,
            SeaStateText = snapshot.SeaStateText?.Value,
            GeneralWeatherText = ToGeneralWeatherText(snapshot.GeneralWeather),
            ConditionStatus = snapshot.ConditionStatus.ToString(),
            ConditionSummary = snapshot.ConditionSummary.Value,
            ObservationTimeUtc = snapshot.ObservationTimeUtc,
            LastUpdatedUtc = snapshot.LastUpdatedUtc,
            LastRefreshAttemptUtc = snapshot.LastRefreshAttemptUtc,
            SourceProvider = snapshot.SourceProvider.Value,
            IsStale = snapshot.IsStale,
            ProviderSnapshots = [.. snapshot.ProviderSnapshots
                .OrderBy(static provider => provider.Priority.Value)
                .Select(static provider => new ProviderSnapshotDto
                {
                    ProviderName = provider.ProviderName.Value,
                    Priority = provider.Priority.Value,
                    QualityScore = provider.QualityScore.Value,
                    IsSuccess = provider.IsSuccess,
                    ObservationTimeUtc = provider.ObservationTimeUtc,
                    RetrievedAtUtc = provider.RetrievedAtUtc,
                    Error = provider.Error,
                })],
        };
    }

    public static LatestWeatherResponseDto MapLatestWeather(LatestWeather latestWeather)
    {
        ArgumentNullException.ThrowIfNull(latestWeather);

        return new LatestWeatherResponseDto
        {
            LastRefreshUtc = latestWeather.LastRefreshUtc,
            Snapshots = [.. latestWeather.Snapshots
                .OrderBy(static snapshot => snapshot.DiveSiteName.Value)
                .Select(MapSnapshot)],
        };
    }

    private static string? ToCardinalDirection(int? windDirectionDegrees)
    {
        if (windDirectionDegrees is null)
        {
            return null;
        }

        var normalized = ((windDirectionDegrees.Value % 360) + 360) % 360;
        var index = (int)Math.Round(
            normalized / 22.5D,
            MidpointRounding.AwayFromZero) % WindDirections.Length;

        return WindDirections[index];
    }

    private static string? ToGeneralWeatherText(
        GeneralWeatherKind? generalWeather)
    {
        if (generalWeather is null)
        {
            return null;
        }

        return generalWeather.Value switch
        {
            GeneralWeatherKind.Unknown => "Unknown",
            GeneralWeatherKind.Sunny => "Sunny",
            GeneralWeatherKind.PartlyCloudy => "Partly cloudy",
            GeneralWeatherKind.Cloudy => "Cloudy",
            GeneralWeatherKind.Fog => "Fog",
            GeneralWeatherKind.Drizzle => "Drizzle",
            GeneralWeatherKind.Rain => "Rain",
            GeneralWeatherKind.Snow => "Snow",
            GeneralWeatherKind.Thunderstorm => "Thunderstorm",
            GeneralWeatherKind.Mixed => "Mixed conditions",
            _ => "Unknown",
        };
    }

    private static readonly string[] WindDirections =
    [
        "N",
        "NNE",
        "NE",
        "ENE",
        "E",
        "ESE",
        "SE",
        "SSE",
        "S",
        "SSW",
        "SW",
        "WSW",
        "W",
        "WNW",
        "NW",
        "NNW",
    ];
}
