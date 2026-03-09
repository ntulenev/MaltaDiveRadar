namespace Models;

/// <summary>
/// Groups classified condition status and summary.
/// </summary>
public sealed class WeatherSnapshotCondition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherSnapshotCondition"/> class.
    /// </summary>
    /// <param name="status">Condition status.</param>
    /// <param name="summary">Condition summary.</param>
    public WeatherSnapshotCondition(
        SeaConditionStatus status,
        SeaConditionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        Status = status;
        Summary = summary;
    }

    /// <summary>
    /// Gets condition status.
    /// </summary>
    public SeaConditionStatus Status { get; }

    /// <summary>
    /// Gets condition summary.
    /// </summary>
    public SeaConditionSummary Summary { get; }

    /// <summary>
    /// Gets a value indicating whether condition is safe for diving.
    /// </summary>
    /// <returns>True when condition status is good.</returns>
    public bool IsSafeForDiving()
    {
        return Status == SeaConditionStatus.Good;
    }
}
