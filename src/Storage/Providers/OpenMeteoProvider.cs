using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Models;

using Storage.Configuration;

namespace Storage.Providers;

/// <summary>
/// Open-Meteo provider implementation (primary source).
/// </summary>
public sealed class OpenMeteoProvider : WeatherProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenMeteoProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="options">Weather options.</param>
    /// <param name="timeProvider">Time provider.</param>
    public OpenMeteoProvider(
        HttpClient httpClient,
        ILogger<OpenMeteoProvider> logger,
        IOptions<WeatherRefreshOptions> options,
        TimeProvider timeProvider)
        : base(httpClient, logger, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public override ProviderName ProviderName => OpenMeteoProviderName;

    /// <inheritdoc />
    public override ProviderPriority Priority =>
        ProviderPriority.From(_options.Value.Providers.OpenMeteo.Priority);

    /// <inheritdoc />
    public override bool SupportsMarineData => true;

    /// <inheritdoc />
    protected override bool IsEnabled => _options.Value.Providers.OpenMeteo.Enabled;

    /// <inheritdoc />
    protected override async Task<WeatherProviderSnapshot> ExecuteCoreAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken)
    {
        var latitudeValue = latitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);
        var longitudeValue = longitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);

        var forecastUri =
            $"https://api.open-meteo.com/v1/forecast?latitude={latitudeValue}" +
            $"&longitude={longitudeValue}" +
            "&current=temperature_2m,wind_speed_10m,wind_direction_10m,weather_code" +
            "&wind_speed_unit=ms&timezone=UTC";

        var marineUri =
            $"https://marine-api.open-meteo.com/v1/marine?latitude={latitudeValue}" +
            $"&longitude={longitudeValue}" +
            "&hourly=wave_height,sea_surface_temperature&forecast_days=1" +
            "&timezone=UTC";

        var forecastCall = await GetPayloadWithRetryAsync(
            new Uri(forecastUri, UriKind.Absolute),
            cancellationToken).ConfigureAwait(false);

        var marineCall = await GetPayloadWithRetryAsync(
            new Uri(marineUri, UriKind.Absolute),
            cancellationToken).ConfigureAwait(false);

        if (!forecastCall.IsSuccess && !marineCall.IsSuccess)
        {
            return CreateFailureSnapshot(
                $"Forecast failed: {forecastCall.Error}; " +
                $"Marine failed: {marineCall.Error}");
        }

        _ = TryParseForecast(
            forecastCall.Payload,
            out var airTemperature,
            out var windSpeed,
            out var windDirection,
            out var generalWeather,
            out var forecastObservationUtc);

        _ = TryParseMarine(
            marineCall.Payload,
            out var waterTemperature,
            out var waveHeight,
            out var marineObservationUtc);

        if (airTemperature is null &&
            windSpeed is null &&
            windDirection is null &&
            waterTemperature is null &&
            waveHeight is null &&
            generalWeather is null)
        {
            return CreateFailureSnapshot(
                "Open-Meteo returned payloads without usable metrics.");
        }

        var observationTimeUtc = MaxTime(
            forecastObservationUtc,
            marineObservationUtc);

        var qualityScore = CalculateQuality(
            airTemperature,
            windSpeed,
            waveHeight,
            waterTemperature,
            observationTimeUtc);

        return CreateSuccessSnapshot(
            airTemperature,
            waterTemperature,
            windSpeed,
            windDirection,
            waveHeight,
            DescribeSeaState(waveHeight),
            generalWeather,
            observationTimeUtc,
            qualityScore);
    }

    private static bool TryParseForecast(
        string payload,
        out double? airTemperature,
        out double? windSpeed,
        out int? windDirection,
        out GeneralWeatherKind? generalWeather,
        out DateTimeOffset? observationUtc)
    {
        airTemperature = null;
        windSpeed = null;
        windDirection = null;
        generalWeather = null;
        observationUtc = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("current", out var current))
            {
                return false;
            }

            airTemperature = JsonValueReader.TryReadDouble(
                current,
                "temperature_2m");

            windSpeed = JsonValueReader.TryReadDouble(
                current,
                "wind_speed_10m");

            windDirection = JsonValueReader.TryReadInt(
                current,
                "wind_direction_10m");

            var weatherCode = JsonValueReader.TryReadInt(
                current,
                "weather_code");
            generalWeather = MapWeatherCode(weatherCode);

            observationUtc = JsonValueReader.TryReadDateTimeOffset(
                current,
                "time");

            return airTemperature is not null ||
                windSpeed is not null ||
                windDirection is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseMarine(
        string payload,
        out double? waterTemperature,
        out double? waveHeight,
        out DateTimeOffset? observationUtc)
    {
        waterTemperature = null;
        waveHeight = null;
        observationUtc = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("hourly", out var hourly))
            {
                return false;
            }

            if (!hourly.TryGetProperty("time", out var times))
            {
                return false;
            }

            hourly.TryGetProperty("wave_height", out var waveHeights);
            hourly.TryGetProperty("sea_surface_temperature", out var waterTemps);

            var index = times.GetArrayLength() - 1;
            while (index >= 0)
            {
                var waveCandidate = JsonValueReader.TryReadArrayDoubleAt(
                    waveHeights,
                    index);

                var waterCandidate = JsonValueReader.TryReadArrayDoubleAt(
                    waterTemps,
                    index);

                if (waveCandidate is not null || waterCandidate is not null)
                {
                    waveHeight = waveCandidate;
                    waterTemperature = waterCandidate;
                    observationUtc = JsonValueReader.TryReadArrayDateTimeAt(
                        times,
                        index);

                    return true;
                }

                index--;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DateTimeOffset? MaxTime(
        DateTimeOffset? first,
        DateTimeOffset? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        return first >= second ? first : second;
    }

    private static double CalculateQuality(
        double? airTemperature,
        double? windSpeed,
        double? waveHeight,
        double? waterTemperature,
        DateTimeOffset? observationUtc)
    {
        var quality = 0.35D;

        if (airTemperature is not null)
        {
            quality += 0.20D;
        }

        if (windSpeed is not null)
        {
            quality += 0.20D;
        }

        if (waveHeight is not null)
        {
            quality += 0.15D;
        }

        if (waterTemperature is not null)
        {
            quality += 0.08D;
        }

        if (observationUtc is not null)
        {
            quality += 0.02D;
        }

        return quality;
    }

    private static string? DescribeSeaState(double? waveHeight)
    {
        if (waveHeight is null)
        {
            return null;
        }

        return waveHeight.Value switch
        {
            < 0.3D => "Calm sea",
            < 0.8D => "Light chop",
            < 1.4D => "Moderate chop",
            < 2.1D => "Rough sea",
            _ => "Very rough sea",
        };
    }

    private static GeneralWeatherKind? MapWeatherCode(int? weatherCode)
    {
        if (weatherCode is null)
        {
            return null;
        }

        return weatherCode.Value switch
        {
            0 => GeneralWeatherKind.Sunny,
            1 or 2 => GeneralWeatherKind.PartlyCloudy,
            3 => GeneralWeatherKind.Cloudy,
            45 or 48 => GeneralWeatherKind.Fog,
            51 or 53 or 55 or 56 or 57 => GeneralWeatherKind.Drizzle,
            61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => GeneralWeatherKind.Rain,
            71 or 73 or 75 or 77 or 85 or 86 => GeneralWeatherKind.Snow,
            95 or 96 or 99 => GeneralWeatherKind.Thunderstorm,
            _ => GeneralWeatherKind.Mixed,
        };
    }

    private static readonly ProviderName OpenMeteoProviderName =
        ProviderName.From("Open-Meteo");

    private readonly IOptions<WeatherRefreshOptions> _options;
}

