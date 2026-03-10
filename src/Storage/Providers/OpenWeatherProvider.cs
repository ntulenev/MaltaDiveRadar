using System.Globalization;
using System.Net;
using System.Text.Json;
using Models;
using Storage.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Storage.Providers;

/// <summary>
/// OpenWeather provider implementation (air and wind fallback only).
/// </summary>
public sealed class OpenWeatherProvider : WeatherProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenWeatherProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="options">Weather options.</param>
    /// <param name="timeProvider">Time provider.</param>
    public OpenWeatherProvider(
        HttpClient httpClient,
        ILogger<OpenWeatherProvider> logger,
        IOptions<WeatherRefreshOptions> options,
        TimeProvider timeProvider)
        : base(httpClient, logger, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public override ProviderName ProviderName => OpenWeatherProviderName;

    /// <inheritdoc />
    public override ProviderPriority Priority =>
        ProviderPriority.From(_options.Value.Providers.OpenWeather.Priority);

    /// <inheritdoc />
    public override bool SupportsMarineData => false;

    /// <inheritdoc />
    protected override bool IsEnabled => _options.Value.Providers.OpenWeather.Enabled;

    /// <inheritdoc />
    protected override async Task<WeatherProviderSnapshot> ExecuteCoreAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken)
    {
        var apiKey = _options.Value.Providers.OpenWeather.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateFailureSnapshot("OpenWeather key is missing.");
        }

        var latitudeValue = latitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);
        var longitudeValue = longitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);

        var endpoint =
            "https://api.openweathermap.org/data/2.5/weather" +
            $"?lat={latitudeValue}&lon={longitudeValue}" +
            "&units=metric" +
            $"&appid={WebUtility.UrlEncode(apiKey)}";

        var call = await GetPayloadWithRetryAsync(
            new Uri(endpoint, UriKind.Absolute),
            cancellationToken).ConfigureAwait(false);

        if (!call.IsSuccess)
        {
            return CreateFailureSnapshot(
                call.Error ?? "OpenWeather request failed.");
        }

        var parseSuccess = TryParse(
            call.Payload,
            out var airTemperature,
            out var windSpeed,
            out var windDirection,
            out var generalWeather,
            out var observationUtc);

        if (!parseSuccess)
        {
            return CreateFailureSnapshot(
                "OpenWeather payload did not contain required metrics.");
        }

        var qualityScore = 0.30D;
        if (airTemperature is not null)
        {
            qualityScore += 0.3D;
        }

        if (windSpeed is not null)
        {
            qualityScore += 0.3D;
        }

        if (observationUtc is not null)
        {
            qualityScore += 0.1D;
        }

        return CreateSuccessSnapshot(
            airTemperature,
            null,
            windSpeed,
            windDirection,
            null,
            null,
            generalWeather,
            observationUtc,
            qualityScore);
    }

    private static bool TryParse(
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

            if (root.TryGetProperty("main", out var main))
            {
                airTemperature = JsonValueReader.TryReadDouble(main, "temp");
            }

            if (root.TryGetProperty("wind", out var wind))
            {
                windSpeed = JsonValueReader.TryReadDouble(wind, "speed");
                windDirection = JsonValueReader.TryReadInt(wind, "deg");
            }

            if (root.TryGetProperty("weather", out var weatherArray) &&
                weatherArray.ValueKind is JsonValueKind.Array &&
                weatherArray.GetArrayLength() > 0)
            {
                var firstWeather = weatherArray[0];
                string? weatherMainText = null;
                string? weatherDescriptionText = null;

                if (firstWeather.TryGetProperty("main", out var weatherMain) &&
                    weatherMain.ValueKind is JsonValueKind.String)
                {
                    weatherMainText = weatherMain.GetString();
                }

                if (firstWeather.TryGetProperty("description", out var description) &&
                    description.ValueKind is JsonValueKind.String)
                {
                    weatherDescriptionText = description.GetString();
                }

                generalWeather = GeneralWeatherClassifier.FromText(
                    weatherDescriptionText ?? weatherMainText);
            }

            observationUtc = JsonValueReader.TryReadUnixTime(root, "dt");
            return airTemperature is not null || windSpeed is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static readonly ProviderName OpenWeatherProviderName =
        ProviderName.From("OpenWeather");

    private readonly IOptions<WeatherRefreshOptions> _options;
}

