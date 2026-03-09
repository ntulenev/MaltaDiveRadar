using Abstractions;
using Models;

namespace Storage.Repositories;

/// <summary>
/// In-memory static catalog of Malta and Gozo dive sites.
/// </summary>
public sealed class InMemoryDiveSiteCatalog : IDiveSiteCatalog
{
    private readonly IReadOnlyList<DiveSite> _allSites;
    private readonly IReadOnlyList<DiveSite> _activeSites;
    private readonly IReadOnlyDictionary<int, DiveSite> _siteById;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDiveSiteCatalog"/> class.
    /// </summary>
    public InMemoryDiveSiteCatalog()
    {
        _allSites = DiveSiteSeedData.Create();
        _activeSites = _allSites
            .Where(static site => site.IsActive)
            .ToArray();

        _siteById = _allSites.ToDictionary(static site => site.Id.Value);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<DiveSite> GetAllSites() => _allSites;

    /// <inheritdoc />
    public IReadOnlyCollection<DiveSite> GetActiveSites() => _activeSites;

    /// <inheritdoc />
    public DiveSite? GetById(DiveSiteId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _siteById.GetValueOrDefault(id.Value);
    }
}

