namespace Aquari.One.Auth;

/// <summary>Represents a completed Aquari Authorization Code flow.</summary>
public sealed class AquariCompleteAuthorizationResult
{
    /// <summary>Gets the validated token set.</summary>
    public required AquariTokenSet Tokens { get; init; }

    /// <summary>Gets the validated identity.</summary>
    public required AquariIdentity Identity { get; init; }
}
