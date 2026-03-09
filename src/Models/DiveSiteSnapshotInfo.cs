namespace Models;

/// <summary>
/// Groups dive-site identity values carried in weather snapshots.
/// </summary>
public sealed class DiveSiteSnapshotInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiveSiteSnapshotInfo"/> class.
    /// </summary>
    /// <param name="diveSiteId">Dive-site identifier.</param>
    /// <param name="diveSiteName">Dive-site name value.</param>
    /// <param name="island">Island name value.</param>
    public DiveSiteSnapshotInfo(
        DiveSiteId diveSiteId,
        DiveSiteName diveSiteName,
        IslandName island)
    {
        ArgumentNullException.ThrowIfNull(diveSiteId);
        ArgumentNullException.ThrowIfNull(diveSiteName);
        ArgumentNullException.ThrowIfNull(island);

        DiveSiteId = diveSiteId;
        DiveSiteName = diveSiteName;
        Island = island;
    }

    /// <summary>
    /// Gets dive-site identifier.
    /// </summary>
    public DiveSiteId DiveSiteId { get; }

    /// <summary>
    /// Gets dive-site name.
    /// </summary>
    public DiveSiteName DiveSiteName { get; }

    /// <summary>
    /// Gets island name.
    /// </summary>
    public IslandName Island { get; }

    /// <summary>
    /// Creates snapshot-site info from a dive-site aggregate.
    /// </summary>
    /// <param name="site">Dive-site aggregate.</param>
    /// <returns>Snapshot-site info.</returns>
    public static DiveSiteSnapshotInfo FromDiveSite(DiveSite site)
    {
        ArgumentNullException.ThrowIfNull(site);
        return new DiveSiteSnapshotInfo(site.Id, site.Name, site.Island);
    }
}
