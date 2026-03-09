namespace Models;

/// <summary>
/// Groups snapshot source and provider provenance metadata.
/// </summary>
public sealed class WeatherSnapshotProvenance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSnapshotProvenance"/> class.
    /// </summary>
    /// <param name="sourceProvider">Source-provider label.</param>
    /// <param name="isStale">Stale flag.</param>
    /// <param name="providerSnapshots">Provider snapshots used for provenance.</param>
    public WeatherSnapshotProvenance(
        SourceProvider sourceProvider,
        bool isStale,
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots)
    {
        ArgumentNullException.ThrowIfNull(sourceProvider);
        ArgumentNullException.ThrowIfNull(providerSnapshots);

        SourceProvider = sourceProvider;
        IsStale = isStale;
        ProviderSnapshots = providerSnapshots.ToArray();
    }

    /// <summary>
    /// Gets source-provider label.
    /// </summary>
    public SourceProvider SourceProvider { get; }

    /// <summary>
    /// Gets stale flag.
    /// </summary>
    public bool IsStale { get; }

    /// <summary>
    /// Gets provider snapshots.
    /// </summary>
    public IReadOnlyList<WeatherProviderSnapshot> ProviderSnapshots { get; }

    /// <summary>
    /// Creates a copy with stale flag enabled.
    /// </summary>
    /// <returns>Updated provenance object.</returns>
    public WeatherSnapshotProvenance MarkStale()
    {
        return new WeatherSnapshotProvenance(
            SourceProvider,
            true,
            ProviderSnapshots);
    }

    /// <summary>
    /// Creates a copy with provider snapshots replaced.
    /// </summary>
    /// <param name="providerSnapshots">Provider snapshots.</param>
    /// <returns>Updated provenance object.</returns>
    public WeatherSnapshotProvenance WithProviderSnapshots(
        IReadOnlyList<WeatherProviderSnapshot> providerSnapshots)
    {
        return new WeatherSnapshotProvenance(
            SourceProvider,
            IsStale,
            providerSnapshots);
    }
}
