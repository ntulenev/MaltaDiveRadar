using MaltaDiveWeather.Domain.Enums;

namespace MaltaDiveWeather.Domain.Services;

/// <summary>
/// Represents sea-condition status and a short operational summary.
/// </summary>
/// <param name="Status">The classified condition status.</param>
/// <param name="Summary">The short condition explanation.</param>
public sealed record SeaConditionEvaluation(
    SeaConditionStatus Status,
    string Summary);
