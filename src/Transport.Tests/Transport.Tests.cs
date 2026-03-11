using FluentAssertions;

using Transport;

using Transport.Tests.Helpers;

namespace Transport.Tests;

[Trait("Category", "Unit")]
public sealed class TransportTests
{
    [Fact(DisplayName = "ApiDtoMapper.MapDiveSite maps domain site fields")]
    public void ApiDtoMapperMapDiveSiteMapsDomainSiteFields()
    {
        // Arrange
        var site = TestDataFactory.CreateDiveSite(id: 11, name: "Ghar Lapsi", island: "Malta");

        // Act
        var dto = ApiDtoMapper.MapDiveSite(site);

        // Assert
        dto.Id.Should().Be(11);
        dto.Name.Should().Be("Ghar Lapsi");
        dto.Description.Should().Be("Rocky site with easy shore access.");
        dto.Island.Should().Be("Malta");
    }

    [Fact(DisplayName = "ApiDtoMapper.MapSnapshot maps wind cardinal and provider snapshots")]
    public void ApiDtoMapperMapSnapshotMapsWindCardinalAndProviderSnapshots()
    {
        // Arrange
        var providerSnapshots = new[]
        {
            TestDataFactory.CreateProviderSuccessSnapshot(
                name: "Provider-B",
                priority: 2),
            TestDataFactory.CreateProviderSuccessSnapshot(
                name: "Provider-A",
                priority: 1),
        };
        var snapshot = TestDataFactory.CreateWeatherSnapshot(
            siteId: 12,
            siteName: "Anchor Bay",
            sourceProvider: "Provider-A + Provider-B",
            providerSnapshots: providerSnapshots);

        // Act
        var dto = ApiDtoMapper.MapSnapshot(snapshot);

        // Assert
        dto.DiveSiteId.Should().Be(12);
        dto.WindDirectionCardinal.Should().Be("NE");
        dto.ProviderSnapshots.Select(item => item.ProviderName)
            .Should().ContainInOrder("Provider-A", "Provider-B");
    }

    [Fact(DisplayName = "ApiDtoMapper.MapLatestWeather sorts snapshots by site name")]
    public void ApiDtoMapperMapLatestWeatherSortsSnapshotsBySiteName()
    {
        // Arrange
        var latest = new global::Models.LatestWeather(
            DateTimeOffset.Parse("2026-03-10T15:00:00+00:00"),
            [
                TestDataFactory.CreateWeatherSnapshot(siteId: 30, siteName: "Zulu"),
                TestDataFactory.CreateWeatherSnapshot(siteId: 31, siteName: "Alpha"),
            ]);

        // Act
        var dto = ApiDtoMapper.MapLatestWeather(latest);

        // Assert
        dto.LastRefreshUtc.Should().Be(latest.LastRefreshUtc);
        dto.Snapshots.Select(snapshot => snapshot.DiveSiteName)
            .Should().ContainInOrder("Alpha", "Zulu");
    }

    [Fact(DisplayName = "DiveSiteDto record stores assigned values")]
    public void DiveSiteDtoRecordStoresAssignedValues()
    {
        // Arrange
        var dto = new DiveSiteDto
        {
            Id = 1,
            Name = "Site",
            Description = "Short description.",
            Island = "Malta",
            Latitude = 35.9D,
            Longitude = 14.3D,
            DisplayX = 10D,
            DisplayY = 20D,
            IsActive = true,
        };

        // Act
        var actual = dto.Name;

        // Assert
        actual.Should().Be("Site");
    }

    [Fact(DisplayName = "LatestWeatherResponseDto record stores assigned values")]
    public void LatestWeatherResponseDtoRecordStoresAssignedValues()
    {
        // Arrange
        var dto = new LatestWeatherResponseDto
        {
            LastRefreshUtc = DateTimeOffset.Parse("2026-03-10T16:00:00+00:00"),
            Snapshots = Array.Empty<WeatherSnapshotDto>(),
        };

        // Act
        var actual = dto.LastRefreshUtc;

        // Assert
        actual.Should().Be(DateTimeOffset.Parse("2026-03-10T16:00:00+00:00"));
    }

    [Fact(DisplayName = "ProviderSnapshotDto record stores assigned values")]
    public void ProviderSnapshotDtoRecordStoresAssignedValues()
    {
        // Arrange
        var dto = new ProviderSnapshotDto
        {
            ProviderName = "Provider-A",
            Priority = 1,
            QualityScore = 0.9D,
            IsSuccess = true,
            RetrievedAtUtc = DateTimeOffset.Parse("2026-03-10T16:10:00+00:00"),
            ObservationTimeUtc = DateTimeOffset.Parse("2026-03-10T16:00:00+00:00"),
            Error = null,
        };

        // Act
        var actual = dto.QualityScore;

        // Assert
        actual.Should().Be(0.9D);
    }

    [Fact(DisplayName = "WeatherSnapshotDto record stores assigned values")]
    public void WeatherSnapshotDtoRecordStoresAssignedValues()
    {
        // Arrange
        var dto = new WeatherSnapshotDto
        {
            DiveSiteId = 44,
            DiveSiteName = "Site-X",
            Island = "Gozo",
            ConditionStatus = "Good",
            ConditionSummary = "Calm",
            LastUpdatedUtc = DateTimeOffset.Parse("2026-03-10T16:20:00+00:00"),
            LastRefreshAttemptUtc = DateTimeOffset.Parse("2026-03-10T16:21:00+00:00"),
            SourceProvider = "Open-Meteo",
            IsStale = false,
        };

        // Act
        var actual = dto.DiveSiteId;

        // Assert
        actual.Should().Be(44);
    }
}



