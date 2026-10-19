namespace Aquari.One.Auth;

/// <summary>Represents identity claims validated from Aquari.</summary>
public sealed class AquariIdentity
{
    /// <summary>Gets the stable Aquari subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the preferred handle, when supplied.</summary>
    public string? Handle { get; init; }

    /// <summary>Gets the canonical handle, when supplied.</summary>
    public string? CanonicalHandle { get; init; }

    /// <summary>Gets the verified handle, when supplied.</summary>
    public string? VerifiedHandle { get; init; }

    /// <summary>Gets the display name, when supplied.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the profile picture URI, when supplied.</summary>
    public Uri? PictureUri { get; init; }

    /// <summary>Gets the email address, when supplied.</summary>
    public string? Email { get; init; }

    /// <summary>Gets email verification state, when supplied.</summary>
    public bool? EmailVerified { get; init; }
}
