using Microsoft.Extensions.Options;

using Models;

using Storage.Configuration;

namespace Storage.Repositories;

/// <summary>
/// Builds initial dive-site domain models from configuration values.
/// </summary>
public sealed class DiveSiteSeedData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiveSiteSeedData"/> class.
    /// </summary>
    /// <param name="options">Dive-site catalog options.</param>
    public DiveSiteSeedData(IOptions<DiveSiteCatalogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <summary>
    /// Creates configured dive-site models.
    /// </summary>
    /// <returns>Configured dive-site models.</returns>
    public IReadOnlyList<DiveSite> Create()
    {
        if (_options.Sites.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one dive site must be configured.");
        }

        return [.. _options.Sites.Select(static site => MapToDomain(site))];
    }

    private static DiveSite MapToDomain(DiveSiteOptions site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return new DiveSite(
            DiveSiteId.FromInt(site.Id),
            DiveSiteName.From(site.Name),
            site.Description,
            IslandName.From(site.Island),
            Latitude.FromDegrees(site.Latitude),
            Longitude.FromDegrees(site.Longitude),
            site.DisplayX,
            site.DisplayY,
            site.IsActive);
    }

    private readonly DiveSiteCatalogOptions _options;
}
