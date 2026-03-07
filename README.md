# MaltaDiveWeather (MaltaDiveRadar)

MaltaDiveWeather is a production-style MVP web application that aggregates weather and marine conditions for popular Malta/Gozo diving sites and presents them on a tactical SVG radar map.

## Project Overview

- Stack: `.NET 10`, ASP.NET Core Minimal API, vanilla JavaScript modules.
- Storage: in-memory only (`ConcurrentDictionary` + `IMemoryCache`), no database.
- Runtime behavior: weather snapshots are refreshed in the background and served instantly to the UI.
- UI style: dark DEFCON-like tactical dashboard with vector map markers.

## Architecture

Solution: `src/MaltaDiveWeather.slnx`

Projects:

- `MaltaDiveWeather.Domain`
  - Core entities: `DiveSite`, `WeatherProviderSnapshot`, `WeatherSnapshot`
  - Enum: `SeaConditionStatus`
  - Domain service: `SeaConditionClassifier`
- `MaltaDiveWeather.Application`
  - Provider abstractions (`IWeatherProvider`)
  - Refresh orchestration (`WeatherAggregationService`)
  - Read/query layer for API DTOs (`WeatherQueryService`)
- `MaltaDiveWeather.Infrastructure`
  - Typed `HttpClient` providers:
    - `OpenMeteoProvider` (primary)
    - `WeatherApiProvider` (optional fallback)
    - `OpenWeatherProvider` (fallback for air/wind)
  - In-memory catalog and snapshot repository
  - Hosted refresh worker (`WeatherRefreshService`)
- `MaltaDiveWeather.Web`
  - Minimal API endpoints
  - Serilog host configuration
  - Static tactical UI (`wwwroot`)

## Weather Refresh Flow

1. App starts.
2. `WeatherRefreshService` waits startup delay and runs first refresh cycle.
3. For each active dive site:
   - providers are called by priority,
   - raw payloads are retained,
   - results are normalized to common units,
   - aggregation chooses best fields by freshness/quality/marine capability.
4. Aggregated snapshots are stored in memory and cache.
5. Service repeats every configured interval.

If providers fail for a site, previous snapshot is preserved and marked as stale.

## Provider Strategy

- Primary: Open-Meteo (forecast + marine API) for air/wind/waves/water.
- Optional fallbacks:
  - WeatherAPI (air/wind)
  - OpenWeather (air/wind)
- Demo mode:
  - `WeatherRefresh:DemoMode=true` bypasses transport-layer HTTP calls.
  - `DemoWeatherProvider` returns deterministic mock data for all sites.
- Aggregation rules:
  - prefer marine-supporting provider for marine metrics,
  - prefer fresher observation timestamp,
  - prefer higher quality score,
  - use provider priority as tie-breaker.

## API Endpoints

- `GET /api/sites`
- `GET /api/sites/{id}`
- `GET /api/sites/{id}/weather`
- `GET /api/weather/latest`
- `GET /api/health/providers` (optional health/diagnostic endpoint)

Error shape: `{ "error": "message" }`

## Configuration

Main settings in `src/MaltaDiveWeather.Web/appsettings.json`:

- `WeatherRefresh:RefreshIntervalMinutes`
- `WeatherRefresh:StartupDelaySeconds`
- `WeatherRefresh:HttpTimeoutSeconds`
- `WeatherRefresh:DemoMode`
- `WeatherRefresh:Providers:*`
  - `Enabled`
  - `Priority`
  - `ApiKey` (for WeatherAPI/OpenWeather)

When `DemoMode` is enabled, provider keys and enabled HTTP providers are ignored.

All timestamps are UTC.

## Run Locally

```powershell
dotnet restore src/MaltaDiveWeather.slnx
dotnet build src/MaltaDiveWeather.slnx -c Release
dotnet run --project src/MaltaDiveWeather.Web/MaltaDiveWeather.Web.csproj
```

Then open:

- [https://localhost:5001](https://localhost:5001) (or the URL shown in console)

## Future Improvements

- Add trend history (in-memory rolling windows or Redis).
- Add site-specific risk weighting (currents/exposure direction).
- Add provider circuit-breaking + telemetry metrics.
- Add automated integration tests with mocked provider payloads.
- Add offline static snapshot fallback for API outages.
