namespace Aquari.One;

/// <summary>
/// Represents an invalid Aquari SDK configuration.
/// </summary>
public sealed class AquariConfigurationException : AquariException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariConfigurationException"/> class.
    /// </summary>
    /// <param name="message">A safe diagnostic message.</param>
    public AquariConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AquariConfigurationException"/> class.
    /// </summary>
    /// <param name="message">A safe diagnostic message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public AquariConfigurationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
