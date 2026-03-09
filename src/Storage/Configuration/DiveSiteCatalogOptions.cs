namespace Storage.Configuration;

/// <summary>
/// Dive-site catalog configuration loaded from application settings.
/// </summary>
public sealed class DiveSiteCatalogOptions
{
    /// <summary>
    /// Configuration section key.
    /// </summary>
    public const string SectionName = "DiveSites";

    /// <summary>
    /// Gets configured dive-site entries.
    /// </summary>
    public IReadOnlyList<DiveSiteOptions> Sites { get; init; } = [];
}
