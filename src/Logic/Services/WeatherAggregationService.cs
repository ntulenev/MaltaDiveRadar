using System.Diagnostics.CodeAnalysis;

using Abstractions;

using Microsoft.Extensions.Logging;

using Models;

namespace Logic.Services;

/// <summary>
/// Aggregates provider snapshots into final dive-site weather snapshots.
/// </summary>
public sealed partial class WeatherAggregationService : IWeatherAggregationService
{
    private readonly ILogger<WeatherAggregationService> _logger;
    private readonly IDiveSiteCatalog _diveSiteCatalog;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly ISeaConditionClassifier _seaConditionClassifier;
    private readonly IWeatherProvider[] _providers;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="WeatherAggregationService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="diveSiteCatalog">Dive-site catalog.</param>
    /// <param name="snapshotRepository">Snapshot repository.</param>
    /// <param name="seaConditionClassifier">Sea-condition classifier.</param>
    /// <param name="providers">Registered weather providers.</param>
    /// <param name="timeProvider">Time provider.</param>
    public WeatherAggregationService(
        ILogger<WeatherAggregationService> logger,
        IDiveSiteCatalog diveSiteCatalog,
        IWeatherSnapshotRepository snapshotRepository,
        ISeaConditionClassifier seaConditionClassifier,
        IEnumerable<IWeatherProvider> providers,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(diveSiteCatalog);
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(seaConditionClassifier);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger = logger;
        _diveSiteCatalog = diveSiteCatalog;
        _snapshotRepository = snapshotRepository;
        _seaConditionClassifier = seaConditionClassifier;
        _providers = providers.OrderBy(static provider => provider.Priority).ToArray();
        _timeProvider = timeProvider;

        if (_providers.Length == 0)
        {
            throw new InvalidOperationException(
                "At least one weather provider must be registered.");
        }
    }

    /// <inheritdoc />
    public DateTimeOffset? LastRefreshCompletedUtc { get; private set; }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<WeatherSnapshot>> RefreshAllAsync(
        CancellationToken cancellationToken)
    {
        var snapshots = new List<WeatherSnapshot>();

        foreach (var site in _diveSiteCatalog.GetActiveSites())
        {
            var snapshot = await RefreshSiteAsync(site, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        LastRefreshCompletedUtc = _timeProvider.GetUtcNow();
        return snapshots;
    }

    /// <inheritdoc />
    public async Task<WeatherSnapshot?> RefreshSiteAsync(
        DiveSite site,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(site);

        var refreshAttemptUtc = _timeProvider.GetUtcNow();
        var providerSnapshots = new List<WeatherProviderSnapshot>(_providers.Length);

        foreach (var provider in _providers)
        {
            var providerSnapshot = await ExecuteProviderAsync(
                provider,
                site,
                cancellationToken).ConfigureAwait(false);

            providerSnapshots.Add(providerSnapshot);
        }

        var successfulSnapshots = providerSnapshots
            .Where(static snapshot => snapshot.IsSuccess)
            .ToArray();

        if (successfulSnapshots.Length == 0)
        {
            LogAllProvidersFailed(
                _logger,
                site.Id.Value,
                site.Name);

            await _snapshotRepository.MarkStaleAsync(
                site.Id,
                refreshAttemptUtc,
                cancellationToken).ConfigureAwait(false);

            var staleSnapshot = _snapshotRepository.GetBySiteId(site.Id);
            if (staleSnapshot is not null)
            {
                return staleSnapshot;
            }

            var unavailableSnapshot = CreateUnavailableSnapshot(
                site,
                providerSnapshots,
                refreshAttemptUtc);

            await _snapshotRepository.UpsertAsync(
                unavailableSnapshot,
                cancellationToken).ConfigureAwait(false);

            await _snapshotRepository.SaveProviderSnapshotsAsync(
                site.Id,
                providerSnapshots,
                cancellationToken).ConfigureAwait(false);

            return unavailableSnapshot;
        }

        var aggregatedSnapshot = BuildAggregatedSnapshot(
            site,
            successfulSnapshots,
            providerSnapshots,
            refreshAttemptUtc);

        await _snapshotRepository.UpsertAsync(
            aggregatedSnapshot,
            cancellationToken).ConfigureAwait(false);

        await _snapshotRepository.SaveProviderSnapshotsAsync(
            site.Id,
            providerSnapshots,
            cancellationToken).ConfigureAwait(false);

        LogSiteAggregationSucceeded(
            _logger,
            site.Id.Value,
            aggregatedSnapshot.SourceProvider);

        return aggregatedSnapshot;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Provider failures must never abort a refresh cycle.")]
    private async Task<WeatherProviderSnapshot> ExecuteProviderAsync(
        IWeatherProvider provider,
        DiveSite site,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await provider.GetLatestAsync(
                site.Latitude,
                site.Longitude,
                cancellationToken).ConfigureAwait(false);

            LogProviderResult(
                _logger,
                provider.ProviderName.Value,
                site.Name,
                snapshot.IsSuccess,
                snapshot.QualityScore);

            return snapshot;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogProviderFailure(
                _logger,
                exception,
                provider.ProviderName.Value,
                site.Name);

            return WeatherProviderSnapshot.CreateFailure(
                provider.ProviderName.Value,
                provider.Priority,
                provider.SupportsMarineData,
                _timeProvider.GetUtcNow(),
                string.Empty,
                exception.Message);
        }
    }

    private WeatherSnapshot BuildAggregatedSnapshot(
        DiveSite site,
        IReadOnlyCollection<WeatherProviderSnapshot> successfulSnapshots,
        IReadOnlyList<WeatherProviderSnapshot> allProviderSnapshots,
        DateTimeOffset refreshAttemptUtc)
    {
        var airSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => snapshot.AirTemperatureC is not null,
            false);

        var waterSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => snapshot.WaterTemperatureC is not null,
            true);

        var windSpeedSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => snapshot.WindSpeedMps is not null,
            false);

