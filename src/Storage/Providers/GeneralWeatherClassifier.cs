using Models;

namespace Storage.Providers;

internal static class GeneralWeatherClassifier
{
    public static GeneralWeatherKind? FromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim();

        if (normalized.Contains("thunder", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Thunderstorm;
        }

        if (normalized.Contains("snow", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("sleet", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("blizzard", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Snow;
        }

        if (normalized.Contains("drizzle", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Drizzle;
        }

        if (normalized.Contains("rain", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("shower", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Rain;
        }

        if (normalized.Contains("fog", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("mist", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("haze", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Fog;
        }

        if (normalized.Contains("partly", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("few clouds", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("scattered clouds", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("broken clouds", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.PartlyCloudy;
        }

        if (normalized.Contains("cloud", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("overcast", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Cloudy;
        }

        if (normalized.Contains("sun", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("clear", StringComparison.OrdinalIgnoreCase))
        {
            return GeneralWeatherKind.Sunny;
        }

        return GeneralWeatherKind.Mixed;
    }
}
