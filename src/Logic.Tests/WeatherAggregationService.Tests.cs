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

    [Fact(DisplayName = "RefreshSiteAsync creates unavailable snapshot when all providers fail and stale is missing")]
    public async Task RefreshSiteAsyncCreatesUnavailableSnapshotWhenAllProvidersFailAndStaleIsMissing()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var repository = new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var classifier = new Mock<ISeaConditionClassifier>(MockBehavior.Strict);
        var provider = new Mock<IWeatherProvider>(MockBehavior.Strict);

        var tokenSource = new CancellationTokenSource();
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T13:30:00+00:00"));
        var site = TestDataFactory.CreateDiveSite(id: 8, name: "Blue Hole");
        var providerFailure = TestDataFactory.CreateProviderFailureSnapshot(
            name: "OpenWeather",
            priority: 1,
            supportsMarineData: false,
            error: "upstream error");

        var markStaleCalls = 0;
        var getBySiteIdCalls = 0;
        var upsertCalls = 0;
        var saveProviderCalls = 0;
        WeatherSnapshot? upsertedSnapshot = null;
        IReadOnlyList<WeatherProviderSnapshot>? savedProviderSnapshots = null;

        provider.SetupGet(current => current.ProviderName)
            .Returns(ProviderName.From("OpenWeather"));
        provider.SetupGet(current => current.Priority)
            .Returns(ProviderPriority.From(1));
        provider.SetupGet(current => current.SupportsMarineData)
            .Returns(false);
        provider
            .Setup(current => current.GetLatestAsync(
                It.Is<Latitude>(latitude => latitude == site.Latitude),
                It.Is<Longitude>(longitude => longitude == site.Longitude),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
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
            .Returns((WeatherSnapshot?)null);

        repository
            .Setup(current => current.UpsertAsync(
                It.IsAny<WeatherSnapshot>(),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback<WeatherSnapshot, CancellationToken>(
                (snapshot, _) =>
                {
                    upsertCalls++;
                    upsertedSnapshot = snapshot;
                })
            .Returns(Task.CompletedTask);

        repository
            .Setup(current => current.SaveProviderSnapshotsAsync(
                It.Is<DiveSiteId>(siteId => siteId == site.Id),
                It.IsAny<IReadOnlyList<WeatherProviderSnapshot>>(),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback<
                DiveSiteId,
                IReadOnlyList<WeatherProviderSnapshot>,
                CancellationToken>(
                (_, snapshots, _) =>
                {
                    saveProviderCalls++;
                    savedProviderSnapshots = snapshots;
                })
            .Returns(Task.CompletedTask);

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
        result.Should().NotBeNull();
        result.Should().BeSameAs(upsertedSnapshot);
        result!.IsStale.Should().BeTrue();
        result.SourceProvider.Value.Should().Be("Unavailable");
        result.ConditionStatus.Should().Be(SeaConditionStatus.Unknown);

        savedProviderSnapshots.Should().NotBeNull();
        savedProviderSnapshots!.Should().ContainSingle(
            snapshot => snapshot == providerFailure);

        markStaleCalls.Should().Be(1);
        getBySiteIdCalls.Should().Be(1);
        upsertCalls.Should().Be(1);
        saveProviderCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RefreshSiteAsync continues when one provider throws and another succeeds")]
    public async Task RefreshSiteAsyncContinuesWhenOneProviderThrowsAndAnotherSucceeds()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var repository = new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var classifier = new Mock<ISeaConditionClassifier>(MockBehavior.Strict);
        var failingProvider = new Mock<IWeatherProvider>(MockBehavior.Strict);
        var successProvider = new Mock<IWeatherProvider>(MockBehavior.Strict);

        var tokenSource = new CancellationTokenSource();
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T14:00:00+00:00"));
        var site = TestDataFactory.CreateDiveSite(id: 9, name: "Paradise Bay");
        var successSnapshot = TestDataFactory.CreateProviderSuccessSnapshot(
            name: "Open-Meteo",
            priority: 2,
            supportsMarineData: true,
            air: 20D,
            water: 19D,
            wind: 4.2D,
            direction: 110,
            wave: 0.3D,
            seaText: "Calm sea");

        WeatherSnapshot? upsertedSnapshot = null;
        IReadOnlyList<WeatherProviderSnapshot>? savedProviderSnapshots = null;

        failingProvider.SetupGet(current => current.ProviderName)
            .Returns(ProviderName.From("WeatherAPI"));
        failingProvider.SetupGet(current => current.Priority)
            .Returns(ProviderPriority.From(1));
        failingProvider.SetupGet(current => current.SupportsMarineData)
            .Returns(false);
        failingProvider
            .Setup(current => current.GetLatestAsync(
                It.Is<Latitude>(latitude => latitude == site.Latitude),
                It.Is<Longitude>(longitude => longitude == site.Longitude),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .ThrowsAsync(new InvalidOperationException("provider crashed"));

        successProvider.SetupGet(current => current.ProviderName)
            .Returns(ProviderName.From("Open-Meteo"));
        successProvider.SetupGet(current => current.Priority)
            .Returns(ProviderPriority.From(2));
        successProvider.SetupGet(current => current.SupportsMarineData)
            .Returns(true);
        successProvider
            .Setup(current => current.GetLatestAsync(
                It.Is<Latitude>(latitude => latitude == site.Latitude),
                It.Is<Longitude>(longitude => longitude == site.Longitude),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .ReturnsAsync(successSnapshot);

        classifier
            .Setup(current => current.Evaluate(
                It.Is<WaveHeight?>(wave => wave == successSnapshot.WaveHeightM),
                It.Is<WindSpeed?>(wind => wind == successSnapshot.WindSpeedMps)))
            .Returns(SeaConditionEvaluation.CreateGood());

        repository
            .Setup(current => current.UpsertAsync(
                It.IsAny<WeatherSnapshot>(),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback<WeatherSnapshot, CancellationToken>(
                (snapshot, _) => upsertedSnapshot = snapshot)
            .Returns(Task.CompletedTask);

        repository
            .Setup(current => current.SaveProviderSnapshotsAsync(
                It.Is<DiveSiteId>(siteId => siteId == site.Id),
                It.IsAny<IReadOnlyList<WeatherProviderSnapshot>>(),
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback<
                DiveSiteId,
                IReadOnlyList<WeatherProviderSnapshot>,
                CancellationToken>(
                (_, snapshots, _) => savedProviderSnapshots = snapshots)
            .Returns(Task.CompletedTask);

        var sut = new WeatherAggregationService(
            NullLogger<WeatherAggregationService>.Instance,
            diveSiteCatalog.Object,
            repository.Object,
            classifier.Object,
            [successProvider.Object, failingProvider.Object],
            timeProvider);

        // Act
        var result = await sut.RefreshSiteAsync(site, tokenSource.Token);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(upsertedSnapshot);
        result!.IsStale.Should().BeFalse();
        result.SourceProvider.Value.Should().Be("Open-Meteo");

        savedProviderSnapshots.Should().NotBeNull();
        savedProviderSnapshots!.Should().HaveCount(2);
        savedProviderSnapshots.Should().ContainSingle(
            snapshot =>
                snapshot.ProviderName.Value == "WeatherAPI" &&
                snapshot.IsSuccess == false &&
                snapshot.Error == "provider crashed");
    }
}



