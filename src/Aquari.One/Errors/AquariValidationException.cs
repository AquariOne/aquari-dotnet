namespace Aquari.One;

/// <summary>
/// Represents an Aquari validation failure.
/// </summary>
public sealed class AquariValidationException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariValidationException"/> class.
    /// </summary>
    public AquariValidationException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
    }

    /// <summary>
    /// Gets field-oriented validation details.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Fields =>
        Details ?? EmptyDetails;

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyDetails { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
}
