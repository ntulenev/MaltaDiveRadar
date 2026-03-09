using MaltaDiveWeather.Web.Startup;

using var app = StartupHelpers.CreateApplication(args);

StartupHelpers.ConfigureMiddleware(app);
StartupHelpers.MapEndpoints(app);

await StartupHelpers.RunAppAsync(app).ConfigureAwait(false);
