namespace Aquari.One.Server.Webhooks;

/// <summary>Data for application.connection.created.</summary>
public sealed class AquariApplicationConnectionCreatedData
{
    /// <summary>Gets the connected user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the OAuth client identifier.</summary>
    public required string OAuthClientId { get; init; }

    /// <summary>Gets the granted scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the connection time.</summary>
    public required DateTimeOffset ConnectedAt { get; init; }
}

/// <summary>Data for application.connection.revoked.</summary>
public sealed class AquariApplicationConnectionRevokedData
{
    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets affected OAuth client identifiers.</summary>
    public required IReadOnlyList<string> OAuthClientIds { get; init; }

    /// <summary>Gets the revoked scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the revocation time.</summary>
    public required DateTimeOffset RevokedAt { get; init; }
}

/// <summary>Data for application.updated.</summary>
public sealed class AquariApplicationUpdatedData
{
    /// <summary>Gets the application identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the owning organization identifier.</summary>
    public required string OrganizationId { get; init; }

    /// <summary>Gets the application name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the application slug.</summary>
    public required string Slug { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the optional logo URL.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Gets the optional homepage URL.</summary>
    public string? HomepageUrl { get; init; }

    /// <summary>Gets the optional privacy-policy URL.</summary>
    public string? PrivacyPolicyUrl { get; init; }

    /// <summary>Gets the optional terms-of-service URL.</summary>
    public string? TermsOfServiceUrl { get; init; }

    /// <summary>Gets the optional support URL.</summary>
    public string? SupportUrl { get; init; }

    /// <summary>Gets the optional support email.</summary>
    public string? SupportEmail { get; init; }

    /// <summary>Gets the application status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the optional update time.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>Data for organization.member.added.</summary>
public sealed class AquariOrganizationMemberAddedData
{
    /// <summary>Gets the member identifier.</summary>
    public required string MemberId { get; init; }

    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the organization role.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the join time.</summary>
    public required DateTimeOffset JoinedAt { get; init; }
}

/// <summary>Data for organization.member.updated.</summary>
public sealed class AquariOrganizationMemberUpdatedData
{
    /// <summary>Gets the member identifier.</summary>
    public required string MemberId { get; init; }

    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the organization role.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the optional application identifier.</summary>
    public string? ApplicationId { get; init; }

    /// <summary>Gets optional application permissions.</summary>
    public IReadOnlyList<string>? ApplicationPermissions { get; init; }

    /// <summary>Gets the update time.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Data for organization.member.removed.</summary>
public sealed class AquariOrganizationMemberRemovedData
{
    /// <summary>Gets the member identifier.</summary>
    public required string MemberId { get; init; }

    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the previous organization role.</summary>
    public required string PreviousRole { get; init; }

    /// <summary>Gets the removal time.</summary>
    public required DateTimeOffset RemovedAt { get; init; }
}

/// <summary>Data for organization.ownership.transferred.</summary>
public sealed class AquariOrganizationOwnershipTransferredData
{
    /// <summary>Gets the previous owner member identifier.</summary>
    public required string PreviousOwnerMemberId { get; init; }

    /// <summary>Gets the previous owner user identifier.</summary>
    public required string PreviousOwnerUserId { get; init; }

    /// <summary>Gets the new owner member identifier.</summary>
    public required string NewOwnerMemberId { get; init; }

    /// <summary>Gets the new owner user identifier.</summary>
    public required string NewOwnerUserId { get; init; }

    /// <summary>Gets the transfer time.</summary>
    public required DateTimeOffset TransferredAt { get; init; }
}

/// <summary>Data for organization.domain.verified.</summary>
public sealed class AquariOrganizationDomainVerifiedData
{
    /// <summary>Gets the domain claim identifier.</summary>
    public required string DomainClaimId { get; init; }

    /// <summary>Gets the verified domain.</summary>
    public required string Domain { get; init; }

    /// <summary>Gets the verification time.</summary>
    public required DateTimeOffset VerifiedAt { get; init; }

    /// <summary>Gets the verification expiration time.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>Data for organization.domain.revoked.</summary>
public sealed class AquariOrganizationDomainRevokedData
{
    /// <summary>Gets the domain claim identifier.</summary>
    public required string DomainClaimId { get; init; }

    /// <summary>Gets the revoked domain.</summary>
    public required string Domain { get; init; }

    /// <summary>Gets the revocation time.</summary>
    public required DateTimeOffset RevokedAt { get; init; }
}

/// <summary>Data for passport.visibility.updated.</summary>
public sealed class AquariPassportVisibilityUpdatedData
{
    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the visibility.</summary>
    public required string Visibility { get; init; }

    /// <summary>Gets the update time.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Data for passport.signal.visibility.updated.</summary>
public sealed class AquariPassportSignalVisibilityUpdatedData
{
    /// <summary>Gets the user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the source type.</summary>
    public required string SourceType { get; init; }

    /// <summary>Gets the source identifier.</summary>
    public required string SourceId { get; init; }

    /// <summary>Gets the visibility.</summary>
    public required string Visibility { get; init; }

    /// <summary>Gets the update time.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}
