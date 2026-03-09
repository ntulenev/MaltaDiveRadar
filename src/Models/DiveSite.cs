namespace Models;

/// <summary>
/// Represents a fixed diving location shown on the Malta tactical map.
/// </summary>
public sealed class DiveSite
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiveSite"/> class.
    /// </summary>
    /// <param name="id">Internal numeric site ID.</param>
    /// <param name="name">Display name.</param>
    /// <param name="island">Island name (Malta, Gozo, or Comino).</param>
    /// <param name="latitude">Site latitude in decimal degrees.</param>
    /// <param name="longitude">Site longitude in decimal degrees.</param>
    /// <param name="displayX">Map X coordinate in SVG space.</param>
    /// <param name="displayY">Map Y coordinate in SVG space.</param>
    /// <param name="isActive">Whether the site is currently enabled.</param>
    public DiveSite(
        DiveSiteId id,
        string name,
        string island,
        Latitude latitude,
        Longitude longitude,
        double displayX,
        double displayY,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(island);
        ArgumentNullException.ThrowIfNull(latitude);
        ArgumentNullException.ThrowIfNull(longitude);

        if (displayX < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayX),
                "Display X coordinate must be non-negative.");
        }

        if (displayY < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayY),
                "Display Y coordinate must be non-negative.");
        }

        Id = id;
        Name = name.Trim();
        Island = island.Trim();
        Latitude = latitude;
        Longitude = longitude;
        DisplayX = displayX;
        DisplayY = displayY;
        IsActive = isActive;
    }

    /// <summary>
    /// Gets the internal ID.
    /// </summary>
    public DiveSiteId Id { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the island name.
    /// </summary>
    public string Island { get; }

    /// <summary>
    /// Gets latitude in decimal degrees.
    /// </summary>
    public Latitude Latitude { get; }

    /// <summary>
    /// Gets longitude in decimal degrees.
    /// </summary>
    public Longitude Longitude { get; }

    /// <summary>
    /// Gets map X coordinate in SVG display space.
    /// </summary>
    public double DisplayX { get; }

    /// <summary>
    /// Gets map Y coordinate in SVG display space.
    /// </summary>
    public double DisplayY { get; }

    /// <summary>
    /// Gets a value indicating whether this site is currently active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Creates a copy with active-state changed.
    /// </summary>
    /// <param name="isActive">New active-state value.</param>
    /// <returns>Updated site instance.</returns>
    public DiveSite SetActive(bool isActive)
    {
        return new DiveSite(
            Id,
            Name,
            Island,
            Latitude,
            Longitude,
            DisplayX,
            DisplayY,
            isActive);
    }
}
