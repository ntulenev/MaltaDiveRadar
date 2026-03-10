using Abstractions;
using FluentAssertions;
using Logic.Services;
using Moq;

using Logic.Tests.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace Logic.Tests;

[Trait("Category", "Unit")]
public sealed class WeatherRefreshProcessorTests
{
    [Fact(DisplayName = "RunRefreshCycleAsync calls aggregation service once")]
    public async Task RunRefreshCycleAsyncCallsAggregationServiceOnce()
    {
        // Arrange
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);
        var refreshCalls = 0;
        var tokenSource = new CancellationTokenSource();

        aggregationService
            .Setup(service => service.RefreshAllAsync(
                It.Is<CancellationToken>(token => token == tokenSource.Token)))
            .Callback(() => refreshCalls++)
            .ReturnsAsync(Array.Empty<global::Models.WeatherSnapshot>());

        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"));
        using var sut = new WeatherRefreshProcessor(
            NullLogger<WeatherRefreshProcessor>.Instance,
            aggregationService.Object,
            timeProvider);

        // Act
        var exception = await Record.ExceptionAsync(
            () => sut.RunRefreshCycleAsync(tokenSource.Token));

        // Assert
        exception.Should().BeNull();
        refreshCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RunRefreshCycleAsync swallows non-cancellation exceptions")]
    public async Task RunRefreshCycleAsyncSwallowsNonCancellationExceptions()
    {
        // Arrange
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);
        var refreshCalls = 0;

        aggregationService
            .Setup(service => service.RefreshAllAsync(It.IsAny<CancellationToken>()))
            .Callback(() => refreshCalls++)
            .ThrowsAsync(new InvalidOperationException("boom"));

        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"));
        using var sut = new WeatherRefreshProcessor(
            NullLogger<WeatherRefreshProcessor>.Instance,
            aggregationService.Object,
            timeProvider);

        // Act
        var exception = await Record.ExceptionAsync(
            () => sut.RunRefreshCycleAsync(CancellationToken.None));

        // Assert
        exception.Should().BeNull();
        refreshCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RunRefreshCycleAsync skips overlapping refresh cycle calls")]
    public async Task RunRefreshCycleAsyncSkipsOverlappingRefreshCycleCalls()
    {
        // Arrange
        var aggregationService =
            new Mock<IWeatherAggregationService>(MockBehavior.Strict);
        var refreshStarted = new TaskCompletionSource<bool>();
        var releaseRefresh = new TaskCompletionSource<bool>();
        var refreshCalls = 0;

        aggregationService
            .Setup(service => service.RefreshAllAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                refreshCalls++;
                refreshStarted.TrySetResult(true);
            })
            .Returns(async () =>
            {
                await releaseRefresh.Task;
                return Array.Empty<global::Models.WeatherSnapshot>();
            });

        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T12:00:00+00:00"));
        using var sut = new WeatherRefreshProcessor(
            NullLogger<WeatherRefreshProcessor>.Instance,
            aggregationService.Object,
            timeProvider);

        // Act
        var firstCycle = sut.RunRefreshCycleAsync(CancellationToken.None);
        await refreshStarted.Task;

        var secondCycleException = await Record.ExceptionAsync(
            () => sut.RunRefreshCycleAsync(CancellationToken.None));

        releaseRefresh.TrySetResult(true);
        await firstCycle;

        // Assert
        secondCycleException.Should().BeNull();
        refreshCalls.Should().Be(1);
    }
}



