using System.ComponentModel.DataAnnotations;

namespace Storage.Configuration;

/// <summary>
/// Single dive-site configuration record.
/// </summary>
public sealed class DiveSiteOptions
{
    /// <summary>
    /// Gets site identifier.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    /// <summary>
    /// Gets site display name.
    /// </summary>
    [Required]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets short site description.
    /// </summary>
    [Required]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets island name.
    /// </summary>
    [Required]
    public string Island { get; init; } = string.Empty;

    /// <summary>
    /// Gets latitude in decimal degrees.
    /// </summary>
    [Range(-90D, 90D)]
    public double Latitude { get; init; }

    /// <summary>
    /// Gets longitude in decimal degrees.
    /// </summary>
    [Range(-180D, 180D)]
    public double Longitude { get; init; }

    /// <summary>
    /// Gets map X coordinate.
    /// </summary>
    [Range(0D, double.MaxValue)]
    public double DisplayX { get; init; }

    /// <summary>
    /// Gets map Y coordinate.
    /// </summary>
    [Range(0D, double.MaxValue)]
    public double DisplayY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this site is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
