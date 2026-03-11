using Abstractions;

using FluentAssertions;

using MaltaDiveWeather.Web.Configuration;
using MaltaDiveWeather.Web.Services;
using MaltaDiveWeather.Web.Startup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace MaltaDiveWeather.Web.Tests;

[Trait("Category", "Unit")]
public sealed class WebLayerTests
{
    [Fact(DisplayName = "WeatherRefreshScheduleOptions exposes expected default values")]
    public void WeatherRefreshScheduleOptionsExposesExpectedDefaultValues()
    {
        // Arrange
        // Act
        var options = new WeatherRefreshScheduleOptions();

        // Assert
        options.RefreshIntervalMinutes.Should().Be(60);
        options.StartupDelaySeconds.Should().Be(5);
    }

    [Fact(DisplayName = "WeatherRefreshService runs one immediate refresh cycle on start")]
    public async Task WeatherRefreshServiceRunsOneImmediateRefreshCycleOnStart()
    {
        // Arrange
        var processor = new Mock<IWeatherRefreshProcessor>(MockBehavior.Strict);
        var refreshCalls = 0;
        processor
            .Setup(current => current.RunRefreshCycleAsync(
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => refreshCalls++)
            .Returns(Task.CompletedTask);

        var options = Options.Create(
            new WeatherRefreshScheduleOptions
            {
                StartupDelaySeconds = 0,
                RefreshIntervalMinutes = 60,
            });

        using var sut = new WeatherRefreshService(
            NullLogger<WeatherRefreshService>.Instance,
            processor.Object,
            options);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        refreshCalls.Should().Be(1);
    }

    [Fact(DisplayName = "WeatherRefreshService skips refresh when stopped during startup delay")]
    public async Task WeatherRefreshServiceSkipsRefreshWhenStoppedDuringStartupDelay()
    {
        // Arrange
        var processor = new Mock<IWeatherRefreshProcessor>(MockBehavior.Strict);
        var refreshCalls = 0;
        processor
            .Setup(current => current.RunRefreshCycleAsync(It.IsAny<CancellationToken>()))
            .Callback(() => refreshCalls++)
            .Returns(Task.CompletedTask);

        var options = Options.Create(
            new WeatherRefreshScheduleOptions
            {
                StartupDelaySeconds = 30,
                RefreshIntervalMinutes = 60,
            });
        using var sut = new WeatherRefreshService(
            NullLogger<WeatherRefreshService>.Instance,
            processor.Object,
            options);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        refreshCalls.Should().Be(0);
    }

    [Fact(DisplayName = "AddLogic registers core logic services and hosted refresh service")]
    public void AddLogicRegistersCoreLogicServicesAndHostedRefreshService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogic();

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IWeatherQueryService));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IWeatherAggregationService));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IWeatherRefreshProcessor));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact(DisplayName = "AddStorage in demo mode registers DemoWeatherProvider as IWeatherProvider")]
    public void AddStorageInDemoModeRegistersDemoWeatherProviderAsIWeatherProvider()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            ["WeatherRefresh:DemoMode"] = "true",
            ["WeatherRefresh:HttpTimeoutSeconds"] = "20",
            ["DiveSites:Sites:0:Id"] = "1",
            ["DiveSites:Sites:0:Name"] = "Cirkewwa",
            ["DiveSites:Sites:0:Description"] = "Simple shore dive with reef and caves.",
            ["DiveSites:Sites:0:Island"] = "Malta",
            ["DiveSites:Sites:0:Latitude"] = "35.99",
            ["DiveSites:Sites:0:Longitude"] = "14.34",
            ["DiveSites:Sites:0:DisplayX"] = "123",
            ["DiveSites:Sites:0:DisplayY"] = "321",
            ["DiveSites:Sites:0:IsActive"] = "true",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);

        // Act
        services.AddStorage(configuration);
        using var provider = services.BuildServiceProvider();
        var weatherProviders = provider.GetServices<IWeatherProvider>().ToArray();

        // Assert
        weatherProviders.Should().ContainSingle();
        weatherProviders[0].ProviderName.Value.Should().Be("Demo-Simulator");
    }

    [Fact(DisplayName = "StartupHelpers.CreateApplication builds a web application instance")]
    public async Task StartupHelpersCreateApplicationBuildsWebApplicationInstance()
    {
        // Arrange
        // Act
        await using var app = StartupHelpers.CreateApplication([]);

        // Assert
        app.Should().NotBeNull();
    }

    [Fact(DisplayName = "StartupHelpers.ConfigureMiddleware throws for null application")]
    public void StartupHelpersConfigureMiddlewareThrowsForNullApplication()
    {
        // Arrange
        // Act
        var exception = Record.Exception(
            () => StartupHelpers.ConfigureMiddleware(null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Fact(DisplayName = "StartupHelpers.MapEndpoints throws for null application")]
    public void StartupHelpersMapEndpointsThrowsForNullApplication()
    {
        // Arrange
        // Act
        var exception = Record.Exception(
            () => StartupHelpers.MapEndpoints(null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Fact(DisplayName = "StartupHelpers.RunAppAsync throws for null application")]
    public async Task StartupHelpersRunAppAsyncThrowsForNullApplication()
    {
        // Arrange
        // Act
        var exception = await Record.ExceptionAsync(
            () => StartupHelpers.RunAppAsync(null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }
}


