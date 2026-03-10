using FluentAssertions;
using Logic.Services;
using Models;

namespace Logic.Tests;

[Trait("Category", "Unit")]
public sealed class SeaConditionClassifierTests
{
    [Fact(DisplayName = "Evaluate returns Unknown when wave and wind are missing")]
    public void EvaluateReturnsUnknownWhenWaveAndWindAreMissing()
    {
        // Arrange
        var sut = new SeaConditionClassifier();

        // Act
        var result = sut.Evaluate(null, null);

        // Assert
        result.Status.Should().Be(SeaConditionStatus.Unknown);
    }

    [Fact(DisplayName = "Evaluate returns Rough when wave height is high")]
    public void EvaluateReturnsRoughWhenWaveHeightIsHigh()
    {
        // Arrange
        var sut = new SeaConditionClassifier();
        var highWave = WaveHeight.FromMeters(1.3D);

        // Act
        var result = sut.Evaluate(highWave, WindSpeed.FromMetersPerSecond(1D));

        // Assert
        result.Status.Should().Be(SeaConditionStatus.Rough);
    }

    [Fact(DisplayName = "Evaluate returns Caution when wind speed is moderate")]
    public void EvaluateReturnsCautionWhenWindSpeedIsModerate()
    {
        // Arrange
        var sut = new SeaConditionClassifier();
        var wind = WindSpeed.FromMetersPerSecond(5.5D);

        // Act
        var result = sut.Evaluate(WaveHeight.FromMeters(0.2D), wind);

        // Assert
        result.Status.Should().Be(SeaConditionStatus.Caution);
    }
}


