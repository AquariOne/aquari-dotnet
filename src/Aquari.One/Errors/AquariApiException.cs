namespace Aquari.One;

/// <summary>
/// Represents a non-success response returned by the Aquari API.
/// </summary>
public class AquariApiException : AquariException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="code">The Aquari error code, when supplied.</param>
    /// <param name="message">A safe diagnostic message.</param>
    /// <param name="details">Structured Aquari error details, when supplied.</param>
    public AquariApiException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }

    /// <summary>
    /// Gets the HTTP response status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the Aquari error code, when supplied.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets structured Aquari error details, when supplied.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Details { get; }
}
