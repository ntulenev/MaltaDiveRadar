using Abstractions;
using Models;
using Microsoft.Extensions.Logging;

namespace Storage.Providers;

/// <summary>
/// Base weather-provider implementation with retry and timeout resilience.
/// </summary>
public abstract partial class WeatherProviderBase : IWeatherProvider
{
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
    public abstract ProviderName ProviderName { get; }

    /// <inheritdoc />
    public abstract ProviderPriority Priority { get; }

    /// <inheritdoc />
    public abstract bool SupportsMarineData { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is enabled.
    /// </summary>
    protected abstract bool IsEnabled { get; }

    /// <inheritdoc />
    public async Task<WeatherProviderSnapshot> GetLatestAsync(
        Latitude latitude,
        Longitude longitude,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(latitude);
        ArgumentNullException.ThrowIfNull(longitude);

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
        Latitude latitude,
        Longitude longitude,
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
                    ProviderName.Value,
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
                    ProviderName.Value,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }
            catch (HttpRequestException exception)
            {
                lastError = exception.Message;

                LogRequestFailedWithHttpException(
                    _logger,
                    exception,
                    ProviderName.Value,
                    attempt,
                    MAX_RETRY_ATTEMPTS);
            }
            catch (InvalidOperationException exception)
            {
                lastError = exception.Message;

                LogRequestFailedWithInvalidOperation(
                    _logger,
                    exception,
                    ProviderName.Value,
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
    /// <returns>Failure snapshot.</returns>
    protected WeatherProviderSnapshot CreateFailureSnapshot(
        string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        var provider = new WeatherProviderMetadata(
            ProviderName,
            Priority,
            SupportsMarineData);

        return WeatherProviderSnapshot.CreateFailure(
            provider,
            _timeProvider.GetUtcNow(),
            error);
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
        double qualityScore)
    {
        var provider = new WeatherProviderMetadata(
            ProviderName,
            Priority,
            SupportsMarineData);
        var metrics = new WeatherMetrics(
            ToAirTemperature(airTemperatureC),
            ToWaterTemperature(waterTemperatureC),
            ToWindSpeed(windSpeedMps),
            ToWindDirection(windDirectionDeg),
            ToWaveHeight(waveHeightM),
            ToSeaStateText(seaStateText));
        var fetchInfo = new ProviderFetchInfo(
            observationTimeUtc,
            _timeProvider.GetUtcNow(),
            QualityScore.FromClamped(qualityScore));

        return WeatherProviderSnapshot.CreateSuccess(
            provider,
            metrics,
            fetchInfo);
    }

    private static AirTemperature? ToAirTemperature(double? airTemperatureC)
    {
        if (airTemperatureC is null)
        {
            return null;
        }

        return AirTemperature.FromCelsius(airTemperatureC.Value);
    }

    private static WaterTemperature? ToWaterTemperature(double? waterTemperatureC)
    {
        if (waterTemperatureC is null)
        {
            return null;
        }

        return WaterTemperature.FromCelsius(waterTemperatureC.Value);
    }

    private static WindSpeed? ToWindSpeed(double? windSpeedMps)
    {
        if (windSpeedMps is null)
        {
            return null;
        }

        return WindSpeed.FromMetersPerSecond(windSpeedMps.Value);
    }

    private static WindDirection? ToWindDirection(int? windDirectionDeg)
    {
        if (windDirectionDeg is null)
        {
            return null;
        }

        return WindDirection.FromDegrees(windDirectionDeg.Value);
    }

    private static WaveHeight? ToWaveHeight(double? waveHeightM)
    {
        if (waveHeightM is null)
        {
            return null;
        }

        return WaveHeight.FromMeters(waveHeightM.Value);
    }

    private static SeaStateText? ToSeaStateText(string? seaStateText)
    {
        if (string.IsNullOrWhiteSpace(seaStateText))
        {
            return null;
        }

        return SeaStateText.From(seaStateText);
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

    private const int MAX_RETRY_ATTEMPTS = 3;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
}

