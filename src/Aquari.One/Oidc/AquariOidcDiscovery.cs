namespace Aquari.One.Oidc;

/// <summary>
/// Represents validated Aquari OpenID Connect discovery metadata.
/// </summary>
public sealed class AquariOidcDiscovery
{
    /// <summary>Gets the exact discovery issuer.</summary>
    public required Uri Issuer { get; init; }

    /// <summary>Gets the authorization endpoint.</summary>
    public required Uri AuthorizationEndpoint { get; init; }

    /// <summary>Gets the token endpoint.</summary>
    public required Uri TokenEndpoint { get; init; }

    /// <summary>Gets the UserInfo endpoint.</summary>
    public required Uri UserInfoEndpoint { get; init; }

    /// <summary>Gets the JSON Web Key Set endpoint.</summary>
    public required Uri JwksUri { get; init; }

    /// <summary>Gets supported OAuth grant types.</summary>
    public required IReadOnlyList<string> GrantTypesSupported { get; init; }

    /// <summary>Gets supported OAuth response types.</summary>
    public required IReadOnlyList<string> ResponseTypesSupported { get; init; }

    /// <summary>Gets supported PKCE challenge methods.</summary>
    public required IReadOnlyList<string> CodeChallengeMethodsSupported { get; init; }

    /// <summary>Gets supported ID-token signing algorithms.</summary>
    public required IReadOnlyList<string> IdTokenSigningAlgorithmsSupported { get; init; }

    /// <summary>Gets supported token-endpoint client-authentication methods.</summary>
    public required IReadOnlyList<string> TokenEndpointAuthMethodsSupported { get; init; }

    /// <summary>
    /// Gets a value indicating whether authorization responses support the issuer parameter.
    /// </summary>
    public required bool AuthorizationResponseIssuerParameterSupported { get; init; }
}
