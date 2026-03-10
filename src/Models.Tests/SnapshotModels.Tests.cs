using FluentAssertions;
using Models;

using Models.Tests.Helpers;

namespace Models.Tests;

[Trait("Category", "Unit")]
public sealed class SnapshotModelsTests
{
    [Fact(DisplayName = "WeatherProviderSnapshot.CreateSuccess exposes provider and metric values")]
    public void WeatherProviderSnapshotCreateSuccessExposesProviderAndMetricValues()
    {
        // Arrange
        var provider = TestDataFactory.CreateProviderMetadata(
            name: "Open-Meteo",
            priority: 1,
            supportsMarineData: true);
        var metrics = new WeatherMetrics(
            AirTemperature.FromCelsius(20D),
            WaterTemperature.FromCelsius(18D),
            WindSpeed.FromMetersPerSecond(4D),
            WindDirection.FromDegrees(90),
            WaveHeight.FromMeters(0.5D),
            SeaStateText.From("Light chop"));
        var fetchInfo = new ProviderFetchInfo(
            DateTimeOffset.Parse("2026-03-10T10:00:00+00:00"),
            DateTimeOffset.Parse("2026-03-10T10:05:00+00:00"),
            QualityScore.From(0.9D));

        // Act
        var snapshot = WeatherProviderSnapshot.CreateSuccess(
            provider,
            metrics,
            fetchInfo);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.ProviderName.Value.Should().Be("Open-Meteo");
        snapshot.WaveHeightM!.Meters.Should().Be(0.5D);
    }

    [Fact(DisplayName = "WeatherProviderSnapshot.CreateFailure stores error and zero quality")]
    public void WeatherProviderSnapshotCreateFailureStoresErrorAndZeroQuality()
    {
        // Arrange
        var provider = TestDataFactory.CreateProviderMetadata(
            name: "WeatherAPI",
            priority: 2,
            supportsMarineData: false);

        // Act
        var snapshot = WeatherProviderSnapshot.CreateFailure(
            provider,
            DateTimeOffset.Parse("2026-03-10T10:10:00+00:00"),
            "request failed");

        // Assert
        snapshot.IsSuccess.Should().BeFalse();
        snapshot.Error.Should().Be("request failed");
        snapshot.QualityScore.Value.Should().Be(0D);
    }

    [Fact(DisplayName = "WeatherSnapshotCondition.IsSafeForDiving returns true only for good status")]
    public void WeatherSnapshotConditionIsSafeForDivingReturnsTrueOnlyForGoodStatus()
    {
        // Arrange
        var goodCondition = new WeatherSnapshotCondition(
            SeaConditionStatus.Good,
            SeaConditionSummary.From("Calm sea"));
        var roughCondition = new WeatherSnapshotCondition(
            SeaConditionStatus.Rough,
            SeaConditionSummary.From("Rough sea"));

        // Act
        var goodResult = goodCondition.IsSafeForDiving();
        var roughResult = roughCondition.IsSafeForDiving();

        // Assert
        goodResult.Should().BeTrue();
        roughResult.Should().BeFalse();
    }

    [Fact(DisplayName = "WeatherSnapshotTiming constructor throws when last updated is default")]
    public void WeatherSnapshotTimingConstructorThrowsWhenLastUpdatedIsDefault()
    {
        // Arrange
        var refreshAttemptUtc = DateTimeOffset.Parse("2026-03-10T12:00:00+00:00");

        // Act
        var exception = Record.Exception(
            () => new WeatherSnapshotTiming(
                observationTimeUtc: null,
                lastUpdatedUtc: default,
                lastRefreshAttemptUtc: refreshAttemptUtc));

        // Assert
        exception.Should().BeOfType<ArgumentException>();
    }

    [Fact(DisplayName = "WeatherSnapshotTiming.WithRefreshAttempt replaces refresh attempt value")]
    public void WeatherSnapshotTimingWithRefreshAttemptReplacesRefreshAttemptValue()
    {
        // Arrange
        var timing = new WeatherSnapshotTiming(
            observationTimeUtc: DateTimeOffset.Parse("2026-03-10T10:00:00+00:00"),
            lastUpdatedUtc: DateTimeOffset.Parse("2026-03-10T11:00:00+00:00"),
            lastRefreshAttemptUtc: DateTimeOffset.Parse("2026-03-10T11:30:00+00:00"));
        var newRefreshAttempt = DateTimeOffset.Parse("2026-03-10T12:15:00+00:00");

        // Act
        var updated = timing.WithRefreshAttempt(newRefreshAttempt);

        // Assert
        updated.LastRefreshAttemptUtc.Should().Be(newRefreshAttempt);
        updated.LastUpdatedUtc.Should().Be(timing.LastUpdatedUtc);
    }

