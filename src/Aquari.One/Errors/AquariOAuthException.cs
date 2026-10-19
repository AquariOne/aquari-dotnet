namespace Aquari.One;

/// <summary>
/// Represents an OAuth or OpenID Connect protocol error returned by Aquari.
/// </summary>
public sealed class AquariOAuthException : AquariException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AquariOAuthException"/> class.
    /// </summary>
    /// <param name="code">The OAuth/OIDC error code.</param>
    /// <param name="message">A safe diagnostic message.</param>
    /// <param name="errorDescription">The protocol error description, when supplied.</param>
    /// <param name="errorUri">The protocol error reference URI, when supplied.</param>
    public AquariOAuthException(
        string code,
        string message,
        string? errorDescription = null,
        Uri? errorUri = null)
        : base(message)
    {
        Code = code;
        ErrorDescription = errorDescription;
        ErrorUri = errorUri;
    }

    /// <summary>Gets the OAuth/OIDC error code.</summary>
    public string Code { get; }

    /// <summary>Gets the protocol error description, when supplied.</summary>
    public string? ErrorDescription { get; }

    /// <summary>Gets the protocol error reference URI, when supplied.</summary>
    public Uri? ErrorUri { get; }
}
