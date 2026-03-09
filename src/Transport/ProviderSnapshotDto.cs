namespace Transport;

/// <summary>
/// Provider snapshot DTO exposed for diagnostics.
/// </summary>
public sealed record ProviderSnapshotDto
{
    /// <summary>
    /// Gets provider name.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets provider priority.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Gets provider quality score.
    /// </summary>
    public required double QualityScore { get; init; }

    /// <summary>
    /// Gets fetch success flag.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets provider observation timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ObservationTimeUtc { get; init; }

    /// <summary>
    /// Gets fetch timestamp in UTC.
    /// </summary>
    public required DateTimeOffset RetrievedAtUtc { get; init; }

    /// <summary>
    /// Gets optional provider error detail.
    /// </summary>
    public string? Error { get; init; }
}

