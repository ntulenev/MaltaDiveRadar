using System.Globalization;
using System.Net;
using System.Text.Json;
using Models;
using Storage.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Storage.Providers;

/// <summary>
/// WeatherAPI provider implementation (optional fallback).
/// </summary>
public sealed class WeatherApiProvider : WeatherProviderBase
{
    private static readonly ProviderName WeatherApiProviderName =
        ProviderName.From("WeatherAPI");

    private readonly IOptions<WeatherRefreshOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherApiProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="options">Weather options.</param>
    /// <param name="timeProvider">Time provider.</param>
    public WeatherApiProvider(
        HttpClient httpClient,
        ILogger<WeatherApiProvider> logger,
        IOptions<WeatherRefreshOptions> options,
        TimeProvider timeProvider)
        : base(httpClient, logger, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public override ProviderName ProviderName => WeatherApiProviderName;

    /// <inheritdoc />
    public override int Priority => _options.Value.Providers.WeatherApi.Priority;

    /// <inheritdoc />
    public override bool SupportsMarineData => false;

    /// <inheritdoc />
    protected override bool IsEnabled => _options.Value.Providers.WeatherApi.Enabled;

    /// <inheritdoc />
    protected override async Task<WeatherProviderSnapshot> ExecuteCoreAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken)
    {
        var apiKey = _options.Value.Providers.WeatherApi.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateFailureSnapshot("WeatherAPI key is missing.");
        }

        var latitudeValue = latitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);
        var longitudeValue = longitude.Degrees.ToString(
            "0.#####",
            CultureInfo.InvariantCulture);

        var endpoint =
            "https://api.weatherapi.com/v1/current.json" +
            $"?key={WebUtility.UrlEncode(apiKey)}" +
            $"&q={latitudeValue},{longitudeValue}&aqi=no";

        var call = await GetPayloadWithRetryAsync(
            new Uri(endpoint, UriKind.Absolute),
            cancellationToken).ConfigureAwait(false);

        if (!call.IsSuccess)
        {
            return CreateFailureSnapshot(
                call.Error ?? "WeatherAPI request failed.",
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
                "WeatherAPI payload did not contain required metrics.",
                call.Payload);
        }

        var qualityScore = 0.35D;
        if (airTemperature is not null)
        {
            qualityScore += 0.3D;
        }

        if (windSpeed is not null)
        {
            qualityScore += 0.25D;
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
            if (!document.RootElement.TryGetProperty("current", out var current))
            {
                return false;
            }

            airTemperature = JsonValueReader.TryReadDouble(current, "temp_c");
            var windSpeedKph = JsonValueReader.TryReadDouble(current, "wind_kph");
            windSpeed = windSpeedKph is null
                ? null
                : windSpeedKph / 3.6D;

            windDirection = JsonValueReader.TryReadInt(current, "wind_degree");
            observationUtc = JsonValueReader.TryReadUnixTime(
                current,
                "last_updated_epoch");

            return airTemperature is not null || windSpeed is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

