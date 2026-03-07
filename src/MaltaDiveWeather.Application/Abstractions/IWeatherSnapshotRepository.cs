using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Application.Abstractions;

/// <summary>
/// In-memory weather-snapshot persistence contract.
/// </summary>
public interface IWeatherSnapshotRepository
{
    /// <summary>
    /// Upserts an aggregated snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    Task UpsertAsync(
        WeatherSnapshot snapshot,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores provider snapshots used for diagnostics.
    /// </summary>
    /// <param name="siteId">Dive site ID.</param>
    /// <param name="providerSnapshots">Provider snapshots.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    Task SaveProviderSnapshotsAsync(
        int siteId,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an existing snapshot as stale when a refresh fails.
    /// </summary>
    /// <param name="siteId">Dive site ID.</param>
    /// <param name="refreshAttemptUtc">Refresh attempt timestamp in UTC.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    Task MarkStaleAsync(
        int siteId,
        DateTimeOffset refreshAttemptUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest snapshot for a site.
    /// </summary>
    /// <param name="siteId">Dive site ID.</param>
    /// <returns>Latest snapshot or null.</returns>
    WeatherSnapshot? GetBySiteId(int siteId);

    /// <summary>
    /// Gets latest snapshots for all sites.
    /// </summary>
    /// <returns>All snapshots.</returns>
    IReadOnlyCollection<WeatherSnapshot> GetLatest();

    /// <summary>
    /// Gets provider snapshots for one site.
    /// </summary>
    /// <param name="siteId">Dive site ID.</param>
    /// <returns>Provider snapshots.</returns>
    IReadOnlyList<WeatherProviderSnapshot> GetProviderSnapshots(int siteId);
}
