namespace Aquari.One.Auth;

/// <summary>
/// Represents a prepared Aquari authorization request.
/// </summary>
public sealed class AquariBeginAuthorizationResult
{
    /// <summary>Gets the authorization URI to open in the user agent.</summary>
    public required Uri AuthorizationUri { get; init; }

    /// <summary>Gets the transaction state retained until callback completion.</summary>
    public required AquariAuthorizationTransaction Transaction { get; init; }
}
