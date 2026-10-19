namespace Aquari.One.Auth;

/// <summary>Represents a validated Aquari OAuth token set.</summary>
public sealed class AquariTokenSet
{
    /// <summary>Gets the stable subject bound to the token set.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the OAuth token type.</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Gets the access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Gets the validated ID token.</summary>
    public required string IdToken { get; init; }

    /// <summary>Gets the rotating refresh token, when granted.</summary>
    public string? RefreshToken { get; init; }

    /// <summary>Gets the reported access-token lifetime.</summary>
    public required TimeSpan ExpiresIn { get; init; }

    /// <summary>Gets the calculated UTC expiration.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the token scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }
}
