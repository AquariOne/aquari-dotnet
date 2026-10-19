namespace Aquari.One.Passport;

/// <summary>Identifies the normalized family of a Passport signal.</summary>
public enum AquariPassportSignalFamily
{
    /// <summary>Badge signal.</summary>
    Badge,

    /// <summary>Stamp signal.</summary>
    Stamp,

    /// <summary>Credential signal.</summary>
    Credential,

    /// <summary>Entitlement signal.</summary>
    Entitlement,

    /// <summary>Formal role signal.</summary>
    Role,

    /// <summary>General membership signal.</summary>
    Membership,

    /// <summary>Future or otherwise unknown signal family.</summary>
    Unknown,
}

/// <summary>Represents connected Passport profile presentation.</summary>
public sealed class AquariPassportProfile
{
    /// <summary>Gets the display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the handle.</summary>
    public required string Handle { get; init; }

    /// <summary>Gets the avatar URI, when present.</summary>
    public Uri? AvatarUri { get; init; }

    /// <summary>Gets the public Passport URI.</summary>
    public required Uri PublicUri { get; init; }

    /// <summary>Gets whether the profile is verified.</summary>
    public required bool Verified { get; init; }

    /// <summary>Gets the profile visibility.</summary>
    public required string Visibility { get; init; }
}

/// <summary>Represents the connected Passport signal summary.</summary>
public sealed class AquariPassportSignalSummary
{
    /// <summary>Gets the total visible signal count.</summary>
    public required int Total { get; init; }
}

/// <summary>Represents the authenticated user's Passport.</summary>
public sealed class AquariPassport
{
    /// <summary>Gets the Passport identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the Passport number.</summary>
    public required string PassportNumber { get; init; }

    /// <summary>Gets the Passport subject.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the profile projection.</summary>
    public required AquariPassportProfile Profile { get; init; }

    /// <summary>Gets the signal summary.</summary>
    public required AquariPassportSignalSummary Signals { get; init; }
}

/// <summary>Represents a signal issuer.</summary>
public sealed class AquariPassportSignalIssuer
{
    /// <summary>Gets the issuer type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the issuer identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the issuer name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the issuer logo URI, when present.</summary>
    public Uri? LogoUri { get; init; }
}

/// <summary>Represents a signal scope.</summary>
public sealed class AquariPassportSignalScope
{
    /// <summary>Gets the scope type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the scope identifier.</summary>
    public required string Id { get; init; }
}

/// <summary>Represents an authenticated Passport signal.</summary>
public sealed class AquariPassportSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the normalized signal family.</summary>
    public required AquariPassportSignalFamily Family { get; init; }

    /// <summary>Gets the raw source type.</summary>
    public required string SourceType { get; init; }

    /// <summary>Gets the definition identifier, when present.</summary>
    public string? DefinitionId { get; init; }

    /// <summary>Gets the presentation type.</summary>
    public required string PresentationType { get; init; }

    /// <summary>Gets the signal name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the signal description.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the signal image URI, when present.</summary>
    public Uri? ImageUri { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariPassportSignalIssuer Issuer { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the expiration time, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets the visibility.</summary>
    public required string Visibility { get; init; }

    /// <summary>Gets whether Aquari considers the signal verified.</summary>
    public required bool Verified { get; init; }

    /// <summary>Gets the occurrence time, when present.</summary>
    public DateTimeOffset? OccurredAt { get; init; }

    /// <summary>Gets the signal scope, when present.</summary>
    public AquariPassportSignalScope? Scope { get; init; }
}

/// <summary>Represents one page of Passport signals.</summary>
public sealed class AquariPassportSignalPage
{
    /// <summary>Gets the page items.</summary>
    public required IReadOnlyList<AquariPassportSignal> Items { get; init; }

    /// <summary>Gets the next cursor, when another page exists.</summary>
    public string? NextCursor { get; init; }

    /// <summary>Gets whether another page exists.</summary>
    public required bool HasMore { get; init; }
}

/// <summary>Configures Passport signal list filtering.</summary>
public sealed class AquariPassportSignalListOptions
{
    /// <summary>Gets or sets the presentation type filter.</summary>
    public string? Type { get; init; }

    /// <summary>Gets or sets the issuer filter.</summary>
    public string? IssuerId { get; init; }

    /// <summary>Gets or sets the definition filter.</summary>
    public string? DefinitionId { get; init; }

    /// <summary>Gets or sets the page size from 1 through 100.</summary>
    public int? Limit { get; init; }

    /// <summary>Gets or sets the continuation cursor.</summary>
    public string? Cursor { get; init; }
}

/// <summary>Represents a publicly visible Passport signal.</summary>
public sealed class AquariPublicPassportSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the presentation type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the image URI, when present.</summary>
    public Uri? ImageUri { get; init; }

    /// <summary>Gets the issuer display name.</summary>
    public required string Issuer { get; init; }

    /// <summary>Gets the issuer logo URI, when present.</summary>
    public Uri? IssuerLogoUri { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the expiration time, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets whether the signal is verified.</summary>
    public required bool Verified { get; init; }

    /// <summary>Gets the occurrence time, when present.</summary>
    public DateTimeOffset? OccurredAt { get; init; }

    /// <summary>Gets the public scope, when present.</summary>
    public AquariPassportSignalScope? Scope { get; init; }
}

/// <summary>Represents a public Passport.</summary>
public sealed class AquariPublicPassport
{
    /// <summary>Gets the display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the handle.</summary>
    public required string Handle { get; init; }

    /// <summary>Gets the avatar URI, when present.</summary>
    public Uri? AvatarUri { get; init; }

    /// <summary>Gets the public Passport URI.</summary>
    public required Uri PublicUri { get; init; }

    /// <summary>Gets whether the Passport is verified.</summary>
    public required bool Verified { get; init; }

    /// <summary>Gets public signals.</summary>
    public required IReadOnlyList<AquariPublicPassportSignal> Signals { get; init; }
}
