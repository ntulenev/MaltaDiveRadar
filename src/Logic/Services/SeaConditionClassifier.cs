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
        return (waveHeight, windSpeed) switch
        {
            (null, null) => SeaConditionEvaluation.CreateUnknown(),

            ({ } wave, _) when wave.IsHighWaves() =>
                SeaConditionEvaluation.CreateRough(),

            (_, { } wind) when wind.IsStrongWind() =>
                SeaConditionEvaluation.CreateRough(),

            ({ } wave, _) when wave.IsModerateWaves() =>
                SeaConditionEvaluation.CreateCaution(),

            (_, { } wind) when wind.IsModerateWind() =>
                SeaConditionEvaluation.CreateCaution(),

            _ => SeaConditionEvaluation.CreateGood(),
        };
    }
}

