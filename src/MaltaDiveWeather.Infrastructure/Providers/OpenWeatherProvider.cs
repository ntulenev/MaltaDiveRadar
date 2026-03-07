using System.Globalization;
using System.Net;
using System.Text.Json;
using MaltaDiveWeather.Domain.Entities;
using MaltaDiveWeather.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaltaDiveWeather.Infrastructure.Providers;

/// <summary>
/// OpenWeather provider implementation (air and wind fallback only).
/// </summary>
public sealed class OpenWeatherProvider : WeatherProviderBase
{
    private readonly IOptions<WeatherRefreshOptions> _options;

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
    public override string ProviderName => "OpenWeather";

    /// <inheritdoc />
    public override int Priority => _options.Value.Providers.OpenWeather.Priority;

    /// <inheritdoc />
    public override bool SupportsMarineData => false;

    /// <inheritdoc />
    protected override bool IsEnabled => _options.Value.Providers.OpenWeather.Enabled;

    /// <inheritdoc />
    protected override async Task<WeatherProviderSnapshot> ExecuteCoreAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var apiKey = _options.Value.Providers.OpenWeather.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateFailureSnapshot("OpenWeather key is missing.");
        }

        var latitudeValue = latitude.ToString("0.#####", CultureInfo.InvariantCulture);
        var longitudeValue = longitude.ToString("0.#####", CultureInfo.InvariantCulture);

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
                call.Error ?? "OpenWeather request failed.",
                call.Payload);
        }

        var parseSuccess = TryParse(
            call.Payload,
            out var airTemperature,
            out var windSpeed,
            out var windDirection,
            out var observationUtc);

        if (!parseSuccess)
        {
            return CreateFailureSnapshot(
                "OpenWeather payload did not contain required metrics.",
                call.Payload);
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
            observationUtc,
            call.Payload,
            qualityScore);
    }

    private static bool TryParse(
        string payload,
        out double? airTemperature,
        out double? windSpeed,
        out int? windDirection,
        out DateTimeOffset? observationUtc)
    {
        airTemperature = null;
        windSpeed = null;
        windDirection = null;
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

            observationUtc = JsonValueReader.TryReadUnixTime(root, "dt");
            return airTemperature is not null || windSpeed is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
