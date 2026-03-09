namespace Models;

/// <summary>
/// Represents normalized metric quality in range [0,1].
/// </summary>
public sealed record QualityScore
{
    private QualityScore(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Quality score must be a finite number.");
        }

        if (value is < 0D or > 1D)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Quality score must be in range [0, 1].");
        }

        Value = value;
    }

    /// <summary>
    /// Gets score value in range [0,1].
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets zero quality score.
    /// </summary>
    public static QualityScore Zero { get; } = new(0D);

    /// <summary>
    /// Creates a validated quality-score value.
    /// </summary>
    /// <param name="value">Raw quality-score value.</param>
    /// <returns>Validated quality-score value object.</returns>
    public static QualityScore From(double value)
    {
        return new QualityScore(value);
    }

    /// <summary>
    /// Creates a clamped quality-score value.
    /// </summary>
    /// <param name="value">Raw quality-score value.</param>
    /// <returns>Validated clamped quality-score value object.</returns>
    public static QualityScore FromClamped(double value)
    {
        return new QualityScore(Math.Clamp(value, 0D, 1D));
    }

    /// <summary>
    /// Gets a value indicating whether score is high confidence.
    /// </summary>
    /// <returns>True when score is at least 0.75.</returns>
    public bool IsHighConfidence()
    {
        return Value >= HIGH_CONFIDENCE_THRESHOLD;
    }

    /// <summary>
    /// Gets a value indicating whether this score is better than another.
    /// </summary>
    /// <param name="other">Other quality-score value.</param>
    /// <returns>True when this score is greater than the other one.</returns>
    public bool IsBetterThan(QualityScore other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Value > other.Value;
    }

    private const double HIGH_CONFIDENCE_THRESHOLD = 0.75D;
}
