namespace Models;

/// <summary>
/// Groups provider identity and capability metadata.
/// </summary>
public sealed class WeatherProviderMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherProviderMetadata"/> class.
    /// </summary>
    /// <param name="providerName">Provider display name.</param>
    /// <param name="priority">Provider priority.</param>
    /// <param name="supportsMarineData">Whether provider supports marine data.</param>
    public WeatherProviderMetadata(
        ProviderName providerName,
        ProviderPriority priority,
        bool supportsMarineData)
    {
        ArgumentNullException.ThrowIfNull(providerName);
        ArgumentNullException.ThrowIfNull(priority);

        ProviderName = providerName;
        Priority = priority;
        SupportsMarineData = supportsMarineData;
    }

    /// <summary>
    /// Gets provider display name.
    /// </summary>
    public ProviderName ProviderName { get; }

    /// <summary>
    /// Gets provider priority.
    /// </summary>
    public ProviderPriority Priority { get; }

    /// <summary>
    /// Gets a value indicating whether provider supports marine data.
    /// </summary>
    public bool SupportsMarineData { get; }

    /// <summary>
    /// Gets a value indicating whether this provider has higher preference.
    /// </summary>
    /// <param name="other">Other provider metadata.</param>
    /// <returns>True when this provider has higher preference.</returns>
    public bool IsHigherPreferenceThan(WeatherProviderMetadata other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Priority.IsHigherPreferenceThan(other.Priority);
    }
}
