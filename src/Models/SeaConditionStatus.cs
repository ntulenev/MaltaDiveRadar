namespace Models;

/// <summary>
/// Represents tactical sea-condition quality for a dive site.
/// </summary>
public enum SeaConditionStatus
{
    /// <summary>
    /// Condition cannot be determined from available weather data.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Suitable for most recreational dives.
    /// </summary>
    Good = 1,

    /// <summary>
    /// Requires extra attention and planning.
    /// </summary>
    Caution = 2,

    /// <summary>
    /// Conditions are rough and generally unsafe for normal diving.
    /// </summary>
    Rough = 3,
}