    [Fact(DisplayName = "WeatherSnapshotProvenance.MarkStale returns stale copy")]
    public void WeatherSnapshotProvenanceMarkStaleReturnsStaleCopy()
    {
        // Arrange
        var provenance = new WeatherSnapshotProvenance(
            SourceProvider.FromLabel("Open-Meteo"),
            isStale: false,
            providerSnapshots: [TestDataFactory.CreateProviderSuccessSnapshot()]);

        // Act
        var stale = provenance.MarkStale();

        // Assert
        stale.IsStale.Should().BeTrue();
        stale.SourceProvider.Should().Be(provenance.SourceProvider);
    }

    [Fact(DisplayName = "WeatherSnapshotProvenance.WithProviderSnapshots replaces provider snapshots")]
    public void WeatherSnapshotProvenanceWithProviderSnapshotsReplacesProviderSnapshots()
    {
        // Arrange
        var original = new WeatherSnapshotProvenance(
            SourceProvider.FromLabel("Open-Meteo"),
            isStale: false,
            providerSnapshots: [TestDataFactory.CreateProviderSuccessSnapshot()]);
        var replacement = new[]
        {
            TestDataFactory.CreateProviderFailureSnapshot(error: "timeout"),
        };

        // Act
        var updated = original.WithProviderSnapshots(replacement);

        // Assert
        updated.ProviderSnapshots.Should().HaveCount(1);
        updated.ProviderSnapshots[0].Error.Should().Be("timeout");
    }

    [Fact(DisplayName = "WeatherSnapshot.CreateUnavailable produces stale unknown snapshot")]
    public void WeatherSnapshotCreateUnavailableProducesStaleUnknownSnapshot()
    {
        // Arrange
        var site = TestDataFactory.CreateDiveSite();
        var refreshAttemptUtc = DateTimeOffset.Parse("2026-03-10T12:00:00+00:00");
        var providerSnapshots = new[]
        {
            TestDataFactory.CreateProviderFailureSnapshot("Provider-A", 1, true, "boom"),
        };

        // Act
        var snapshot = WeatherSnapshot.CreateUnavailable(
            site,
            refreshAttemptUtc,
            providerSnapshots);

        // Assert
        snapshot.IsStale.Should().BeTrue();
        snapshot.SourceProvider.Value.Should().Be("Unavailable");
        snapshot.ConditionStatus.Should().Be(SeaConditionStatus.Unknown);
    }

    [Fact(DisplayName = "WeatherSnapshot.MarkAsStale enables stale flag and updates refresh attempt")]
    public void WeatherSnapshotMarkAsStaleEnablesStaleFlagAndUpdatesRefreshAttempt()
    {
        // Arrange
        var snapshot = TestDataFactory.CreateWeatherSnapshot(isStale: false);
        var refreshAttemptUtc = DateTimeOffset.Parse("2026-03-10T13:00:00+00:00");

        // Act
        var stale = snapshot.MarkAsStale(refreshAttemptUtc);

        // Assert
        stale.IsStale.Should().BeTrue();
        stale.LastRefreshAttemptUtc.Should().Be(refreshAttemptUtc);
    }

    [Fact(DisplayName = "WeatherSnapshot.WithProviderSnapshots replaces provider snapshot diagnostics")]
    public void WeatherSnapshotWithProviderSnapshotsReplacesProviderSnapshotDiagnostics()
    {
        // Arrange
        var snapshot = TestDataFactory.CreateWeatherSnapshot();
        var replacement = new[]
        {
            TestDataFactory.CreateProviderFailureSnapshot(error: "downstream failure"),
        };

        // Act
        var updated = snapshot.WithProviderSnapshots(replacement);

        // Assert
        updated.ProviderSnapshots.Should().HaveCount(1);
        updated.ProviderSnapshots[0].IsSuccess.Should().BeFalse();
    }

    [Fact(DisplayName = "SourceProvider.FromProvider copies provider name value")]
    public void SourceProviderFromProviderCopiesProviderNameValue()
    {
        // Arrange
        var providerName = ProviderName.From("OpenWeather");

        // Act
        var source = SourceProvider.FromProvider(providerName);

        // Assert
        source.Value.Should().Be("OpenWeather");
    }
}



