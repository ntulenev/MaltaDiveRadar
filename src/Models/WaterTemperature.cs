namespace Models;

/// <summary>
/// Represents a validated sea-water temperature in Celsius.
/// </summary>
public sealed record WaterTemperature
{
    private WaterTemperature(double celsius)
    {
        if (double.IsNaN(celsius) || double.IsInfinity(celsius))
        {
            throw new ArgumentOutOfRangeException(
                nameof(celsius),
                "Water temperature must be a finite number.");
        }

        if (celsius is < MIN_CELSIUS or > MAX_CELSIUS)
        {
            throw new ArgumentOutOfRangeException(
                nameof(celsius),
                "Water temperature must be in range [-5, 50] Celsius.");
        }

        Celsius = celsius;
    }

    /// <summary>
    /// Gets water temperature value in Celsius.
    /// </summary>
    public double Celsius { get; }

    /// <summary>
    /// Creates a validated water-temperature value from Celsius.
    /// </summary>
    /// <param name="celsius">Water temperature in Celsius.</param>
    /// <returns>Validated water-temperature value object.</returns>
    public static WaterTemperature FromCelsius(double celsius)
    {
        return new WaterTemperature(celsius);
    }

    private const double MIN_CELSIUS = -5D;
    private const double MAX_CELSIUS = 50D;
}
