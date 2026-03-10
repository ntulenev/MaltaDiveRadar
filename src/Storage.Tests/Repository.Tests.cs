using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Models;

using Storage.Configuration;
using Storage.Repositories;

using Storage.Tests.Helpers;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class RepositoryTests
{
    [Fact(DisplayName = "DiveSiteSeedData.Create throws when no sites are configured")]
    public void DiveSiteSeedDataCreateThrowsWhenNoSitesAreConfigured()
    {
        // Arrange
        var options = Options.Create(new DiveSiteCatalogOptions
        {
            Sites = [],
        });
        var sut = new DiveSiteSeedData(options);

        // Act
        var exception = Record.Exception(() => sut.Create());

        // Assert
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact(DisplayName = "DiveSiteSeedData.Create maps configured options into domain objects")]
    public void DiveSiteSeedDataCreateMapsConfiguredOptionsIntoDomainObjects()
    {
        // Arrange
        var options = Options.Create(
            new DiveSiteCatalogOptions
            {
                Sites =
                [
                    new DiveSiteOptions
                    {
                        Id = 4,
                        Name = "Blue Hole",
                        Island = "Gozo",
                        Latitude = 36.04D,
                        Longitude = 14.20D,
                        DisplayX = 150D,
                        DisplayY = 230D,
                        IsActive = true,
                    },
                ],
            });
        var sut = new DiveSiteSeedData(options);

        // Act
        var result = sut.Create();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Value.Should().Be(4);
        result[0].Island.Value.Should().Be("Gozo");
    }

    [Fact(DisplayName = "InMemoryDiveSiteCatalog returns active sites and resolves by identifier")]
    public void InMemoryDiveSiteCatalogReturnsActiveSitesAndResolvesByIdentifier()
    {
        // Arrange
        var options = Options.Create(
            new DiveSiteCatalogOptions
            {
                Sites =
                [
                    new DiveSiteOptions
                    {
                        Id = 1,
                        Name = "Site-A",
                        Island = "Malta",
                        Latitude = 35.9D,
                        Longitude = 14.3D,
                        DisplayX = 10D,
                        DisplayY = 20D,
                        IsActive = true,
                    },
                    new DiveSiteOptions
                    {
                        Id = 2,
                        Name = "Site-B",
                        Island = "Gozo",
                        Latitude = 36.0D,
                        Longitude = 14.2D,
                        DisplayX = 11D,
                        DisplayY = 21D,
                        IsActive = false,
                    },
                ],
            });
        var seedData = new DiveSiteSeedData(options);
        var sut = new InMemoryDiveSiteCatalog(seedData);

        // Act
        var activeSites = sut.GetActiveSites();
        var found = sut.GetById(DiveSiteId.FromInt(1));
        var missing = sut.GetById(DiveSiteId.FromInt(99));

        // Assert
        activeSites.Should().HaveCount(1);
        found.Should().NotBeNull();
        missing.Should().BeNull();
    }

    [Fact(DisplayName = "InMemoryWeatherSnapshotRepository.UpsertAsync stores and returns snapshot")]
    public async Task InMemoryWeatherSnapshotRepositoryUpsertAsyncStoresAndReturnsSnapshot()
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryWeatherSnapshotRepository(cache);
        var snapshot = TestDataFactory.CreateWeatherSnapshot(siteId: 8, siteName: "Wied iz-Zurrieq");

        // Act
        await sut.UpsertAsync(snapshot, CancellationToken.None);
        var result = sut.GetBySiteId(snapshot.DiveSiteId);

        // Assert
        result.Should().NotBeNull();
        result!.DiveSiteId.Should().Be(snapshot.DiveSiteId);
    }

    [Fact(DisplayName = "InMemoryWeatherSnapshotRepository.SaveProviderSnapshotsAsync updates diagnostics list")]
    public async Task InMemoryWeatherSnapshotRepositorySaveProviderSnapshotsAsyncUpdatesDiagnosticsList()
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryWeatherSnapshotRepository(cache);
        var snapshot = TestDataFactory.CreateWeatherSnapshot(siteId: 9, siteName: "Anchor Bay");
        await sut.UpsertAsync(snapshot, CancellationToken.None);
        var providerSnapshots = new[]
        {
            TestDataFactory.CreateProviderFailureSnapshot(error: "network timeout"),
        };

        // Act
        await sut.SaveProviderSnapshotsAsync(
            snapshot.DiveSiteId,
            providerSnapshots,
            CancellationToken.None);
        var result = sut.GetProviderSnapshots(snapshot.DiveSiteId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Error.Should().Be("network timeout");
    }

    [Fact(DisplayName = "InMemoryWeatherSnapshotRepository.MarkStaleAsync marks existing snapshot as stale")]
    public async Task InMemoryWeatherSnapshotRepositoryMarkStaleAsyncMarksExistingSnapshotAsStale()
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryWeatherSnapshotRepository(cache);
        var snapshot = TestDataFactory.CreateWeatherSnapshot(siteId: 10, siteName: "Mgarr ix-Xini");
        await sut.UpsertAsync(snapshot, CancellationToken.None);
        var refreshAttemptUtc = DateTimeOffset.Parse("2026-03-10T16:00:00+00:00");

        // Act
        await sut.MarkStaleAsync(
            snapshot.DiveSiteId,
            refreshAttemptUtc,
            CancellationToken.None);
        var result = sut.GetBySiteId(snapshot.DiveSiteId);

        // Assert
        result.Should().NotBeNull();
        result!.IsStale.Should().BeTrue();
        result.LastRefreshAttemptUtc.Should().Be(refreshAttemptUtc);
    }

    [Fact(DisplayName = "InMemoryWeatherSnapshotRepository.GetLatest returns snapshots sorted by site name")]
    public async Task InMemoryWeatherSnapshotRepositoryGetLatestReturnsSnapshotsSortedBySiteName()
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new InMemoryWeatherSnapshotRepository(cache);
        var zulu = TestDataFactory.CreateWeatherSnapshot(siteId: 20, siteName: "Zulu");
        var alpha = TestDataFactory.CreateWeatherSnapshot(siteId: 21, siteName: "Alpha");
        await sut.UpsertAsync(zulu, CancellationToken.None);
        await sut.UpsertAsync(alpha, CancellationToken.None);

        // Act
        var result = sut.GetLatest();

        // Assert
        result.Select(snapshot => snapshot.DiveSiteName.Value)
            .Should().ContainInOrder("Alpha", "Zulu");
    }
}



