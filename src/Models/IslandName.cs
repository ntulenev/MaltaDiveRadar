namespace Models;

/// <summary>
/// Represents a validated Malta archipelago island name.
/// </summary>
public sealed record IslandName
{
    private IslandName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value.Trim().ToUpperInvariant() switch
        {
            "MALTA" => "Malta",
            "GOZO" => "Gozo",
            "COMINO" => "Comino",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                "Island must be one of: Malta, Gozo, Comino."),
        };
    }

    /// <summary>
    /// Gets the normalized island-name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated island-name value object.
    /// </summary>
    /// <param name="value">Raw island-name value.</param>
    /// <returns>Validated island-name value object.</returns>
    public static IslandName From(string value)
    {
        return new IslandName(value);
    }
}
