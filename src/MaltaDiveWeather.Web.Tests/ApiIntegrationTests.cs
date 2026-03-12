using Abstractions;

using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Storage.Providers;

namespace MaltaDiveWeather.Web.Tests;

[Trait("Category", "Integration")]
public sealed class ApiIntegrationTests
{
    [Fact(DisplayName = "GET /api/sites in demo mode returns configured sites")]
    public async Task GetSitesWhenDemoModeEnabledReturnsConfiguredSites()
    {
        // Arrange
        using var factory = new DemoModeApiFactory();
        using var client = CreateHttpsClient(factory);

        // Act
        var sites = await client.GetFromJsonAsync<IReadOnlyList<DiveSiteResponse>>(
            "/api/sites");

        // Assert
        sites.Should().NotBeNullOrEmpty();
        sites.Should().OnlyContain(static site => site.Id > 0);
        sites.Should().OnlyContain(
            static site => !string.IsNullOrWhiteSpace(site.Name));
        sites.Should().Contain(static site => site.Name == "Cirkewwa");
    }

    [Fact(DisplayName = "GET /api/weather/latest in demo mode returns demo snapshots")]
    public async Task GetLatestWeatherWhenDemoModeEnabledReturnsDemoSnapshots()
    {
        // Arrange
        using var factory = new DemoModeApiFactory();
        using var client = CreateHttpsClient(factory);

        // Act
        var latestWeather = await WaitForLatestWeatherAsync(client);

        // Assert
        latestWeather.LastRefreshUtc.Should().NotBeNull();
        latestWeather.Snapshots.Should().NotBeNullOrEmpty();
        latestWeather.Snapshots.Should().OnlyContain(
            static snapshot => snapshot.SourceProvider == DemoProviderName);
        latestWeather.Snapshots.Should().OnlyContain(
            static snapshot =>
                snapshot.ProviderSnapshots.Count == 1 &&
                snapshot.ProviderSnapshots[0].ProviderName == DemoProviderName &&
                snapshot.ProviderSnapshots[0].IsSuccess);
    }

    [Fact(DisplayName = "GET /api/sites/{id}/weather in demo mode returns site snapshot")]
    public async Task GetSiteWeatherWhenDemoModeEnabledReturnsSnapshotForKnownSite()
    {
        // Arrange
        using var factory = new DemoModeApiFactory();
        using var client = CreateHttpsClient(factory);
        var latestWeather = await WaitForLatestWeatherAsync(client);
        var siteId = latestWeather.Snapshots[0].DiveSiteId;

        // Act
        var snapshot = await client.GetFromJsonAsync<WeatherSnapshotResponse>(
            $"/api/sites/{siteId}/weather");

        // Assert
        snapshot.Should().NotBeNull();
        snapshot!.DiveSiteId.Should().Be(siteId);
        snapshot.SourceProvider.Should().Be(DemoProviderName);
        snapshot.ProviderSnapshots.Should().ContainSingle();
        snapshot.ProviderSnapshots[0].ProviderName.Should().Be(DemoProviderName);
        snapshot.ProviderSnapshots[0].IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "GET /api/health/providers in demo mode reports demo provider")]
    public async Task GetProviderHealthWhenDemoModeEnabledReportsDemoProviderStats()
    {
        // Arrange
        using var factory = new DemoModeApiFactory();
        using var client = CreateHttpsClient(factory);
        _ = await WaitForLatestWeatherAsync(client);

        // Act
        var health = await client.GetFromJsonAsync<ProviderHealthResponse>(
            "/api/health/providers");

        // Assert
        health.Should().NotBeNull();
        health!.Providers.Should().ContainSingle();
        var provider = health.Providers[0];
        provider.Provider.Should().Be(DemoProviderName);
        provider.CallCount.Should().BeGreaterThan(0);
        provider.SuccessRate.Should().Be(1D);
        provider.LastError.Should().BeNull();
    }

    private static async Task<LatestWeatherResponse> WaitForLatestWeatherAsync(
        HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            while (true)
            {
                var latestWeather = await client.GetFromJsonAsync<LatestWeatherResponse>(
                    "/api/weather/latest",
                    timeoutCts.Token);

                if (latestWeather is not null &&
                    latestWeather.LastRefreshUtc is not null &&
                    latestWeather.Snapshots.Count > 0)
                {
                    return latestWeather;
                }

                await Task.Delay(100, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException exception)
            when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Timed out waiting for demo-mode weather snapshots.",
                exception);
        }
    }

    private static HttpClient CreateHttpsClient(DemoModeApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });
    }

    private const string DemoProviderName = "Demo-Simulator";

    private sealed class DemoModeApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(
                static (hostingContext, configurationBuilder) =>
                {
                    configurationBuilder.Sources.Clear();

                    configurationBuilder
                        .AddJsonFile(
                            "appsettings.json",
                            optional: false,
                            reloadOnChange: false)
                        .AddJsonFile(
                            $"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}" +
                            ".json",
                            optional: true,
                            reloadOnChange: false)
                        .AddEnvironmentVariables();

                    var overrides = new Dictionary<string, string?>
                    {
                        ["WeatherRefresh:DemoMode"] = "true",
                        ["WeatherRefresh:StartupDelaySeconds"] = "0",
                        ["WeatherRefresh:RefreshIntervalMinutes"] = "120",
                    };

                    configurationBuilder.AddInMemoryCollection(overrides);
                });

            builder.ConfigureServices(
                static services =>
                {
                    services.RemoveAll<IWeatherProvider>();
                    services.RemoveAll<OpenMeteoProvider>();
                    services.RemoveAll<WeatherApiProvider>();
                    services.RemoveAll<OpenWeatherProvider>();
                    services.RemoveAll<DemoWeatherProvider>();

                    services.AddSingleton<DemoWeatherProvider>();
                    services.AddTransient<IWeatherProvider>(
                        static provider =>
                            provider.GetRequiredService<DemoWeatherProvider>());
                });
        }
    }
}

public sealed class DiveSiteResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class LatestWeatherResponse
{
    public DateTimeOffset? LastRefreshUtc { get; init; }
    public IReadOnlyList<WeatherSnapshotResponse> Snapshots { get; init; } = [];
}

public sealed class WeatherSnapshotResponse
{
    public int DiveSiteId { get; init; }
    public string SourceProvider { get; init; } = string.Empty;
    public IReadOnlyList<ProviderSnapshotResponse> ProviderSnapshots { get; init; } = [];
}

public sealed class ProviderSnapshotResponse
{
    public string ProviderName { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
}

public sealed class ProviderHealthResponse
{
    public IReadOnlyList<ProviderHealthItemResponse> Providers { get; init; } = [];
}

public sealed class ProviderHealthItemResponse
{
    public string Provider { get; init; } = string.Empty;
    public int CallCount { get; init; }
    public double SuccessRate { get; init; }
    public string? LastError { get; init; }
}
