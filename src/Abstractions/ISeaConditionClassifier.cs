using Models;

namespace Abstractions;

/// <summary>
/// Classifies marine conditions using normalized wind and wave metrics.
/// </summary>
public interface ISeaConditionClassifier
{
    /// <summary>
    /// Classifies sea conditions for a dive site.
    /// </summary>
    /// <param name="waveHeight">Wave-height value object.</param>
    /// <param name="windSpeed">Wind-speed value object.</param>
    /// <returns>The condition status and summary text.</returns>
    SeaConditionEvaluation Evaluate(
        WaveHeight? waveHeight,
        WindSpeed? windSpeed);
}

