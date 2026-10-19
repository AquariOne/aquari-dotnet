namespace Aquari.One;

/// <summary>
/// Represents an Aquari request that exceeded its configured timeout.
/// </summary>
public sealed class AquariTimeoutException : AquariTransportException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariTimeoutException"/> class.
    /// </summary>
    /// <param name="timeout">The configured timeout.</param>
    /// <param name="innerException">The underlying cancellation exception, when available.</param>
    public AquariTimeoutException(
        TimeSpan timeout,
        Exception? innerException = null)
        : base(
            $"Aquari request timed out after {timeout.TotalMilliseconds:0}ms.",
            false,
            innerException)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the configured request timeout.
    /// </summary>
    public TimeSpan Timeout { get; }
}
