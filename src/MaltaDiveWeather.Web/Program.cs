using MaltaDiveWeather.Application.Abstractions;
using MaltaDiveWeather.Application.DependencyInjection;
using MaltaDiveWeather.Infrastructure.DependencyInjection;
using Serilog;

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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRouting(
    static options =>
    {
        options.LowercaseUrls = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        exceptionHandler =>
        {
            exceptionHandler.Run(
                async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(
                        new { error = "Unexpected server error." })
                        .ConfigureAwait(false);
                });
        });

    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

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
        if (id <= 0)
        {
            return Results.BadRequest(new { error = "Site ID must be positive." });
        }

        var site = queryService.GetSite(id);
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
        if (id <= 0)
        {
            return Results.BadRequest(new { error = "Site ID must be positive." });
        }

        var weather = queryService.GetSiteWeather(id);
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

app.Run();
