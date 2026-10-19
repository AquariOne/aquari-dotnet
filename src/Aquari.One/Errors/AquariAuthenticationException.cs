namespace Aquari.One;

/// <summary>
/// Represents an Aquari authentication failure.
/// </summary>
public sealed class AquariAuthenticationException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariAuthenticationException"/> class.
    /// </summary>
    public AquariAuthenticationException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
    }
}
