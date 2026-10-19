namespace Aquari.One;

/// <summary>
/// Represents an Aquari not-found response.
/// </summary>
public sealed class AquariNotFoundException : AquariApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariNotFoundException"/> class.
    /// </summary>
    public AquariNotFoundException(
        int statusCode,
        string? code,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? details = null)
        : base(statusCode, code, message, details)
    {
    }
}
