using Abstractions;

using FluentAssertions;

using Logic.Services;
using Logic.Tests.Helpers;

using Models;

using Moq;

namespace Logic.Tests;

[Trait("Category", "Unit")]
public sealed class WeatherQueryServiceTests
{
    [Fact(DisplayName = "GetSites returns sites sorted by name")]
    public void GetSitesReturnsSitesSortedByName()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var snapshotRepository =
            new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);

        var getAllSitesCalls = 0;
        var first = TestDataFactory.CreateDiveSite(id: 1, name: "Zulu");
        var second = TestDataFactory.CreateDiveSite(id: 2, name: "Alpha");

        diveSiteCatalog
            .Setup(catalog => catalog.GetAllSites())
            .Callback(() => getAllSitesCalls++)
            .Returns([first, second]);

        var sut = new WeatherQueryService(
            diveSiteCatalog.Object,
            snapshotRepository.Object,
            aggregationService.Object);

        // Act
        var result = sut.GetSites();

        // Assert
        result.Select(site => site.Name.Value)
            .Should().ContainInOrder("Alpha", "Zulu");
        getAllSitesCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetSite delegates to catalog with the same site identifier")]
    public void GetSiteDelegatesToCatalogWithSameSiteIdentifier()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var snapshotRepository =
            new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);

        var siteId = DiveSiteId.FromInt(4);
        var expectedSite = TestDataFactory.CreateDiveSite(id: 4, name: "Blue Hole");
        var getByIdCalls = 0;

        diveSiteCatalog
            .Setup(catalog => catalog.GetById(It.Is<DiveSiteId>(id => id == siteId)))
            .Callback(() => getByIdCalls++)
            .Returns(expectedSite);

        var sut = new WeatherQueryService(
            diveSiteCatalog.Object,
            snapshotRepository.Object,
            aggregationService.Object);

        // Act
        var result = sut.GetSite(siteId);

        // Assert
        result.Should().Be(expectedSite);
        getByIdCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetSiteWeather delegates to repository with the same site identifier")]
    public void GetSiteWeatherDelegatesToRepositoryWithSameSiteIdentifier()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var snapshotRepository =
            new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);

        var siteId = DiveSiteId.FromInt(3);
        var expectedSnapshot = TestDataFactory.CreateWeatherSnapshot(siteId: 3);
        var getWeatherCalls = 0;

        snapshotRepository
            .Setup(repository =>
                repository.GetBySiteId(It.Is<DiveSiteId>(id => id == siteId)))
            .Callback(() => getWeatherCalls++)
            .Returns(expectedSnapshot);

        var sut = new WeatherQueryService(
            diveSiteCatalog.Object,
            snapshotRepository.Object,
            aggregationService.Object);

        // Act
        var result = sut.GetSiteWeather(siteId);

        // Assert
        result.Should().Be(expectedSnapshot);
        getWeatherCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetLatestWeather returns sorted snapshots and refresh timestamp")]
    public void GetLatestWeatherReturnsSortedSnapshotsAndRefreshTimestamp()
    {
        // Arrange
        var diveSiteCatalog = new Mock<IDiveSiteCatalog>(MockBehavior.Strict);
        var snapshotRepository =
            new Mock<IWeatherSnapshotRepository>(MockBehavior.Strict);
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);

        var repositoryCalls = 0;
        var first = TestDataFactory.CreateWeatherSnapshot(siteId: 1, siteName: "Zulu");
        var second = TestDataFactory.CreateWeatherSnapshot(siteId: 2, siteName: "Alpha");
        var refreshUtc = DateTimeOffset.Parse("2026-03-10T14:00:00+00:00");

        snapshotRepository
            .Setup(repository => repository.GetLatest())
            .Callback(() => repositoryCalls++)
            .Returns([first, second]);

        aggregationService
            .SetupGet(service => service.LastRefreshCompletedUtc)
            .Returns(refreshUtc);

        var sut = new WeatherQueryService(
            diveSiteCatalog.Object,
            snapshotRepository.Object,
            aggregationService.Object);

        // Act
        var result = sut.GetLatestWeather();

        // Assert
        result.LastRefreshUtc.Should().Be(refreshUtc);
        result.Snapshots.Select(snapshot => snapshot.DiveSiteName.Value)
            .Should().ContainInOrder("Alpha", "Zulu");
        repositoryCalls.Should().Be(1);
    }
}



