using Abstractions;

using FluentAssertions;

using Logic.Services;

using Moq;

using Models;

using Logic.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace Logic.Tests;

[Trait("Category", "Unit")]
public sealed class WeatherAggregationServiceTests
{
    [Fact(DisplayName = "Constructor throws when provider list is empty")]
    public void ConstructorThrowsWhenProviderListIsEmpty()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var repository = new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var classifier = new Mock<ISeaConditionClassifier>(MockBehavior.Strict);
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"));

        // Act
        var exception = Record.Exception(
            () => new WeatherAggregationService(
                NullLogger<WeatherAggregationService>.Instance,
                diveSiteCatalog.Object,
                repository.Object,
                classifier.Object,
                [],
                timeProvider));

        // Assert
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact(DisplayName = "RefreshAllAsync aggregates active sites and updates LastRefreshCompletedUtc")]
    public async Task RefreshAllAsyncAggregatesActiveSitesAndUpdatesLastRefreshCompletedUtc()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var repository = new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var classifier = new Mock<ISeaConditionClassifier>(MockBehavior.Strict);
        var provider = new Mock<IWeatherProvider>(MockBehavior.Strict);

        var tokenSource = new CancellationTokenSource();
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T12:30:00+00:00"));
        var site = TestDataFactory.CreateDiveSite(id: 7, name: "Blue Lagoon");
        var snapshot = TestDataFactory.CreateProviderSuccessSnapshot(
            name: "Open-Meteo",
            priority: 1,
            supportsMarineData: true,
            air: 19D,
            water: 18D,
            wind: 4.4D,
            direction: 120,
            wave: 0.4D,
            seaText: "Calm sea");

        var getSitesCalls = 0;
        var providerCalls = 0;
        var classifyCalls = 0;
        var upsertCalls = 0;
        var saveProviderCalls = 0;

        diveSiteCatalog
            .Setup(catalog => catalog.GetActiveSites())
            .Callback(() => getSitesCalls++)
            .Returns([site]);

        provider.SetupGet(current => current.ProviderName)
            .Returns(ProviderName.From("Open-Meteo"));
        provider.SetupGet(current => current.Priority)
            .Returns(ProviderPriority.From(1));
        provider.SetupGet(current => current.SupportsMarineData)
            .Returns(true);
        provider
            .Setup(current => current.GetLatestAsync(
                It.Is<Latitude>(latitude => latitude == site.Latitude),
                It.Is<Longitude>(longitude => longitude == site.Longitude),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => providerCalls++)
            .ReturnsAsync(snapshot);

        classifier
            .Setup(current => current.Evaluate(
                It.Is<WaveHeight?>(wave => wave == snapshot.WaveHeightM),
                It.Is<WindSpeed?>(wind => wind == snapshot.WindSpeedMps)))
            .Callback(() => classifyCalls++)
            .Returns(SeaConditionEvaluation.CreateGood());

        repository
            .Setup(current => current.UpsertAsync(
                It.IsAny<WeatherSnapshot>(),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => upsertCalls++)
            .Returns(Task.CompletedTask);

        repository
            .Setup(current => current.SaveProviderSnapshotsAsync(
                It.Is<DiveSiteId>(siteId => siteId == site.Id),
                It.Is<IReadOnlyList<WeatherProviderSnapshot>>(
                    items => items.Count == 1 && items[0] == snapshot),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => saveProviderCalls++)
            .Returns(Task.CompletedTask);

        var sut = new WeatherAggregationService(
            NullLogger<WeatherAggregationService>.Instance,
            diveSiteCatalog.Object,
            repository.Object,
            classifier.Object,
            [provider.Object],
            timeProvider);

        // Act
        var result = await sut.RefreshAllAsync(tokenSource.Token);

        // Assert
        result.Should().HaveCount(1);
        sut.LastRefreshCompletedUtc.Should().Be(timeProvider.GetUtcNow());
        getSitesCalls.Should().Be(1);
        providerCalls.Should().Be(1);
        classifyCalls.Should().Be(1);
        upsertCalls.Should().Be(1);
        saveProviderCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RefreshSiteAsync returns stale snapshot when all providers fail and stale exists")]
    public async Task RefreshSiteAsyncReturnsStaleSnapshotWhenAllProvidersFailAndStaleExists()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var repository = new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var classifier = new Mock<ISeaConditionClassifier>(MockBehavior.Strict);
        var provider = new Mock<IWeatherProvider>(MockBehavior.Strict);

        var tokenSource = new CancellationTokenSource();
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T13:00:00+00:00"));
        var site = TestDataFactory.CreateDiveSite(id: 5, name: "Hondoq");
        var providerFailure = TestDataFactory.CreateProviderFailureSnapshot(
            name: "WeatherAPI",
            priority: 2,
            supportsMarineData: false,
            error: "timeout");
        var staleSnapshot = TestDataFactory.CreateWeatherSnapshot(
            siteId: 5,
            siteName: "Hondoq",
            isStale: true);

        var providerCalls = 0;
        var markStaleCalls = 0;
        var getBySiteIdCalls = 0;

        provider.SetupGet(current => current.ProviderName)
            .Returns(ProviderName.From("WeatherAPI"));
        provider.SetupGet(current => current.Priority)
            .Returns(ProviderPriority.From(2));
        provider.SetupGet(current => current.SupportsMarineData)
            .Returns(false);
        provider
            .Setup(current => current.GetLatestAsync(
                It.Is<Latitude>(latitude => latitude == site.Latitude),
                It.Is<Longitude>(longitude => longitude == site.Longitude),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => providerCalls++)
            .ReturnsAsync(providerFailure);

        repository
            .Setup(current => current.MarkStaleAsync(
                It.Is<DiveSiteId>(siteId => siteId == site.Id),
                It.Is<DateTimeOffset>(value => value != default),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => markStaleCalls++)
            .Returns(Task.CompletedTask);

        repository
            .Setup(current => current.GetBySiteId(It.Is<DiveSiteId>(id => id == site.Id)))
            .Callback(() => getBySiteIdCalls++)
            .Returns(staleSnapshot);

        var sut = new WeatherAggregationService(
            NullLogger<WeatherAggregationService>.Instance,
            diveSiteCatalog.Object,
            repository.Object,
            classifier.Object,
            [provider.Object],
            timeProvider);

        // Act
        var result = await sut.RefreshSiteAsync(site, tokenSource.Token);

        // Assert
        result.Should().Be(staleSnapshot);
        providerCalls.Should().Be(1);
        markStaleCalls.Should().Be(1);
        getBySiteIdCalls.Should().Be(1);
    }
}



