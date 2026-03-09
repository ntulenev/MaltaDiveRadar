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
}
