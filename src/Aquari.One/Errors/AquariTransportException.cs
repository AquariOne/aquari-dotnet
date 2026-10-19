namespace Aquari.One;

/// <summary>
/// Represents a failure before Aquari returned a valid application response.
/// </summary>
public class AquariTransportException : AquariException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariTransportException"/> class.
    /// </summary>
    /// <param name="message">A safe diagnostic message.</param>
    /// <param name="isCancelled">Whether the caller cancelled the operation.</param>
    /// <param name="innerException">The underlying transport exception, when available.</param>
    public AquariTransportException(
        string message,
        bool isCancelled = false,
        Exception? innerException = null)
        : base(
            message,
            innerException)
    {
        IsCancelled = isCancelled;
    }

    /// <summary>
    /// Gets a value indicating whether the caller cancelled the operation.
    /// </summary>
    public bool IsCancelled { get; }
}
