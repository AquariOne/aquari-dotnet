namespace Aquari.One;

/// <summary>
/// Represents an Aquari conflict response.
/// </summary>
public sealed class AquariConflictException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariConflictException"/> class.
    /// </summary>
    public AquariConflictException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
    }
}
