namespace Models;

/// <summary>
/// Represents a validated weather-provider display name.
/// </summary>
public sealed record ProviderName
{
    private ProviderName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets the provider display-name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated provider name.
    /// </summary>
    /// <param name="value">Raw provider-name value.</param>
    /// <returns>Validated provider-name value object.</returns>
    public static ProviderName From(string value)
    {
        return new ProviderName(value);
    }
}
