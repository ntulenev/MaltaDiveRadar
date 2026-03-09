namespace Abstractions;

/// <summary>
/// Executes one weather refresh cycle.
/// </summary>
public interface IWeatherRefreshProcessor
{
    /// <summary>
    /// Runs a single refresh cycle for all active sites.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the refresh-cycle execution.</returns>
    Task RunRefreshCycleAsync(CancellationToken cancellationToken);
}
