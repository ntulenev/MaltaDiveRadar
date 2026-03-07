using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MaltaDiveWeather.Infrastructure.Providers;

/// <summary>
/// Base weather-provider implementation with retry and timeout resilience.
/// </summary>
public abstract partial class WeatherProviderBase : IWeatherProvider
{
    private const int MAX_RETRY_ATTEMPTS = 3;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherProviderBase"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="timeProvider">Time provider.</param>
    protected WeatherProviderBase(
        HttpClient httpClient,
        ILogger logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <inheritdoc />
    public abstract int Priority { get; }

    /// <inheritdoc />
    public abstract bool SupportsMarineData { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is enabled.
    /// </summary>
    protected abstract bool IsEnabled { get; }

    /// <inheritdoc />
    public async Task<WeatherProviderSnapshot> GetLatestAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return CreateFailureSnapshot(
                "Provider is disabled by configuration.");
        }

        return await ExecuteCoreAsync(
            latitude,
            longitude,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes provider-specific request and mapping logic.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mapped provider snapshot.</returns>
    protected abstract Task<WeatherProviderSnapshot> ExecuteCoreAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);

    /// <summary>
    /// Performs a resilient GET request with retry support.
    /// </summary>
    /// <param name="requestUri">Request URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP call result with payload or error.</returns>
    protected async Task<HttpCallResult> GetPayloadWithRetryAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        string? lastError = null;

        for (var attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    requestUri,
                    cancellationToken).ConfigureAwait(false);

                var payload = await response.Content.ReadAsStringAsync(
                    cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return new HttpCallResult(
                        true,
                        payload,
                        null);
                }

                lastError =
                    $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";

                LogRequestReturnedErrorStatus(
                    _logger,
                    ProviderName,
                    (int)response.StatusCode,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                lastError = "Request timed out.";

                LogRequestTimeout(
                    _logger,
                    exception,
                    ProviderName,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }
            catch (HttpRequestException exception)
            {
                lastError = exception.Message;

                LogRequestFailedWithHttpException(
                    _logger,
                    exception,
                    ProviderName,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }
            catch (InvalidOperationException exception)
            {
                lastError = exception.Message;

                LogRequestFailedWithInvalidOperation(
                    _logger,
                    exception,
                    ProviderName,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }

            if (attempt < MAX_RETRY_ATTEMPTS)
            {
                var delay = TimeSpan.FromMilliseconds(250 * attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return new HttpCallResult(
            false,
            string.Empty,
            lastError ?? "Provider request failed.");
    }

    /// <summary>
    /// Creates a failed provider snapshot.
    /// </summary>
    /// <param name="error">Failure detail.</param>
    /// <param name="rawPayloadJson">Raw payload.</param>
    /// <returns>Failure snapshot.</returns>
    protected WeatherProviderSnapshot CreateFailureSnapshot(
        string error,
        string rawPayloadJson = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new WeatherProviderSnapshot
        {
            ProviderName = ProviderName,
            Priority = Priority,
            SupportsMarineData = SupportsMarineData,
            IsSuccess = false,
            RetrievedAtUtc = _timeProvider.GetUtcNow(),
            RawPayloadJson = rawPayloadJson,
            QualityScore = 0D,
            Error = error,
        };
    }

    /// <summary>
    /// Creates a successful provider snapshot.
    /// </summary>
    /// <param name="airTemperatureC">Air temperature in Celsius.</param>
    /// <param name="waterTemperatureC">Water temperature in Celsius.</param>
    /// <param name="windSpeedMps">Wind speed in meters per second.</param>
    /// <param name="windDirectionDeg">Wind direction in degrees.</param>
    /// <param name="waveHeightM">Wave height in meters.</param>
    /// <param name="seaStateText">Sea state text.</param>
    /// <param name="observationTimeUtc">Observation timestamp in UTC.</param>
    /// <param name="rawPayloadJson">Raw payload JSON.</param>
    /// <param name="qualityScore">Provider quality score.</param>
    /// <returns>Success snapshot.</returns>
    protected WeatherProviderSnapshot CreateSuccessSnapshot(
        double? airTemperatureC,
        double? waterTemperatureC,
        double? windSpeedMps,
        int? windDirectionDeg,
        double? waveHeightM,
        string? seaStateText,
        DateTimeOffset? observationTimeUtc,
        string rawPayloadJson,
        double qualityScore)
    {
        ArgumentNullException.ThrowIfNull(rawPayloadJson);

        return new WeatherProviderSnapshot
        {
            ProviderName = ProviderName,
            Priority = Priority,
            SupportsMarineData = SupportsMarineData,
            IsSuccess = true,
            AirTemperatureC = airTemperatureC,
            WaterTemperatureC = waterTemperatureC,
            WindSpeedMps = windSpeedMps,
            WindDirectionDeg = windDirectionDeg,
            WaveHeightM = waveHeightM,
            SeaStateText = seaStateText,
            ObservationTimeUtc = observationTimeUtc,
            RetrievedAtUtc = _timeProvider.GetUtcNow(),
            RawPayloadJson = rawPayloadJson,
            QualityScore = Math.Clamp(qualityScore, 0D, 1D),
        };
    }

    /// <summary>
    /// Builds a composite raw-payload JSON object from multiple provider calls.
    /// </summary>
    /// <param name="entries">Named payload entries.</param>
    /// <returns>Composite JSON payload.</returns>
    protected static string BuildCompositePayload(
        IReadOnlyDictionary<string, string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return "{}";
        }

        var segments = entries
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Select(pair =>
                $"\"{pair.Key}\":{NormalizeJsonOrNull(pair.Value)}");

        return $"{{{string.Join(",", segments)}}}";
    }

    private static string NormalizeJsonOrNull(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "null";
        }

        var trimmed = payload.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return trimmed;
        }

        return "null";
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Provider {ProviderName} request failed with status {StatusCode} " +
            "on attempt {Attempt}/{MaxAttempts}.")]
    private static partial void LogRequestReturnedErrorStatus(
        ILogger logger,
        string providerName,
        int statusCode,
        int attempt,
        int maxAttempts);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Provider {ProviderName} timed out on attempt {Attempt}/{MaxAttempts}.")]
    private static partial void LogRequestTimeout(
        ILogger logger,
        Exception exception,
        string providerName,
        int attempt,
        int maxAttempts);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Provider {ProviderName} request failed on attempt " +
            "{Attempt}/{MaxAttempts}.")]
    private static partial void LogRequestFailedWithHttpException(
        ILogger logger,
        Exception exception,
        string providerName,
        int attempt,
        int maxAttempts);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Provider {ProviderName} request failed due to invalid operation " +
            "on attempt {Attempt}/{MaxAttempts}.")]
    private static partial void LogRequestFailedWithInvalidOperation(
        ILogger logger,
        Exception exception,
        string providerName,
        int attempt,
        int maxAttempts);

    /// <summary>
    /// Represents resilient HTTP call output.
    /// </summary>
    /// <param name="IsSuccess">Whether the call succeeded.</param>
    /// <param name="Payload">Response payload.</param>
    /// <param name="Error">Optional error detail.</param>
    protected sealed record HttpCallResult(
        bool IsSuccess,
        string Payload,
        string? Error);
}
