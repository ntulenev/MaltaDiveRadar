using Abstractions;
using Models;

namespace Logic.Services;

/// <summary>
/// Default classifier for tactical dive-condition labels.
/// </summary>
public sealed class SeaConditionClassifier : ISeaConditionClassifier
{
    /// <inheritdoc />
    public SeaConditionEvaluation Evaluate(
        WaveHeight? waveHeight,
        WindSpeed? windSpeed)
    {
        if (waveHeight is null && windSpeed is null)
        {
            return new SeaConditionEvaluation(
                SeaConditionStatus.Unknown,
                "Insufficient marine data");
        }

        if (waveHeight?.IsHighWaves() == true ||
            windSpeed?.IsStrongWind() == true)
        {
            return new SeaConditionEvaluation(
                SeaConditionStatus.Rough,
                "Rough sea conditions");
        }

        if (waveHeight?.IsModerateWaves() == true ||
            windSpeed?.IsModerateWind() == true)
        {
            return new SeaConditionEvaluation(
                SeaConditionStatus.Caution,
                "Moderate chop, caution advised");
        }

        return new SeaConditionEvaluation(
            SeaConditionStatus.Good,
            "Calm sea, light wind");
    }
}

