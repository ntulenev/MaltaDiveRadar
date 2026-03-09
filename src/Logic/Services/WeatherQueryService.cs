using Models;
using Abstractions;

namespace Logic.Services;

/// <summary>
/// Read model service that exposes domain weather data.
/// </summary>
public sealed class WeatherQueryService : IWeatherQueryService
{
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
    public IReadOnlyCollection<DiveSite> GetSites()
    {
        return _diveSiteCatalog.GetAllSites()
            .OrderBy(static site => site.Name.Value)
            .ToArray();
    }

    /// <inheritdoc />
    public DiveSite? GetSite(DiveSiteId siteId)
    {
        return _diveSiteCatalog.GetById(siteId);
    }

    /// <inheritdoc />
    public WeatherSnapshot? GetSiteWeather(DiveSiteId siteId)
    {
        return _snapshotRepository.GetBySiteId(siteId);
    }

    /// <inheritdoc />
    public LatestWeather GetLatestWeather()
    {
        var snapshots = _snapshotRepository.GetLatest()
            .OrderBy(static snapshot => snapshot.DiveSiteName.Value)
            .ToArray();

        return new LatestWeather(
            _aggregationService.LastRefreshCompletedUtc,
            snapshots);
    }

    private readonly IDiveSiteCatalog _diveSiteCatalog;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly IWeatherAggregationService _aggregationService;
}

