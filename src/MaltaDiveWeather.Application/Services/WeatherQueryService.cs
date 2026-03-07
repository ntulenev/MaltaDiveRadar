using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Application.Dtos;
using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Application.Services;

/// <summary>
/// Read model service that maps domain entities to API DTOs.
/// </summary>
public sealed class WeatherQueryService : IWeatherQueryService
{
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

    private readonly IDiveSiteCatalog _diveSiteCatalog;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly IWeatherAggregationService _aggregationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherQueryService"/> class.
    /// </summary>
    /// <param name="diveSiteCatalog">Dive-site catalog.</param>
    /// <param name="snapshotRepository">Snapshot repository.</param>
    /// <param name="aggregationService">Aggregation service.</param>
    public WeatherQueryService(
        IDiveSiteCatalog diveSiteCatalog,
        IWeatherSnapshotRepository snapshotRepository,
        IWeatherAggregationService aggregationService)
    {
        ArgumentNullException.ThrowIfNull(diveSiteCatalog);
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(aggregationService);

        _diveSiteCatalog = diveSiteCatalog;
        _snapshotRepository = snapshotRepository;
        _aggregationService = aggregationService;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<DiveSiteDto> GetSites()
    {
        return _diveSiteCatalog.GetAllSites()
            .OrderBy(static site => site.Name)
            .Select(MapDiveSite)
            .ToArray();
    }

    /// <inheritdoc />
    public DiveSiteDto? GetSite(int siteId)
    {
        var site = _diveSiteCatalog.GetById(siteId);
        if (site is null)
        {
            return null;
        }

        return MapDiveSite(site);
    }

    /// <inheritdoc />
    public WeatherSnapshotDto? GetSiteWeather(int siteId)
    {
        var snapshot = _snapshotRepository.GetBySiteId(siteId);
        if (snapshot is null)
        {
            return null;
        }

        return MapSnapshot(snapshot);
    }

    /// <inheritdoc />
    public LatestWeatherResponseDto GetLatestWeather()
    {
        var snapshots = _snapshotRepository.GetLatest()
            .OrderBy(static snapshot => snapshot.DiveSiteName)
            .Select(MapSnapshot)
            .ToArray();

        return new LatestWeatherResponseDto
        {
            LastRefreshUtc = _aggregationService.LastRefreshCompletedUtc,
            Snapshots = snapshots,
        };
    }

    private static DiveSiteDto MapDiveSite(DiveSite site)
    {
        return new DiveSiteDto
        {
            Id = site.Id,
            Name = site.Name,
            Island = site.Island,
            Latitude = site.Latitude,
            Longitude = site.Longitude,
            DisplayX = site.DisplayX,
            DisplayY = site.DisplayY,
            IsActive = site.IsActive,
        };
    }

    private static WeatherSnapshotDto MapSnapshot(WeatherSnapshot snapshot)
    {
        return new WeatherSnapshotDto
        {
            DiveSiteId = snapshot.DiveSiteId,
            DiveSiteName = snapshot.DiveSiteName,
            Island = snapshot.Island,
            AirTemperatureC = snapshot.AirTemperatureC,
            WaterTemperatureC = snapshot.WaterTemperatureC,
            WindSpeedMps = snapshot.WindSpeedMps,
            WindDirectionDeg = snapshot.WindDirectionDeg,
            WindDirectionCardinal = ToCardinalDirection(snapshot.WindDirectionDeg),
            WaveHeightM = snapshot.WaveHeightM,
            SeaStateText = snapshot.SeaStateText,
            ConditionStatus = snapshot.ConditionStatus.ToString(),
            ConditionSummary = snapshot.ConditionSummary,
            ObservationTimeUtc = snapshot.ObservationTimeUtc,
            LastUpdatedUtc = snapshot.LastUpdatedUtc,
            LastRefreshAttemptUtc = snapshot.LastRefreshAttemptUtc,
            SourceProvider = snapshot.SourceProvider,
            IsStale = snapshot.IsStale,
            ProviderSnapshots = snapshot.ProviderSnapshots
                .OrderBy(static provider => provider.Priority)
                .Select(provider => new ProviderSnapshotDto
                {
                    ProviderName = provider.ProviderName,
                    Priority = provider.Priority,
                    QualityScore = provider.QualityScore,
                    IsSuccess = provider.IsSuccess,
                    ObservationTimeUtc = provider.ObservationTimeUtc,
                    RetrievedAtUtc = provider.RetrievedAtUtc,
                    Error = provider.Error,
                })
                .ToArray(),
        };
    }

    private static string? ToCardinalDirection(int? windDirectionDegrees)
    {
        if (windDirectionDegrees is null)
        {
            return null;
        }

        var normalized = ((windDirectionDegrees.Value % 360) + 360) % 360;
        var index = (int)Math.Round(normalized / 22.5D, MidpointRounding.AwayFromZero) %
            WindDirections.Length;

        return WindDirections[index];
    }
}
