namespace Aquari.One.Auth;

/// <summary>
/// Binds an Aquari authorization request to its callback.
/// </summary>
public sealed class AquariAuthorizationTransaction
{
    /// <summary>Gets the transaction schema version.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets the exact normalized issuer URI.</summary>
    public required Uri Issuer { get; init; }

    /// <summary>Gets the OAuth client identifier.</summary>
    public required string ClientId { get; init; }

    /// <summary>Gets the exact redirect URI.</summary>
    public required Uri RedirectUri { get; init; }

    /// <summary>Gets the ordered requested scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the PKCE code verifier.</summary>
    public required string CodeVerifier { get; init; }

    /// <summary>Gets the OAuth state value.</summary>
    public required string State { get; init; }

    /// <summary>Gets the OpenID Connect nonce.</summary>
    public required string Nonce { get; init; }

    /// <summary>Gets the UTC transaction creation time.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
