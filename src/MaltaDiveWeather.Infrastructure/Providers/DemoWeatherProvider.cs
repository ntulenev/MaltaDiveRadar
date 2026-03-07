using System.Text.Json;
using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Infrastructure.Providers;

/// <summary>
/// Deterministic mock weather provider used when demo mode is enabled.
/// </summary>
public sealed class DemoWeatherProvider : IWeatherProvider
{
    private readonly TimeProvider _timeProvider;

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
    public string ProviderName => "Demo-Simulator";

    /// <inheritdoc />
    public int Priority => 1;

    /// <inheritdoc />
    public bool SupportsMarineData => true;

    /// <inheritdoc />
    public Task<WeatherProviderSnapshot> GetLatestAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
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

        var payload = JsonSerializer.Serialize(
            new
            {
                mode = "demo",
                coordinates = new { latitude, longitude },
                generatedAtUtc = nowUtc,
                observationTimeUtc = hourStart,
                metrics = new
                {
                    airTemperatureC = airTemperature,
                    waterTemperatureC = waterTemperature,
                    windSpeedMps = windSpeed,
                    windDirectionDeg = windDirection,
                    waveHeightM = waveHeight,
                },
            });

        var snapshot = new WeatherProviderSnapshot
        {
            ProviderName = ProviderName,
            Priority = Priority,
            SupportsMarineData = SupportsMarineData,
            IsSuccess = true,
            AirTemperatureC = Math.Round(airTemperature, 1),
            WaterTemperatureC = Math.Round(waterTemperature, 1),
            WindSpeedMps = Math.Round(windSpeed, 1),
            WindDirectionDeg = windDirection,
            WaveHeightM = Math.Round(waveHeight, 2),
            SeaStateText = DescribeSeaState(waveHeight),
            ObservationTimeUtc = hourStart,
            RetrievedAtUtc = nowUtc,
            RawPayloadJson = payload,
            QualityScore = 0.98D,
        };

        return Task.FromResult(snapshot);
    }

    private static int BuildSeed(
        double latitude,
        double longitude,
        int hour)
    {
        var latComponent = (int)Math.Round(Math.Abs(latitude) * 1000D);
        var lonComponent = (int)Math.Round(Math.Abs(longitude) * 1000D);
        return (latComponent * 31 + lonComponent * 17 + (hour * 13)) & 0x7FFFFFFF;
    }

    private static string DescribeSeaState(double waveHeightM)
    {
        return waveHeightM switch
        {
            < 0.5D => "Calm sea, light wind",
            <= 1.2D => "Moderate chop, caution advised",
            _ => "Rough sea conditions",
        };
    }
}
