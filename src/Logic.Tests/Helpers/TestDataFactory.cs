using Models;

namespace Logic.Tests.Helpers;

public static class TestDataFactory
{
    public static DiveSite CreateDiveSite(
        int id = 1,
        string name = "Cirkewwa",
        string island = "Malta",
        double latitude = 35.9865D,
        double longitude = 14.3389D,
        bool isActive = true)
    {
        return new DiveSite(
            DiveSiteId.FromInt(id),
            DiveSiteName.From(name),
            IslandName.From(island),
            Latitude.FromDegrees(latitude),
            Longitude.FromDegrees(longitude),
            displayX: 100D + id,
            displayY: 200D + id,
            isActive);
    }

    public static WeatherProviderMetadata CreateProviderMetadata(
        string name = "Provider-A",
        int priority = 1,
        bool supportsMarineData = true)
    {
        return new WeatherProviderMetadata(
            ProviderName.From(name),
            ProviderPriority.From(priority),
            supportsMarineData);
    }

    public static WeatherProviderSnapshot CreateProviderSuccessSnapshot(
        string name = "Provider-A",
        int priority = 1,
        bool supportsMarineData = true,
        double? air = 21.3D,
        double? water = 18.2D,
        double? wind = 4.8D,
        int? direction = 120,
        double? wave = 0.4D,
        string? seaText = "Calm sea",
        DateTimeOffset? observationUtc = null,
        DateTimeOffset? retrievedAtUtc = null,
        double quality = 0.8D)
    {
        var provider = CreateProviderMetadata(name, priority, supportsMarineData);
        var metrics = new WeatherMetrics(
            air is null ? null : AirTemperature.FromCelsius(air.Value),
            water is null ? null : WaterTemperature.FromCelsius(water.Value),
            wind is null ? null : WindSpeed.FromMetersPerSecond(wind.Value),
            direction is null ? null : WindDirection.FromDegrees(direction.Value),
            wave is null ? null : WaveHeight.FromMeters(wave.Value),
            seaText is null ? null : SeaStateText.From(seaText));
        var fetchInfo = new ProviderFetchInfo(
            observationUtc,
            retrievedAtUtc ?? DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"),
            QualityScore.FromClamped(quality));

        return WeatherProviderSnapshot.CreateSuccess(provider, metrics, fetchInfo);
    }

    public static WeatherProviderSnapshot CreateProviderFailureSnapshot(
        string name = "Provider-A",
        int priority = 1,
        bool supportsMarineData = true,
        string error = "boom")
    {
        return WeatherProviderSnapshot.CreateFailure(
            CreateProviderMetadata(name, priority, supportsMarineData),
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"),
            error);
    }

    public static WeatherSnapshot CreateWeatherSnapshot(
        int siteId = 1,
        string siteName = "Cirkewwa",
        string sourceProvider = "Provider-A",
        bool isStale = false,
        IReadOnlyList<WeatherProviderSnapshot>? providerSnapshots = null)
    {
        var site = new DiveSiteSnapshotInfo(
            DiveSiteId.FromInt(siteId),
            DiveSiteName.From(siteName),
            IslandName.From("Malta"));
        var metrics = new WeatherMetrics(
            AirTemperature.FromCelsius(20.1D),
            WaterTemperature.FromCelsius(18.4D),
            WindSpeed.FromMetersPerSecond(4.2D),
            WindDirection.FromDegrees(45),
            WaveHeight.FromMeters(0.3D),
            SeaStateText.From("Calm sea"));
        var condition = new WeatherSnapshotCondition(
            SeaConditionStatus.Good,
            SeaConditionSummary.From("Calm sea, light wind"));
        var timing = new WeatherSnapshotTiming(
            DateTimeOffset.Parse("2026-03-10T10:00:00+00:00"),
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"),
            DateTimeOffset.Parse("2026-03-10T12:05:00+00:00"));
        var provenance = new WeatherSnapshotProvenance(
            SourceProvider.FromLabel(sourceProvider),
            isStale,
            providerSnapshots ?? [CreateProviderSuccessSnapshot()]);

        return new WeatherSnapshot(site, metrics, condition, timing, provenance);
    }
}

