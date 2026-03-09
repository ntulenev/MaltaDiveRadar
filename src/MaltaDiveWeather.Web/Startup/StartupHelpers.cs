using System.Diagnostics.CodeAnalysis;

using Abstractions;

using Microsoft.AspNetCore.Diagnostics;

using Models;

using Serilog;

namespace MaltaDiveWeather.Web.Startup;

/// <summary>
/// Provides helper methods for web-host startup.
/// </summary>
internal static class StartupHelpers
{
    /// <summary>
    /// Builds and configures the <see cref="WebApplication"/> and its services.
    /// </summary>
    /// <param name="args">Application command-line arguments.</param>
    /// <returns>Configured application instance.</returns>
    public static WebApplication CreateApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog(
            static (context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

        builder.Services.AddProblemDetails();
        builder.Services.AddLogic();
        builder.Services.AddStorage(builder.Configuration);
        builder.Services.AddRouting(
            static options =>
            {
                options.LowercaseUrls = true;
            });

        return builder.Build();
    }

    /// <summary>
    /// Configures host middleware.
    /// </summary>
    /// <param name="app">Configured application instance.</param>
    public static void ConfigureMiddleware(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(HandleException);
            app.UseHsts();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    /// <summary>
    /// Maps HTTP endpoints.
    /// </summary>
    /// <param name="app">Configured application instance.</param>
    public static void MapEndpoints(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var api = app.MapGroup("/api");

        api.MapGet(
            "/sites",
            (IWeatherQueryService queryService) =>
            {
                var sites = queryService.GetSites();
                return TypedResults.Ok(sites);
            });

        api.MapGet(
            "/sites/{id:int}",
            (int id, IWeatherQueryService queryService) =>
            {
                if (!TryCreateSiteId(id, out var siteId, out var error))
                {
                    return Results.BadRequest(new { error });
                }

                var site = queryService.GetSite(siteId);
                if (site is null)
                {
                    return Results.NotFound(new { error = $"Site '{id}' was not found." });
                }

                return Results.Ok(site);
            });

        api.MapGet(
            "/sites/{id:int}/weather",
            (int id, IWeatherQueryService queryService) =>
            {
                if (!TryCreateSiteId(id, out var siteId, out var error))
                {
                    return Results.BadRequest(new { error });
                }

                var weather = queryService.GetSiteWeather(siteId);
                if (weather is null)
                {
                    return Results.NotFound(
                        new { error = $"Weather snapshot for site '{id}' was not found." });
                }

                return Results.Ok(weather);
            });

        api.MapGet(
            "/weather/latest",
            (IWeatherQueryService queryService) =>
            {
                var latestWeather = queryService.GetLatestWeather();
                return TypedResults.Ok(latestWeather);
            });

        api.MapGet(
            "/health/providers",
            (IWeatherQueryService queryService) =>
            {
                var latestWeather = queryService.GetLatestWeather();

                var providerHealth = latestWeather.Snapshots
                    .SelectMany(static snapshot => snapshot.ProviderSnapshots)
                    .GroupBy(static provider => provider.ProviderName)
                    .Select(group => new
                    {
                        provider = group.Key,
                        callCount = group.Count(),
                        successRate = Math.Round(
                            group.Count(static provider => provider.IsSuccess) /
                            (double)group.Count(),
                            2),
                        lastRetrievedUtc = group
                            .Max(static provider => provider.RetrievedAtUtc),
                        lastError = group
                            .Where(static provider => !provider.IsSuccess)
                            .OrderByDescending(static provider => provider.RetrievedAtUtc)
                            .Select(static provider => provider.Error)
                            .FirstOrDefault(),
                    })
                    .OrderBy(static provider => provider.provider)
                    .ToArray();

                return TypedResults.Ok(
                    new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        providers = providerHealth,
                    });
            });

        app.MapFallbackToFile("index.html");
    }

    /// <summary>
    /// Runs the configured host.
    /// </summary>
    /// <param name="app">Configured application instance.</param>
    /// <returns>Task representing host run.</returns>
    public static async Task RunAppAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await app.RunAsync().ConfigureAwait(false);
    }

    private static bool TryCreateSiteId(
        int rawId,
        [NotNullWhen(true)] out DiveSiteId? siteId,
        [NotNullWhen(false)] out string? error)
    {
        siteId = null;

        if (rawId <= 0)
        {
            error = "Site ID must be positive.";
            return false;
        }

        siteId = DiveSiteId.FromInt(rawId);
        error = null;
        return true;
    }

    private static void HandleException(IApplicationBuilder errorApp)
    {
        ArgumentNullException.ThrowIfNull(errorApp);

        errorApp.Run(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(
                    new { error = "Unexpected server error." })
                    .ConfigureAwait(false);
            });
    }
}
