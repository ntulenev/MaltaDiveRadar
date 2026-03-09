using Abstractions;
using Models;

namespace Storage.Providers;

/// <summary>
/// Deterministic mock weather provider used when demo mode is enabled.
/// </summary>
public sealed class DemoWeatherProvider : IWeatherProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemoWeatherProvider"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider.</param>
    public DemoWeatherProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public ProviderName ProviderName => DemoProviderName;

    /// <inheritdoc />
    public ProviderPriority Priority => DemoPriority;

    /// <inheritdoc />
    public bool SupportsMarineData => true;

    /// <inheritdoc />
    public Task<WeatherProviderSnapshot> GetLatestAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(latitude);
        ArgumentNullException.ThrowIfNull(longitude);

        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = _timeProvider.GetUtcNow();
        var hourStart = new DateTimeOffset(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            nowUtc.Hour,
            0,
            0,
            TimeSpan.Zero);

        var seed = BuildSeed(latitude, longitude, hourStart.Hour);

        var mode = seed % 3;
        var waveHeight = mode switch
        {
            0 => 0.22D + (seed % 18) / 100D,
            1 => 0.62D + (seed % 40) / 100D,
            _ => 1.30D + (seed % 55) / 100D,
        };

        var windSpeed = mode switch
        {
            0 => 2.9D + (seed % 20) / 10D,
            1 => 5.2D + (seed % 35) / 10D,
            _ => 9.4D + (seed % 44) / 10D,
        };

        var airTemperature = 16D + ((seed % 160) / 10D);
        var waterTemperature = 14D + ((seed % 120) / 10D);
        var windDirection = seed % 360;
        var airTemperatureValue = AirTemperature.FromCelsius(
            Math.Round(airTemperature, 1));
        var waterTemperatureValue = WaterTemperature.FromCelsius(
            Math.Round(waterTemperature, 1));
        var windSpeedValue = WindSpeed.FromMetersPerSecond(
            Math.Round(windSpeed, 1));
        var windDirectionValue = WindDirection.FromDegrees(windDirection);
        var waveHeightValue = WaveHeight.FromMeters(
            Math.Round(waveHeight, 2));

        var provider = new WeatherProviderMetadata(
            ProviderName,
            Priority,
            SupportsMarineData);
        var metrics = new WeatherMetrics(
            airTemperatureValue,
            waterTemperatureValue,
            windSpeedValue,
            windDirectionValue,
            waveHeightValue,
            SeaStateText.From(DescribeSeaState(waveHeightValue)));
        var fetchInfo = new ProviderFetchInfo(
            hourStart,
            nowUtc,
            QualityScore.From(0.98D));

        return Task.FromResult(
            WeatherProviderSnapshot.CreateSuccess(
                provider,
                metrics,
                fetchInfo));
    }

    private static int BuildSeed(
        Latitude latitude,
        Longitude longitude,
        int hour)
    {
        ArgumentNullException.ThrowIfNull(latitude);
        ArgumentNullException.ThrowIfNull(longitude);

        var latComponent = (int)Math.Round(Math.Abs(latitude.Degrees) * 1000D);
        var lonComponent = (int)Math.Round(Math.Abs(longitude.Degrees) * 1000D);
        return (latComponent * 31 + lonComponent * 17 + (hour * 13)) & 0x7FFFFFFF;
    }

    private static string DescribeSeaState(WaveHeight waveHeight)
    {
        ArgumentNullException.ThrowIfNull(waveHeight);

        return waveHeight.Meters switch
        {
            < 0.5D => "Calm sea, light wind",
            <= 1.2D => "Moderate chop, caution advised",
            _ => "Rough sea conditions",
        };
    }

    private static readonly ProviderName DemoProviderName =
        ProviderName.From("Demo-Simulator");
    private static readonly ProviderPriority DemoPriority = ProviderPriority.From(1);

    private readonly TimeProvider _timeProvider;
}

