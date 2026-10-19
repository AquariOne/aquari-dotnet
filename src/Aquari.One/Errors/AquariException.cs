namespace Aquari.One;

/// <summary>
/// Represents the base exception for Aquari One SDK failures.
/// </summary>
public class AquariException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariException"/> class.
    /// </summary>
    /// <param name="message">A safe diagnostic message.</param>
    public AquariException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AquariException"/> class.
    /// </summary>
    /// <param name="message">A safe diagnostic message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public AquariException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
