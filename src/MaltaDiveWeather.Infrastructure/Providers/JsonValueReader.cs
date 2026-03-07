using System.Globalization;
using System.Text.Json;

namespace MaltaDiveWeather.Infrastructure.Providers;

internal static class JsonValueReader
{
    public static double? TryReadDouble(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return TryReadDouble(property);
    }

    public static int? TryReadInt(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return TryReadInt(property);
    }

    public static DateTimeOffset? TryReadDateTimeOffset(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind is JsonValueKind.String)
        {
            var value = property.GetString();
            if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
            {
                return parsed.ToUniversalTime();
            }
        }

        return null;
    }

    public static DateTimeOffset? TryReadUnixTime(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind is JsonValueKind.Number &&
            property.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return null;
    }

    public static double? TryReadArrayDoubleAt(
        JsonElement arrayElement,
        int index)
    {
        if (arrayElement.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        if (index < 0 || index >= arrayElement.GetArrayLength())
        {
            return null;
        }

        var valueElement = arrayElement[index];
        return TryReadDouble(valueElement);
    }

    public static DateTimeOffset? TryReadArrayDateTimeAt(
        JsonElement arrayElement,
        int index)
    {
        if (arrayElement.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        if (index < 0 || index >= arrayElement.GetArrayLength())
        {
            return null;
        }

        var valueElement = arrayElement[index];
        if (valueElement.ValueKind is not JsonValueKind.String)
        {
            return null;
        }

        var rawValue = valueElement.GetString();
        if (DateTimeOffset.TryParse(
            rawValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private static double? TryReadDouble(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Number &&
            element.TryGetDouble(out var number))
        {
            return number;
        }

        if (element.ValueKind is JsonValueKind.String &&
            double.TryParse(
                element.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        return null;
    }

    private static int? TryReadInt(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Number &&
            element.TryGetInt32(out var number))
        {
            return number;
        }

        if (element.ValueKind is JsonValueKind.String &&
            int.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        return null;
    }
}
