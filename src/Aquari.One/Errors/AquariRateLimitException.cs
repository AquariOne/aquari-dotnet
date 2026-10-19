namespace Aquari.One;

/// <summary>
/// Represents an Aquari rate-limit response.
/// </summary>
public sealed class AquariRateLimitException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariRateLimitException"/> class.
    /// </summary>
    public AquariRateLimitException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details,
        TimeSpan? retryAfter)
        : base(statusCode, code, message, details)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the server-advertised retry delay, when supplied.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
