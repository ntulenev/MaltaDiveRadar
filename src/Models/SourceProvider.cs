namespace Models;

/// <summary>
/// Represents provider-source label used in aggregated snapshots.
/// </summary>
public sealed record SourceProvider
{
    private SourceProvider(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets provider-source label value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated source-provider label from raw text.
    /// </summary>
    /// <param name="value">Raw label value.</param>
    /// <returns>Validated source-provider label value object.</returns>
    public static SourceProvider FromLabel(string value)
    {
        return new SourceProvider(value);
    }

    /// <summary>
    /// Creates source-provider label from a single provider name.
    /// </summary>
    /// <param name="providerName">Provider name.</param>
    /// <returns>Validated source-provider value object.</returns>
    public static SourceProvider FromProvider(ProviderName providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);
        return new SourceProvider(providerName.Value);
    }

    /// <summary>
    /// Composes a source-provider label from distinct provider names.
    /// </summary>
    /// <param name="providerNames">Provider names.</param>
    /// <returns>Validated source-provider value object.</returns>
    public static SourceProvider Compose(IEnumerable<ProviderName> providerNames)
    {
        ArgumentNullException.ThrowIfNull(providerNames);

        var labels = providerNames
            .Select(static provider => provider.Value)
            .Where(static label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (labels.Length == 0)
        {
            throw new ArgumentException(
                "At least one provider name must be supplied.",
                nameof(providerNames));
        }

        return new SourceProvider(string.Join(" + ", labels));
    }

    /// <summary>
    /// Gets a value indicating whether source includes multiple providers.
    /// </summary>
    /// <returns>True when source combines multiple provider names.</returns>
    public bool IsComposite()
    {
        return Value.Contains(" + ", StringComparison.Ordinal);
    }
}
