using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Models;
using Storage.Providers;

using Storage.Tests.Helpers;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class WeatherProviderBaseTests
{
    [Fact(DisplayName = "GetLatestAsync returns disabled failure when provider is disabled")]
    public async Task GetLatestAsyncReturnsDisabledFailureWhenProviderIsDisabled()
    {
        // Arrange
        var handler = new SequenceHttpMessageHandler([]);
        var provider = new TestProvider(
            new HttpClient(handler),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-03-10T12:00:00+00:00")))
        {
            Enabled = false,
        };

        // Act
        var snapshot = await provider.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeFalse();
        snapshot.Error.Should().Be("Provider is disabled by configuration.");
        provider.ExecuteCoreCalls.Should().Be(0);
    }

    [Fact(DisplayName = "GetLatestAsync executes core logic when provider is enabled")]
    public async Task GetLatestAsyncExecutesCoreLogicWhenProviderIsEnabled()
    {
        // Arrange
        var handler = new SequenceHttpMessageHandler([]);
        var provider = new TestProvider(
            new HttpClient(handler),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-03-10T12:00:00+00:00")))
        {
            Enabled = true,
        };

        // Act
        var snapshot = await provider.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        provider.ExecuteCoreCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetPayloadWithRetryAsync retries failed HTTP responses and returns HttpCallResult")]
    public async Task GetPayloadWithRetryAsyncRetriesFailedResponsesAndReturnsHttpCallResult()
    {
        // Arrange
        var handler = new SequenceHttpMessageHandler(
            [
                _ => new System.Net.Http.HttpResponseMessage(
                    System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("x"),
                },
                _ => new System.Net.Http.HttpResponseMessage(
                    System.Net.HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("y"),
                },
                _ => new System.Net.Http.HttpResponseMessage(
                    System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("z"),
                },
            ]);
        var provider = new TestProvider(
            new HttpClient(handler),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-03-10T12:00:00+00:00")))
        {
            Enabled = true,
        };
        var uri = new Uri("https://example.com/weather", UriKind.Absolute);

        // Act
        var result = await provider.CallGetPayloadWithRetryAsync(uri, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("HTTP");
        result.ResultTypeName.Should().Be("HttpCallResult");
        handler.SendCalls.Should().Be(3);
    }

    private sealed class TestProvider : WeatherProviderBase
    {
        public TestProvider(
            HttpClient httpClient,
            TimeProvider timeProvider)
            : base(httpClient, NullLogger<TestProvider>.Instance, timeProvider)
        {
        }

        public bool Enabled { get; set; } = true;

        public int ExecuteCoreCalls { get; private set; }

        public override ProviderName ProviderName => ProviderName.From("TestProvider");

        public override ProviderPriority Priority => ProviderPriority.From(1);

        public override bool SupportsMarineData => true;

        protected override bool IsEnabled => Enabled;

        protected override Task<WeatherProviderSnapshot> ExecuteCoreAsync(
            Latitude latitude,
            Longitude longitude,
            CancellationToken cancellationToken)
        {
            ExecuteCoreCalls++;

            var snapshot = CreateSuccessSnapshot(
                airTemperatureC: 20D,
                waterTemperatureC: 18D,
                windSpeedMps: 4D,
                windDirectionDeg: 90,
                waveHeightM: 0.5D,
                seaStateText: "Calm sea",
                generalWeather: GeneralWeatherKind.Sunny,
                observationTimeUtc: DateTimeOffset.Parse("2026-03-10T11:00:00+00:00"),
                qualityScore: 0.8D);

            return Task.FromResult(snapshot);
        }

        public async Task<PayloadResult> CallGetPayloadWithRetryAsync(
            Uri requestUri,
            CancellationToken cancellationToken)
        {
            var result = await GetPayloadWithRetryAsync(requestUri, cancellationToken);
            return new PayloadResult(
                result.IsSuccess,
                result.Payload,
                result.Error,
                result.GetType().Name);
        }
    }

    private sealed record PayloadResult(
        bool IsSuccess,
        string Payload,
        string? Error,
        string ResultTypeName);
}