        var windDirectionSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => snapshot.WindDirectionDeg is not null,
            false);

        var waveSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => snapshot.WaveHeightM is not null,
            true);

        var seaTextSource = SelectBestProviderForMetric(
            successfulSnapshots,
            static snapshot => !string.IsNullOrWhiteSpace(snapshot.SeaStateText),
            true);

        var condition = _seaConditionClassifier.Evaluate(
            ToWaveHeight(waveSource?.WaveHeightM),
            ToWindSpeed(windSpeedSource?.WindSpeedMps));

        DateTimeOffset? observationTimeUtc = successfulSnapshots
            .Select(static snapshot => snapshot.ObservationTimeUtc)
            .Max();

        var sourceProviders = new[]
            {
                airSource?.ProviderName,
                waterSource?.ProviderName,
                windSpeedSource?.ProviderName,
                windDirectionSource?.ProviderName,
                waveSource?.ProviderName,
                seaTextSource?.ProviderName,
            }
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sourceProviderLabel = sourceProviders.Length > 0
            ? string.Join(" + ", sourceProviders)
            : successfulSnapshots
                .OrderBy(static snapshot => snapshot.Priority)
                .First()
                .ProviderName;

        var conditionSummary = BuildConditionSummary(
            condition,
            seaTextSource?.SeaStateText);

        return new WeatherSnapshot(
            site.Id,
            site.Name,
            site.Island,
            airSource?.AirTemperatureC,
            waterSource?.WaterTemperatureC,
            windSpeedSource?.WindSpeedMps,
            windDirectionSource?.WindDirectionDeg,
            waveSource?.WaveHeightM,
            seaTextSource?.SeaStateText,
            condition.Status,
            conditionSummary,
            observationTimeUtc,
            _timeProvider.GetUtcNow(),
            refreshAttemptUtc,
            sourceProviderLabel,
            false,
            allProviderSnapshots);
    }

    private static string BuildConditionSummary(
        SeaConditionEvaluation condition,
        string? seaStateText)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (!string.IsNullOrWhiteSpace(seaStateText))
        {
            return seaStateText;
        }

        return condition.Summary;
    }

    private static WaveHeight? ToWaveHeight(double? waveHeightM)
    {
        if (waveHeightM is null)
        {
            return null;
        }

        return WaveHeight.FromMeters(waveHeightM.Value);
    }

    private static WindSpeed? ToWindSpeed(double? windSpeedMps)
    {
        if (windSpeedMps is null)
        {
            return null;
        }

        return WindSpeed.FromMetersPerSecond(windSpeedMps.Value);
    }

    private static WeatherProviderSnapshot? SelectBestProviderForMetric(
        IReadOnlyCollection<WeatherProviderSnapshot> snapshots,
        Func<WeatherProviderSnapshot, bool> hasMetricPredicate,
        bool preferMarineData)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(hasMetricPredicate);

        var matchingCandidates = snapshots
            .Where(hasMetricPredicate)
            .ToArray();

        if (matchingCandidates.Length == 0)
        {
            return null;
        }

        if (preferMarineData)
        {
            var marineCandidates = matchingCandidates
                .Where(static snapshot => snapshot.SupportsMarineData)
                .ToArray();

            if (marineCandidates.Length > 0)
            {
                matchingCandidates = marineCandidates;
            }
        }

        return matchingCandidates
            .OrderByDescending(static snapshot =>
                snapshot.ObservationTimeUtc ?? DateTimeOffset.MinValue)
            .ThenByDescending(static snapshot => snapshot.QualityScore)
            .ThenBy(static snapshot => snapshot.Priority)
            .First();
    }

    private static WeatherSnapshot CreateUnavailableSnapshot(
        DiveSite site,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots,
        DateTimeOffset refreshAttemptUtc)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(providerSnapshots);
        return WeatherSnapshot.CreateUnavailable(
            site,
            refreshAttemptUtc,
            providerSnapshots);
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "All providers failed for site {SiteId} ({SiteName}).")]
    private static partial void LogAllProvidersFailed(
        ILogger logger,
        int siteId,
        string siteName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Aggregated weather for site {SiteId} using source {SourceProvider}.")]
    private static partial void LogSiteAggregationSucceeded(
        ILogger logger,
        int siteId,
        string sourceProvider);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Provider {ProviderName} for site {SiteName}: success={Success}, " +
            "quality={QualityScore}.")]
    private static partial void LogProviderResult(
        ILogger logger,
        string providerName,
        string siteName,
        bool success,
        double qualityScore);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Provider {ProviderName} failed for site {SiteName}.")]
    private static partial void LogProviderFailure(
        ILogger logger,
        Exception exception,
        string providerName,
        string siteName);
}
