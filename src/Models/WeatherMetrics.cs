namespace Models;

/// <summary>
/// Groups normalized weather and marine metric values.
/// </summary>
public sealed class WeatherMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherMetrics"/> class.
    /// </summary>
    /// <param name="airTemperatureC">Air-temperature value.</param>
    /// <param name="waterTemperatureC">Water-temperature value.</param>
    /// <param name="windSpeedMps">Wind-speed value.</param>
    /// <param name="windDirectionDeg">Wind-direction value.</param>
    /// <param name="waveHeightM">Wave-height value.</param>
    /// <param name="seaStateText">Sea-state text value.</param>
    public WeatherMetrics(
        AirTemperature? airTemperatureC,
        WaterTemperature? waterTemperatureC,
        WindSpeed? windSpeedMps,
        WindDirection? windDirectionDeg,
        WaveHeight? waveHeightM,
        SeaStateText? seaStateText)
    {
        AirTemperatureC = airTemperatureC;
        WaterTemperatureC = waterTemperatureC;
        WindSpeedMps = windSpeedMps;
        WindDirectionDeg = windDirectionDeg;
        WaveHeightM = waveHeightM;
        SeaStateText = seaStateText;
    }

    /// <summary>
    /// Gets air-temperature value.
    /// </summary>
    public AirTemperature? AirTemperatureC { get; }

    /// <summary>
    /// Gets water-temperature value.
    /// </summary>
    public WaterTemperature? WaterTemperatureC { get; }

    /// <summary>
    /// Gets wind-speed value.
    /// </summary>
    public WindSpeed? WindSpeedMps { get; }

    /// <summary>
    /// Gets wind-direction value.
    /// </summary>
    public WindDirection? WindDirectionDeg { get; }

    /// <summary>
    /// Gets wave-height value.
    /// </summary>
    public WaveHeight? WaveHeightM { get; }

    /// <summary>
    /// Gets sea-state text value.
    /// </summary>
    public SeaStateText? SeaStateText { get; }

    /// <summary>
    /// Gets an empty metrics object.
    /// </summary>
    public static WeatherMetrics Empty { get; } = new(
        null,
        null,
        null,
        null,
        null,
        null);

    /// <summary>
    /// Gets a value indicating whether any metric value is present.
    /// </summary>
    /// <returns>True when at least one metric value is available.</returns>
    public bool HasAnyData()
    {
        return AirTemperatureC is not null ||
            WaterTemperatureC is not null ||
            WindSpeedMps is not null ||
            WindDirectionDeg is not null ||
            WaveHeightM is not null ||
            SeaStateText is not null;
    }

    /// <summary>
    /// Gets a value indicating whether any marine metric is present.
    /// </summary>
    /// <returns>True when wave/water/sea-state data is available.</returns>
    public bool HasMarineData()
    {
        return WaterTemperatureC is not null ||
            WaveHeightM is not null ||
            SeaStateText is not null;
    }
}
