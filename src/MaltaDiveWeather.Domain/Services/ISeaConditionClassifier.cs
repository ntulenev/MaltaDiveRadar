namespace MaltaDiveWeather.Domain.Services;

/// <summary>
/// Classifies marine conditions using normalized wind and wave metrics.
/// </summary>
public interface ISeaConditionClassifier
{
    /// <summary>
    /// Classifies sea conditions for a dive site.
    /// </summary>
    /// <param name="waveHeightM">Wave height in meters.</param>
    /// <param name="windSpeedMps">Wind speed in meters per second.</param>
    /// <returns>The condition status and summary text.</returns>
    SeaConditionEvaluation Evaluate(
        double? waveHeightM,
        double? windSpeedMps);
}
