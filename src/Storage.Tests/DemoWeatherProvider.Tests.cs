using FluentAssertions;
using Models;
using Storage.Providers;

using Storage.Tests.Helpers;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class DemoWeatherProviderTests
{
    [Fact(DisplayName = "GetLatestAsync returns successful deterministic snapshot")]
    public async Task GetLatestAsyncReturnsSuccessfulDeterministicSnapshot()
    {
        // Arrange
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T14:42:00+00:00"));
        var sut = new DemoWeatherProvider(timeProvider);
        var latitude = Latitude.FromDegrees(35.98D);
        var longitude = Longitude.FromDegrees(14.35D);

        // Act
        var snapshot = await sut.GetLatestAsync(
            latitude,
            longitude,
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.ProviderName.Value.Should().Be("Demo-Simulator");
        snapshot.SeaStateText.Should().NotBeNull();
    }

    [Fact(DisplayName = "GetLatestAsync throws when cancellation is already requested")]
    public async Task GetLatestAsyncThrowsWhenCancellationAlreadyRequested()
    {
        // Arrange
        var timeProvider = new FixedTimeProvider(
            DateTimeOffset.Parse("2026-03-10T14:42:00+00:00"));
        var sut = new DemoWeatherProvider(timeProvider);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        // Act
        var exception = await Record.ExceptionAsync(
            () => sut.GetLatestAsync(
                Latitude.FromDegrees(35.98D),
                Longitude.FromDegrees(14.35D),
                tokenSource.Token));

        // Assert
        exception.Should().BeOfType<OperationCanceledException>();
    }
}



