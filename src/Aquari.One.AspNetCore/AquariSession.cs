namespace Aquari.One.AspNetCore;

/// <summary>
/// Represents a server-side Aquari application session.
/// </summary>
public sealed class AquariSession
{
    /// <summary>Gets the authenticated user.</summary>
    public required AquariSessionUser User { get; init; }

    /// <summary>Gets the OAuth scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the access-token expiration time.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Represents user information exposed by an Aquari application session.
/// </summary>
public sealed class AquariSessionUser
{
    /// <summary>Gets the stable Aquari subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the handle, when supplied.</summary>
    public string? Handle { get; init; }

    /// <summary>Gets the display name, when supplied.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the email address, when supplied.</summary>
    public string? Email { get; init; }

    /// <summary>Gets email verification state, when supplied.</summary>
    public bool? EmailVerified { get; init; }

    /// <summary>Gets the profile picture URI, when supplied.</summary>
    public Uri? PictureUri { get; init; }
}
