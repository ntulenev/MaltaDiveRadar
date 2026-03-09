namespace Transport;

/// <summary>
/// API DTO for dive-site metadata.
/// </summary>
public sealed record DiveSiteDto
{
    /// <summary>
    /// Gets site ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets site name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets island name.
    /// </summary>
    public required string Island { get; init; }

    /// <summary>
    /// Gets latitude in decimal degrees.
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// Gets longitude in decimal degrees.
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Gets map X coordinate.
    /// </summary>
    public required double DisplayX { get; init; }

    /// <summary>
    /// Gets map Y coordinate.
    /// </summary>
    public required double DisplayY { get; init; }

    /// <summary>
    /// Gets a value indicating whether the site is active.
    /// </summary>
    public required bool IsActive { get; init; }
}

