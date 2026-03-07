using MaltaDiveWeather.Domain.Enums;

namespace MaltaDiveWeather.Domain.Services;

/// <summary>
/// Default classifier for tactical dive-condition labels.
/// </summary>
public sealed class SeaConditionClassifier : ISeaConditionClassifier
{
    /// <inheritdoc />
    public SeaConditionEvaluation Evaluate(
        double? waveHeightM,
        double? windSpeedMps)
    {
        if (waveHeightM is null && windSpeedMps is null)
        {
            return new SeaConditionEvaluation(
                SeaConditionStatus.Unknown,
                "Insufficient marine data");
        }

        var wave = waveHeightM ?? 0D;
        var wind = windSpeedMps ?? 0D;

        if (wave > 1.2D || wind > 9D)
        {
            return new SeaConditionEvaluation(
                SeaConditionStatus.Rough,
                "Rough sea conditions");
        }

        if ((wave >= 0.5D && wave <= 1.2D) ||
            (wind >= 5D && wind <= 9D))
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
