using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace MaltaDiveWeather.Infrastructure.Background;

/// <summary>
/// Periodically refreshes weather snapshots for all active dive sites.
/// </summary>
public sealed partial class WeatherRefreshService : BackgroundService
{
    private readonly ILogger<WeatherRefreshService> _logger;
    private readonly IWeatherAggregationService _weatherAggregationService;
    private readonly IOptions<WeatherRefreshOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherRefreshService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="weatherAggregationService">Weather aggregation service.</param>
    /// <param name="options">Weather refresh options.</param>
    /// <param name="timeProvider">Time provider.</param>
    public WeatherRefreshService(
        ILogger<WeatherRefreshService> logger,
        IWeatherAggregationService weatherAggregationService,
        IOptions<WeatherRefreshOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(weatherAggregationService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger = logger;
        _weatherAggregationService = weatherAggregationService;
        _options = options;
        _timeProvider = timeProvider;
        _refreshLock = new SemaphoreSlim(1, 1);
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

        await RunRefreshCycleSafeAsync(stoppingToken).ConfigureAwait(false);

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

                await RunRefreshCycleSafeAsync(stoppingToken).ConfigureAwait(false);
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

    /// <inheritdoc />
    public override void Dispose()
    {
        _refreshLock.Dispose();
        base.Dispose();
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Refresh failures should not crash the hosted service.")]
    private async Task RunRefreshCycleSafeAsync(CancellationToken cancellationToken)
    {
        if (!await _refreshLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            LogSkippedRefreshDueToOverlap(_logger);

            return;
        }

        var startedUtc = _timeProvider.GetUtcNow();

        try
        {
            LogRefreshStarted(
                _logger,
                startedUtc);

            var snapshots = await _weatherAggregationService.RefreshAllAsync(
                cancellationToken).ConfigureAwait(false);

            var durationMs = (_timeProvider.GetUtcNow() - startedUtc).TotalMilliseconds;

            LogRefreshCompleted(
                _logger,
                snapshots.Count,
                durationMs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRefreshCancelled(_logger);
        }
        catch (Exception exception)
        {
            LogRefreshCycleFailed(_logger, exception);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Error,
        Message = "Unhandled error in weather refresh loop. Loop will continue.")]
    private static partial void LogUnhandledLoopError(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Skipping weather refresh cycle because a previous cycle is still " +
            "running.")]
    private static partial void LogSkippedRefreshDueToOverlap(ILogger logger);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Weather refresh started at {StartedUtc}.")]
    private static partial void LogRefreshStarted(
        ILogger logger,
        DateTimeOffset startedUtc);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Weather refresh completed. SiteCount={SiteCount}; " +
            "DurationMs={DurationMs:0}.")]
    private static partial void LogRefreshCompleted(
        ILogger logger,
        int siteCount,
        double durationMs);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Weather refresh cancellation was requested.")]
    private static partial void LogRefreshCancelled(ILogger logger);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Error,
        Message = "Weather refresh cycle failed. Existing snapshots will remain in " +
            "cache.")]
    private static partial void LogRefreshCycleFailed(
        ILogger logger,
        Exception exception);
}
