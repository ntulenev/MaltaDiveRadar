using FluentAssertions;

using Models;

using Models.Tests.Helpers;

namespace Models.Tests;

[Trait("Category", "Unit")]
public sealed class ValueObjectsAndCoreModelsTests
{
    [Fact(DisplayName = "AirTemperature.FromCelsius stores finite in-range values")]
    public void AirTemperatureFromCelsiusStoresFiniteInRangeValues()
    {
        // Arrange
        var celsius = 23.4D;

        // Act
        var result = AirTemperature.FromCelsius(celsius);

        // Assert
        result.Celsius.Should().Be(celsius);
    }

    [Fact(DisplayName = "AirTemperature.FromCelsius throws for out-of-range values")]
    public void AirTemperatureFromCelsiusThrowsForOutOfRangeValues()
    {
        // Arrange
        const double invalid = 100D;

        // Act
        var exception = Record.Exception(
            () => AirTemperature.FromCelsius(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "DiveSite.SetActive returns a new instance with updated state")]
    public void DiveSiteSetActiveReturnsNewInstanceWithUpdatedState()
    {
        // Arrange
        var site = TestDataFactory.CreateDiveSite(isActive: true);

        // Act
        var updated = site.SetActive(false);

        // Assert
        updated.IsActive.Should().BeFalse();
        updated.Id.Should().Be(site.Id);
    }

    [Fact(DisplayName = "DiveSite constructor throws for negative display coordinates")]
    public void DiveSiteConstructorThrowsForNegativeDisplayCoordinates()
    {
        // Arrange
        var id = DiveSiteId.FromInt(1);
        var name = DiveSiteName.From("Site");
        var island = IslandName.From("Malta");
        var latitude = Latitude.FromDegrees(35.0D);
        var longitude = Longitude.FromDegrees(14.0D);

        // Act
        var exception = Record.Exception(
            () => new DiveSite(
                id,
                name,
                island,
                latitude,
                longitude,
                displayX: -1D,
                displayY: 0D));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "DiveSiteId.FromInt throws for non-positive values")]
    public void DiveSiteIdFromIntThrowsForNonPositiveValues()
    {
        // Arrange
        const int invalid = 0;

        // Act
        var exception = Record.Exception(() => DiveSiteId.FromInt(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "DiveSiteName.From trims surrounding whitespace")]
    public void DiveSiteNameFromTrimsSurroundingWhitespace()
    {
        // Arrange
        const string raw = "  Ghar Lapsi  ";

        // Act
        var result = DiveSiteName.From(raw);

        // Assert
        result.Value.Should().Be("Ghar Lapsi");
    }

    [Fact(DisplayName = "DiveSiteSnapshotInfo.FromDiveSite copies identity values")]
    public void DiveSiteSnapshotInfoFromDiveSiteCopiesIdentityValues()
    {
        // Arrange
        var site = TestDataFactory.CreateDiveSite(id: 9, name: "Xatt l-Ahmar");

        // Act
        var snapshotInfo = DiveSiteSnapshotInfo.FromDiveSite(site);

        // Assert
        snapshotInfo.DiveSiteId.Should().Be(site.Id);
        snapshotInfo.DiveSiteName.Should().Be(site.Name);
        snapshotInfo.Island.Should().Be(site.Island);
    }

    [Fact(DisplayName = "IslandName.From normalizes supported names")]
    public void IslandNameFromNormalizesSupportedNames()
    {
        // Arrange
        const string raw = "  gOzO ";

        // Act
        var island = IslandName.From(raw);

        // Assert
        island.Value.Should().Be("Gozo");
    }

    [Fact(DisplayName = "LatestWeather constructor copies snapshots collection")]
    public void LatestWeatherConstructorCopiesSnapshotsCollection()
    {
        // Arrange
        var original = new List<WeatherSnapshot> { TestDataFactory.CreateWeatherSnapshot() };

        // Act
        var latest = new LatestWeather(
            DateTimeOffset.Parse("2026-03-10T12:30:00+00:00"),
            original);
        original.Clear();

        // Assert
        latest.Snapshots.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Latitude.FromDegrees throws outside [-90, 90]")]
    public void LatitudeFromDegreesThrowsOutsideAllowedRange()
    {
        // Arrange
        const double invalid = 90.1D;

        // Act
        var exception = Record.Exception(() => Latitude.FromDegrees(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Longitude.FromDegrees throws outside [-180, 180]")]
    public void LongitudeFromDegreesThrowsOutsideAllowedRange()
    {
        // Arrange
        const double invalid = -180.1D;

        // Act
        var exception = Record.Exception(() => Longitude.FromDegrees(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "ProviderFetchInfo constructor throws when retrieved timestamp is default")]
    public void ProviderFetchInfoConstructorThrowsWhenRetrievedTimestampIsDefault()
    {
        // Arrange
        var score = QualityScore.From(0.5D);

        // Act
        var exception = Record.Exception(
            () => new ProviderFetchInfo(
                DateTimeOffset.Parse("2026-03-10T10:00:00+00:00"),
                default,
                score));

        // Assert
        exception.Should().BeOfType<ArgumentException>();
    }

    [Fact(DisplayName = "ProviderName.From trims surrounding whitespace")]
    public void ProviderNameFromTrimsSurroundingWhitespace()
    {
        // Arrange
        const string raw = "  Open-Meteo  ";

        // Act
        var providerName = ProviderName.From(raw);

        // Assert
        providerName.Value.Should().Be("Open-Meteo");
    }

    [Fact(DisplayName = "ProviderPriority.IsHigherPreferenceThan returns true for lower value")]
    public void ProviderPriorityIsHigherPreferenceThanReturnsTrueForLowerValue()
    {
        // Arrange
        var first = ProviderPriority.From(1);
        var second = ProviderPriority.From(3);

        // Act
        var isHigher = first.IsHigherPreferenceThan(second);

        // Assert
        isHigher.Should().BeTrue();
    }

    [Fact(DisplayName = "QualityScore.FromClamped clamps values into [0, 1]")]
    public void QualityScoreFromClampedClampsValuesIntoExpectedRange()
    {
        // Arrange
        const double aboveRange = 5D;

        // Act
        var clamped = QualityScore.FromClamped(aboveRange);

        // Assert
        clamped.Value.Should().Be(1D);
    }

    [Fact(DisplayName = "QualityScore.IsHighConfidence returns true at threshold")]
    public void QualityScoreIsHighConfidenceReturnsTrueAtThreshold()
    {
        // Arrange
        var score = QualityScore.From(0.75D);

        // Act
        var isHighConfidence = score.IsHighConfidence();

        // Assert
        isHighConfidence.Should().BeTrue();
    }

    [Fact(DisplayName = "SeaConditionEvaluation.CreateGood returns good status and summary")]
    public void SeaConditionEvaluationCreateGoodReturnsGoodStatusAndSummary()
    {
        // Arrange
        // Act
        var evaluation = SeaConditionEvaluation.CreateGood();

        // Assert
        evaluation.Status.Should().Be(SeaConditionStatus.Good);
        evaluation.Summary.Should().Be("Calm sea, light wind");
    }

    [Fact(DisplayName = "SeaConditionStatus enum values remain stable")]
    public void SeaConditionStatusEnumValuesRemainStable()
    {
        // Arrange
        // Act
        var good = (int)SeaConditionStatus.Good;
        var caution = (int)SeaConditionStatus.Caution;
        var rough = (int)SeaConditionStatus.Rough;

        // Assert
        good.Should().Be(1);
        caution.Should().Be(2);
        rough.Should().Be(3);
    }

    [Fact(DisplayName = "SeaConditionSummary.RequiresCaution detects caution keywords")]
    public void SeaConditionSummaryRequiresCautionDetectsKeywords()
    {
        // Arrange
        var summary = SeaConditionSummary.From("Moderate chop, caution advised");

        // Act
        var requiresCaution = summary.RequiresCaution();

        // Assert
        requiresCaution.Should().BeTrue();
    }

    [Fact(DisplayName = "SeaStateText.IndicatesRoughConditions detects rough keywords")]
    public void SeaStateTextIndicatesRoughConditionsDetectsKeywords()
    {
        // Arrange
        var state = SeaStateText.From("Very rough sea");

        // Act
        var rough = state.IndicatesRoughConditions();

        // Assert
        rough.Should().BeTrue();
    }

    [Fact(DisplayName = "SourceProvider.Compose creates distinct composite label")]
    public void SourceProviderComposeCreatesDistinctCompositeLabel()
    {
        // Arrange
        var providers = new[]
        {
            ProviderName.From("Open-Meteo"),
            ProviderName.From("WeatherAPI"),
            ProviderName.From("open-meteo"),
        };

        // Act
        var source = SourceProvider.Compose(providers);

        // Assert
        source.Value.Should().Be("Open-Meteo + WeatherAPI");
        source.IsComposite().Should().BeTrue();
    }

    [Fact(DisplayName = "WaterTemperature.FromCelsius throws outside [-5, 50]")]
    public void WaterTemperatureFromCelsiusThrowsOutsideAllowedRange()
    {
        // Arrange
        const double invalid = -6D;

        // Act
        var exception = Record.Exception(
            () => WaterTemperature.FromCelsius(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "WaveHeight threshold helpers classify moderate and high waves")]
    public void WaveHeightThresholdHelpersClassifyModerateAndHighWaves()
    {
        // Arrange
        var moderate = WaveHeight.FromMeters(0.5D);
        var high = WaveHeight.FromMeters(1.3D);

        // Act
        var moderateResult = moderate.IsModerateWaves();
        var highResult = high.IsHighWaves();

        // Assert
        moderateResult.Should().BeTrue();
        highResult.Should().BeTrue();
    }

    [Fact(DisplayName = "WeatherMetrics.HasMarineData returns true when marine values exist")]
    public void WeatherMetricsHasMarineDataReturnsTrueWhenMarineValuesExist()
    {
        // Arrange
        var metrics = new WeatherMetrics(
            null,
            WaterTemperature.FromCelsius(17D),
            null,
            null,
            null,
            null);

        // Act
        var hasMarineData = metrics.HasMarineData();

        // Assert
        hasMarineData.Should().BeTrue();
    }

    [Fact(DisplayName = "WeatherProviderMetadata.IsHigherPreferenceThan compares by priority")]
    public void WeatherProviderMetadataIsHigherPreferenceThanComparesByPriority()
    {
        // Arrange
        var preferred = TestDataFactory.CreateProviderMetadata(
            name: "A",
            priority: 1);
        var fallback = TestDataFactory.CreateProviderMetadata(
            name: "B",
            priority: 4);

        // Act
        var isHigher = preferred.IsHigherPreferenceThan(fallback);

        // Assert
        isHigher.Should().BeTrue();
    }

    [Fact(DisplayName = "WindDirection.FromDegrees throws outside [0, 359]")]
    public void WindDirectionFromDegreesThrowsOutsideAllowedRange()
    {
        // Arrange
        const int invalid = 360;

        // Act
        var exception = Record.Exception(() => WindDirection.FromDegrees(invalid));

        // Assert
        exception.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "WindSpeed helpers classify moderate and strong wind")]
    public void WindSpeedHelpersClassifyModerateAndStrongWind()
    {
        // Arrange
        var moderate = WindSpeed.FromMetersPerSecond(5D);
        var strong = WindSpeed.FromMetersPerSecond(9.1D);

        // Act
        var moderateResult = moderate.IsModerateWind();
        var strongResult = strong.IsStrongWind();

        // Assert
        moderateResult.Should().BeTrue();
        strongResult.Should().BeTrue();
    }
}



