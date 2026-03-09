using System.Diagnostics.CodeAnalysis;

using Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Storage.Configuration;

namespace MaltaDiveWeather.Web.Services;

/// <summary>
/// Periodically triggers weather refresh processing.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Activated by dependency injection as hosted service.")]
internal sealed partial class WeatherRefreshService : BackgroundService
{
    private readonly ILogger<WeatherRefreshService> _logger;
    private readonly IWeatherRefreshProcessor _weatherRefreshProcessor;
    private readonly IOptions<WeatherRefreshOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherRefreshService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="weatherRefreshProcessor">Refresh processor.</param>
    /// <param name="options">Weather refresh options.</param>
    public WeatherRefreshService(
        ILogger<WeatherRefreshService> logger,
        IWeatherRefreshProcessor weatherRefreshProcessor,
        IOptions<WeatherRefreshOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(weatherRefreshProcessor);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _weatherRefreshProcessor = weatherRefreshProcessor;
        _options = options;
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The background loop must remain alive on unexpected failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(_options.Value.StartupDelaySeconds);
        if (startupDelay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(startupDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        await _weatherRefreshProcessor.RunRefreshCycleAsync(stoppingToken)
            .ConfigureAwait(false);

        var interval = TimeSpan.FromMinutes(_options.Value.RefreshIntervalMinutes);
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken)
                    .ConfigureAwait(false);

                if (!hasNextTick)
                {
                    break;
                }

                await _weatherRefreshProcessor.RunRefreshCycleAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                LogUnhandledLoopError(_logger, exception);
            }
        }
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Error,
        Message = "Unhandled error in weather refresh loop. Loop will continue.")]
    private static partial void LogUnhandledLoopError(
        ILogger logger,
        Exception exception);
}
