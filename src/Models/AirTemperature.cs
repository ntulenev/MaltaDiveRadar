namespace Models;

/// <summary>
/// Represents a validated air temperature in Celsius.
/// </summary>
public sealed record AirTemperature
{
    private AirTemperature(double celsius)
    {
        if (double.IsNaN(celsius) || double.IsInfinity(celsius))
        {
            throw new ArgumentOutOfRangeException(
                nameof(celsius),
                "Air temperature must be a finite number.");
        }

        if (celsius is < MIN_CELSIUS or > MAX_CELSIUS)
        {
            throw new ArgumentOutOfRangeException(
                nameof(celsius),
                "Air temperature must be in range [-90, 60] Celsius.");
        }

        Celsius = celsius;
    }

    /// <summary>
    /// Gets air temperature value in Celsius.
    /// </summary>
    public double Celsius { get; }

    /// <summary>
    /// Creates a validated air-temperature value from Celsius.
    /// </summary>
    /// <param name="celsius">Air temperature in Celsius.</param>
    /// <returns>Validated air-temperature value object.</returns>
    public static AirTemperature FromCelsius(double celsius)
    {
        return new AirTemperature(celsius);
    }

    private const double MIN_CELSIUS = -90D;
    private const double MAX_CELSIUS = 60D;
}
