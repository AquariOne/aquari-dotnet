namespace Aquari.One;

/// <summary>
/// Represents an Aquari authorization failure.
/// </summary>
public sealed class AquariAuthorizationException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariAuthorizationException"/> class.
    /// </summary>
    public AquariAuthorizationException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
    }
}
