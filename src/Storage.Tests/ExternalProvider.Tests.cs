using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Models;

using Storage.Configuration;
using Storage.Providers;

using Storage.Tests.Helpers;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class ExternalProviderTests
{
    [Fact(DisplayName = "OpenWeatherProvider returns failure when API key is missing")]
    public async Task OpenWeatherProviderReturnsFailureWhenApiKeyIsMissing()
    {
        // Arrange
        var handler = new SequenceHttpMessageHandler([]);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(
            new WeatherRefreshOptions
            {
                Providers = new WeatherProviderPoolOptions
                {
                    OpenWeather = new OpenWeatherProviderOptions
                    {
                        Enabled = true,
                        ApiKey = string.Empty,
                    },
                },
            });
        var sut = new OpenWeatherProvider(
            httpClient,
            NullLogger<OpenWeatherProvider>.Instance,
            options,
            TimeProvider.System);

        // Act
        var snapshot = await sut.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeFalse();
        snapshot.Error.Should().Be("OpenWeather key is missing.");
        handler.SendCalls.Should().Be(0);
    }

    [Fact(DisplayName = "OpenWeatherProvider maps successful payload into snapshot")]
    public async Task OpenWeatherProviderMapsSuccessfulPayloadIntoSnapshot()
    {
        // Arrange
        var payload =
            """{"main":{"temp":22.4},"wind":{"speed":6.3,"deg":200},"dt":1710000000}""";
        var handler = new SequenceHttpMessageHandler(
            [
                _ => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(payload),
                },
            ]);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(
            new WeatherRefreshOptions
            {
                Providers = new WeatherProviderPoolOptions
                {
                    OpenWeather = new OpenWeatherProviderOptions
                    {
                        Enabled = true,
                        ApiKey = "secret",
                    },
                },
            });
        var sut = new OpenWeatherProvider(
            httpClient,
            NullLogger<OpenWeatherProvider>.Instance,
            options,
            TimeProvider.System);

        // Act
        var snapshot = await sut.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.AirTemperatureC!.Celsius.Should().Be(22.4D);
        snapshot.WindDirectionDeg!.Degrees.Should().Be(200);
        handler.SendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "WeatherApiProvider maps successful payload with kph to mps conversion")]
    public async Task WeatherApiProviderMapsSuccessfulPayloadWithKphToMpsConversion()
    {
        // Arrange
        var payload =
            """{"current":{"temp_c":19.2,"wind_kph":18.0,"wind_degree":90,"last_updated_epoch":1710000000}}""";
        var handler = new SequenceHttpMessageHandler(
            [
                _ => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(payload),
                },
            ]);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(
            new WeatherRefreshOptions
            {
                Providers = new WeatherProviderPoolOptions
                {
                    WeatherApi = new WeatherApiProviderOptions
                    {
                        Enabled = true,
                        ApiKey = "secret",
                    },
                },
            });
        var sut = new WeatherApiProvider(
            httpClient,
            NullLogger<WeatherApiProvider>.Instance,
            options,
            TimeProvider.System);

        // Act
        var snapshot = await sut.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.WindSpeedMps!.MetersPerSecond.Should().BeApproximately(5D, 0.0001D);
        snapshot.ProviderName.Value.Should().Be("WeatherAPI");
        handler.SendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "OpenMeteoProvider merges forecast and marine payloads into one snapshot")]
    public async Task OpenMeteoProviderMergesForecastAndMarinePayloadsIntoOneSnapshot()
    {
        // Arrange
        var forecastPayload =
            """{"current":{"temperature_2m":20.5,"wind_speed_10m":5.1,"wind_direction_10m":120,"time":"2026-03-10T12:00:00Z"}}""";
        var marinePayload =
            """{"hourly":{"time":["2026-03-10T11:00:00Z","2026-03-10T12:00:00Z"],"wave_height":[0.1,0.4],"sea_surface_temperature":[17.9,18.2]}}""";
        var handler = new SequenceHttpMessageHandler(
            [
                _ => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(forecastPayload),
                },
                _ => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(marinePayload),
                },
            ]);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new WeatherRefreshOptions());
        var sut = new OpenMeteoProvider(
            httpClient,
            NullLogger<OpenMeteoProvider>.Instance,
            options,
            TimeProvider.System);

        // Act
        var snapshot = await sut.GetLatestAsync(
            Latitude.FromDegrees(35.9D),
            Longitude.FromDegrees(14.3D),
            CancellationToken.None);

        // Assert
        snapshot.IsSuccess.Should().BeTrue();
        snapshot.WaterTemperatureC!.Celsius.Should().Be(18.2D);
        snapshot.WaveHeightM!.Meters.Should().Be(0.4D);
        snapshot.ProviderName.Value.Should().Be("Open-Meteo");
        handler.SendCalls.Should().Be(2);
    }
}



