using System.Collections.Concurrent;
using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MaltaDiveWeather.Infrastructure.Storage;

/// <summary>
/// Thread-safe in-memory repository for aggregated and provider snapshots.
/// </summary>
public sealed class InMemoryWeatherSnapshotRepository : IWeatherSnapshotRepository
{
    private const string LATEST_CACHE_KEY = "weather:snapshots:latest";
    private const string SITE_CACHE_PREFIX = "weather:snapshot:";
    private const string PROVIDER_CACHE_PREFIX = "weather:provider:";

    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<int, WeatherSnapshot> _snapshots;
    private readonly ConcurrentDictionary<int, IReadOnlyList<WeatherProviderSnapshot>>
        _providerSnapshots;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InMemoryWeatherSnapshotRepository"/> class.
    /// </summary>
    /// <param name="memoryCache">Memory cache.</param>
    public InMemoryWeatherSnapshotRepository(IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);

        _memoryCache = memoryCache;
        _snapshots = new ConcurrentDictionary<int, WeatherSnapshot>();
        _providerSnapshots =
            new ConcurrentDictionary<int, IReadOnlyList<WeatherProviderSnapshot>>();
    }

    /// <inheritdoc />
    public Task UpsertAsync(
        WeatherSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        _snapshots[snapshot.DiveSiteId] = snapshot;

        _memoryCache.Set(
            GetSiteCacheKey(snapshot.DiveSiteId),
            snapshot,
            TimeSpan.FromMinutes(10));

        _memoryCache.Set(
            GetProviderCacheKey(snapshot.DiveSiteId),
            snapshot.ProviderSnapshots,
            TimeSpan.FromMinutes(10));

        _memoryCache.Remove(LATEST_CACHE_KEY);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveProviderSnapshotsAsync(
        int siteId,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerSnapshots);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotsCopy = providerSnapshots.ToArray();
        _providerSnapshots[siteId] = snapshotsCopy;

        _memoryCache.Set(
            GetProviderCacheKey(siteId),
            snapshotsCopy,
            TimeSpan.FromMinutes(10));

        if (_snapshots.TryGetValue(siteId, out var current))
        {
            var updated = current with
            {
                ProviderSnapshots = snapshotsCopy,
            };

            _snapshots[siteId] = updated;
            _memoryCache.Set(
                GetSiteCacheKey(siteId),
                updated,
                TimeSpan.FromMinutes(10));

            _memoryCache.Remove(LATEST_CACHE_KEY);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkStaleAsync(
        int siteId,
        DateTimeOffset refreshAttemptUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_snapshots.TryGetValue(siteId, out var current))
        {
            var stale = current with
            {
                IsStale = true,
                LastRefreshAttemptUtc = refreshAttemptUtc,
            };

            _snapshots[siteId] = stale;
            _memoryCache.Set(
                GetSiteCacheKey(siteId),
                stale,
                TimeSpan.FromMinutes(10));

            _memoryCache.Remove(LATEST_CACHE_KEY);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public WeatherSnapshot? GetBySiteId(int siteId)
    {
        if (_memoryCache.TryGetValue<WeatherSnapshot>(
            GetSiteCacheKey(siteId),
            out var cachedSnapshot))
        {
            return cachedSnapshot;
        }

        if (_snapshots.TryGetValue(siteId, out var snapshot))
        {
            _memoryCache.Set(
                GetSiteCacheKey(siteId),
                snapshot,
                TimeSpan.FromMinutes(10));

            return snapshot;
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<WeatherSnapshot> GetLatest()
    {
        if (_memoryCache.TryGetValue<IReadOnlyCollection<WeatherSnapshot>>(
            LATEST_CACHE_KEY,
            out var cachedSnapshots) && cachedSnapshots is not null)
        {
            return cachedSnapshots;
        }

        var latestSnapshots = _snapshots.Values
            .OrderBy(static snapshot => snapshot.DiveSiteName)
            .ToArray();

        _memoryCache.Set(
            LATEST_CACHE_KEY,
            latestSnapshots,
            TimeSpan.FromMinutes(2));

        return latestSnapshots;
    }

    /// <inheritdoc />
    public IReadOnlyList<WeatherProviderSnapshot> GetProviderSnapshots(int siteId)
    {
        if (_memoryCache.TryGetValue<IReadOnlyList<WeatherProviderSnapshot>>(
            GetProviderCacheKey(siteId),
            out var cachedSnapshots) && cachedSnapshots is not null)
        {
            return cachedSnapshots;
        }

        if (_providerSnapshots.TryGetValue(siteId, out var providerSnapshots))
        {
            _memoryCache.Set(
                GetProviderCacheKey(siteId),
                providerSnapshots,
                TimeSpan.FromMinutes(10));

            return providerSnapshots;
        }

        return Array.Empty<WeatherProviderSnapshot>();
    }

    private static string GetSiteCacheKey(int siteId)
    {
        return $"{SITE_CACHE_PREFIX}{siteId}";
    }

    private static string GetProviderCacheKey(int siteId)
    {
        return $"{PROVIDER_CACHE_PREFIX}{siteId}";
    }
}
