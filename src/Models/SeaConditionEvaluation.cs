namespace Models;

/// <summary>
/// Represents sea-condition status and a short operational summary.
/// </summary>
public sealed record SeaConditionEvaluation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeaConditionEvaluation"/> class.
    /// </summary>
    /// <param name="status">The classified condition status.</param>
    /// <param name="summary">The short condition explanation.</param>
    public SeaConditionEvaluation(
        SeaConditionStatus status,
        string summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        Status = status;
        Summary = summary.Trim();
    }

    /// <summary>
    /// Gets the classified condition status.
    /// </summary>
    public SeaConditionStatus Status { get; }

    /// <summary>
    /// Gets the short condition explanation.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Creates an evaluation for unknown sea conditions.
    /// </summary>
    /// <returns>Unknown-condition evaluation.</returns>
    public static SeaConditionEvaluation CreateUnknown()
    {
        return new SeaConditionEvaluation(
            SeaConditionStatus.Unknown,
            "Insufficient marine data");
    }

    /// <summary>
    /// Creates an evaluation for rough sea conditions.
    /// </summary>
    /// <returns>Rough-condition evaluation.</returns>
    public static SeaConditionEvaluation CreateRough()
    {
        return new SeaConditionEvaluation(
            SeaConditionStatus.Rough,
            "Rough sea conditions");
    }

    /// <summary>
    /// Creates an evaluation for caution sea conditions.
    /// </summary>
    /// <returns>Caution-condition evaluation.</returns>
    public static SeaConditionEvaluation CreateCaution()
    {
        return new SeaConditionEvaluation(
            SeaConditionStatus.Caution,
            "Moderate chop, caution advised");
    }

    /// <summary>
    /// Creates an evaluation for good sea conditions.
    /// </summary>
    /// <returns>Good-condition evaluation.</returns>
    public static SeaConditionEvaluation CreateGood()
    {
        return new SeaConditionEvaluation(
            SeaConditionStatus.Good,
            "Calm sea, light wind");
    }
}
