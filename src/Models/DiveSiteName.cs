namespace Models;

/// <summary>
/// Represents a validated dive-site display name.
/// </summary>
public sealed record DiveSiteName
{
    private DiveSiteName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets the dive-site display-name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated dive-site display name.
    /// </summary>
    /// <param name="value">Raw site-name value.</param>
    /// <returns>Validated dive-site name value object.</returns>
    public static DiveSiteName From(string value)
    {
        return new DiveSiteName(value);
    }
}
