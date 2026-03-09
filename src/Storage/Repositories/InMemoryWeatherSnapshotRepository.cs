using System.Collections.Concurrent;

using Abstractions;

using Microsoft.Extensions.Caching.Memory;

using Models;

namespace Storage.Repositories;

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

        var siteId = snapshot.DiveSiteId.Value;
        _snapshots[siteId] = snapshot;

        _memoryCache.Set(
            GetSiteCacheKey(siteId),
            snapshot,
            TimeSpan.FromMinutes(10));

        _memoryCache.Set(
            GetProviderCacheKey(siteId),
            snapshot.ProviderSnapshots,
            TimeSpan.FromMinutes(10));

        _memoryCache.Remove(LATEST_CACHE_KEY);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveProviderSnapshotsAsync(
        DiveSiteId siteId,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(siteId);
        ArgumentNullException.ThrowIfNull(providerSnapshots);
        cancellationToken.ThrowIfCancellationRequested();

        var siteKey = siteId.Value;
        var snapshotsCopy = providerSnapshots.ToArray();

        _providerSnapshots[siteKey] = snapshotsCopy;

        _memoryCache.Set(
            GetProviderCacheKey(siteKey),
            snapshotsCopy,
            TimeSpan.FromMinutes(10));

        if (_snapshots.TryGetValue(siteKey, out var current))
        {
            var updated = current.WithProviderSnapshots(snapshotsCopy);

            _snapshots[siteKey] = updated;

            _memoryCache.Set(
                GetSiteCacheKey(siteKey),
                updated,
                TimeSpan.FromMinutes(10));

            _memoryCache.Remove(LATEST_CACHE_KEY);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkStaleAsync(
        DiveSiteId siteId,
        DateTimeOffset refreshAttemptUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(siteId);
        cancellationToken.ThrowIfCancellationRequested();

        var siteKey = siteId.Value;

        if (_snapshots.TryGetValue(siteKey, out var current))
        {
            var stale = current.MarkAsStale(refreshAttemptUtc);

            _snapshots[siteKey] = stale;

            _memoryCache.Set(
                GetSiteCacheKey(siteKey),
                stale,
                TimeSpan.FromMinutes(10));

            _memoryCache.Remove(LATEST_CACHE_KEY);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public WeatherSnapshot? GetBySiteId(DiveSiteId siteId)
    {
        ArgumentNullException.ThrowIfNull(siteId);

        var siteKey = siteId.Value;

        if (_memoryCache.TryGetValue<WeatherSnapshot>(
            GetSiteCacheKey(siteKey),
            out var cachedSnapshot))
        {
            return cachedSnapshot;
        }

        if (_snapshots.TryGetValue(siteKey, out var snapshot))
        {
            _memoryCache.Set(
                GetSiteCacheKey(siteKey),
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
    public IReadOnlyList<WeatherProviderSnapshot> GetProviderSnapshots(DiveSiteId siteId)
    {
        ArgumentNullException.ThrowIfNull(siteId);

        var siteKey = siteId.Value;

        if (_memoryCache.TryGetValue<IReadOnlyList<WeatherProviderSnapshot>>(
            GetProviderCacheKey(siteKey),
            out var cachedSnapshots) && cachedSnapshots is not null)
        {
            return cachedSnapshots;
        }

        if (_providerSnapshots.TryGetValue(siteKey, out var providerSnapshots))
        {
            _memoryCache.Set(
                GetProviderCacheKey(siteKey),
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
