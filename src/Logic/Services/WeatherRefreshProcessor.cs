using System.Diagnostics.CodeAnalysis;

using Abstractions;

using Microsoft.Extensions.Logging;

namespace Logic.Services;

/// <summary>
/// Executes one safe weather refresh cycle.
/// </summary>
public sealed partial class WeatherRefreshProcessor :
    IWeatherRefreshProcessor,
    IDisposable
{
    private readonly ILogger<WeatherRefreshProcessor> _logger;
    private readonly IWeatherAggregationService _weatherAggregationService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherRefreshProcessor"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="weatherAggregationService">Weather aggregation service.</param>
    /// <param name="timeProvider">Time provider.</param>
    public WeatherRefreshProcessor(
        ILogger<WeatherRefreshProcessor> logger,
        IWeatherAggregationService weatherAggregationService,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(weatherAggregationService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger = logger;
        _weatherAggregationService = weatherAggregationService;
        _timeProvider = timeProvider;
        _refreshLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Refresh failures should not crash background execution.")]
    public async Task RunRefreshCycleAsync(CancellationToken cancellationToken)
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

    /// <inheritdoc />
    public void Dispose()
    {
        _refreshLock.Dispose();
    }

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
