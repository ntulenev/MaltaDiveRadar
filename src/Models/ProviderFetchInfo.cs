namespace Models;

/// <summary>
/// Groups acquisition metadata for one provider fetch.
/// </summary>
public sealed class ProviderFetchInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderFetchInfo"/> class.
    /// </summary>
    /// <param name="observationTimeUtc">Provider observation timestamp in UTC.</param>
    /// <param name="retrievedAtUtc">Fetch completion timestamp in UTC.</param>
    /// <param name="qualityScore">Quality score value.</param>
    public ProviderFetchInfo(
        DateTimeOffset? observationTimeUtc,
        DateTimeOffset retrievedAtUtc,
        QualityScore qualityScore)
    {
        ArgumentNullException.ThrowIfNull(qualityScore);

        if (retrievedAtUtc == default)
        {
            throw new ArgumentException(
                "Retrieved timestamp must be defined.",
                nameof(retrievedAtUtc));
        }

        ObservationTimeUtc = observationTimeUtc;
        RetrievedAtUtc = retrievedAtUtc;
        QualityScore = qualityScore;
    }

    /// <summary>
    /// Gets provider observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; }

    /// <summary>
    /// Gets fetch completion timestamp in UTC.
    /// </summary>
    public DateTimeOffset RetrievedAtUtc { get; }

    /// <summary>
    /// Gets quality-score value.
    /// </summary>
    public QualityScore QualityScore { get; }
}
