namespace Aquari.One.Server.Issuer;

/// <summary>Represents an issuer identity.</summary>
public sealed class AquariIssuerIdentity
{
    /// <summary>Gets the issuer type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the issuer identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the issuer name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the issuer logo URL, when present.</summary>
    public string? LogoUrl { get; init; }
}

/// <summary>Configures idempotent issue operations.</summary>
public sealed class AquariIssueOptions
{
    /// <summary>Gets or sets the caller-provided idempotency key.</summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>Input for badge issuance.</summary>
public sealed class AquariBadgeIssueInput
{
    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }
}

/// <summary>Represents a badge signal.</summary>
public sealed class AquariBadgeSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the definition identifier.</summary>
    public required string DefinitionId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariIssuerIdentity Issuer { get; init; }

    /// <summary>Gets the badge name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description, when present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the criteria, when present.</summary>
    public string? Criteria { get; init; }

    /// <summary>Gets the image URL, when present.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Gets the default visibility.</summary>
    public required string DefaultVisibility { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the revoke time, when present.</summary>
    public DateTimeOffset? RevokedAt { get; init; }
}

/// <summary>Represents badge issuance.</summary>
public sealed class AquariBadgeIssueResult
{
    /// <summary>Gets the issued signal.</summary>
    public required AquariBadgeSignal Signal { get; init; }

    /// <summary>Gets whether the response replayed an idempotent issue.</summary>
    public required bool IdempotentReplay { get; init; }
}

/// <summary>Represents credential evidence input.</summary>
public sealed class AquariCredentialEvidenceInput
{
    /// <summary>Gets the evidence type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the evidence reference.</summary>
    public required string Reference { get; init; }

    /// <summary>Gets the verification time.</summary>
    public required DateTimeOffset VerifiedAt { get; init; }
}

/// <summary>Represents stored credential evidence.</summary>
public sealed class AquariCredentialEvidence
{
    /// <summary>Gets the evidence identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the evidence type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the evidence reference.</summary>
    public required string Reference { get; init; }

    /// <summary>Gets the verification time.</summary>
    public required DateTimeOffset VerifiedAt { get; init; }

    /// <summary>Gets the creation time.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Input for credential issuance.</summary>
public sealed class AquariCredentialIssueInput
{
    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the expiry, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets evidence, when supplied.</summary>
    public IReadOnlyList<AquariCredentialEvidenceInput>? Evidence { get; init; }
}

/// <summary>Input for credential updates.</summary>
public sealed class AquariCredentialUpdateInput
{
    /// <summary>Gets the expiry, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets evidence, when supplied.</summary>
    public IReadOnlyList<AquariCredentialEvidenceInput>? Evidence { get; init; }
}

/// <summary>Represents a credential signal.</summary>
public sealed class AquariCredentialSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the definition identifier.</summary>
    public required string DefinitionId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariIssuerIdentity Issuer { get; init; }

    /// <summary>Gets the definition key.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the credential name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description, when present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the assurance level.</summary>
    public required string AssuranceLevel { get; init; }

    /// <summary>Gets the verification method.</summary>
    public required string VerificationMethod { get; init; }

    /// <summary>Gets the default visibility.</summary>
    public required string DefaultVisibility { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets whether the signal is effective.</summary>
    public required bool Effective { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the expiry, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets the revoke time, when present.</summary>
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>Gets evidence records.</summary>
    public required IReadOnlyList<AquariCredentialEvidence> Evidence { get; init; }
}

/// <summary>Represents credential issuance.</summary>
public sealed class AquariCredentialIssueResult
{
    /// <summary>Gets the issued signal.</summary>
    public required AquariCredentialSignal Signal { get; init; }

    /// <summary>Gets whether the result is an idempotent replay.</summary>
    public required bool IdempotentReplay { get; init; }
}

/// <summary>Input for entitlement issuance.</summary>
public sealed class AquariEntitlementIssueInput
{
    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the plan identifier, when present.</summary>
    public string? PlanId { get; init; }

    /// <summary>Gets the entitlement status, when supplied.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the start time, when supplied.</summary>
    public DateTimeOffset? StartsAt { get; init; }

    /// <summary>Gets the expiration time, when supplied.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>Input for entitlement updates.</summary>
public sealed class AquariEntitlementUpdateInput
{
    /// <summary>Gets the plan identifier, when present.</summary>
    public string? PlanId { get; init; }

    /// <summary>Gets the entitlement status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the start time.</summary>
    public required DateTimeOffset StartsAt { get; init; }

    /// <summary>Gets the expiration time, when supplied.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>Represents an entitlement plan.</summary>
public sealed class AquariEntitlementPlan
{
    /// <summary>Gets the plan identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the plan name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the plan slug.</summary>
    public required string Slug { get; init; }

    /// <summary>Gets the billing period.</summary>
    public required string BillingPeriod { get; init; }

    /// <summary>Gets the price in cents.</summary>
    public required int PriceCents { get; init; }

    /// <summary>Gets the currency.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the plan status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the creation time.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the update time, when present.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Gets the disable time, when present.</summary>
    public DateTimeOffset? DisabledAt { get; init; }
}

/// <summary>Represents an entitlement signal.</summary>
public sealed class AquariEntitlementSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the definition identifier.</summary>
    public required string DefinitionId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariIssuerIdentity Issuer { get; init; }

    /// <summary>Gets the entitlement name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description, when present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the plan, when present.</summary>
    public AquariEntitlementPlan? Plan { get; init; }

    /// <summary>Gets the source.</summary>
    public required string Source { get; init; }

    /// <summary>Gets the status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets whether the signal is effective.</summary>
    public required bool Effective { get; init; }

    /// <summary>Gets the start time.</summary>
    public required DateTimeOffset StartsAt { get; init; }

    /// <summary>Gets the expiration time, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Gets the revoke time, when present.</summary>
    public DateTimeOffset? RevokedAt { get; init; }
}

/// <summary>Represents entitlement issuance.</summary>
public sealed class AquariEntitlementIssueResult
{
    /// <summary>Gets the issued signal.</summary>
    public required AquariEntitlementSignal Signal { get; init; }

    /// <summary>Gets whether the result is an idempotent replay.</summary>
    public required bool IdempotentReplay { get; init; }
}

/// <summary>Input for membership issuance.</summary>
public sealed class AquariMembershipIssueInput
{
    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the optional formal role definition.</summary>
    public string? RoleId { get; init; }
}

/// <summary>Input for membership updates.</summary>
public sealed class AquariMembershipUpdateInput
{
    /// <summary>Gets the role definition, or null to clear the formal role.</summary>
    public string? RoleId { get; init; }
}

/// <summary>Represents a formal membership role definition.</summary>
public sealed class AquariMembershipRole
{
    /// <summary>Gets the role identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the role name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description, when present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the role status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the creation time.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the update time, when present.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Gets the retire time, when present.</summary>
    public DateTimeOffset? RetiredAt { get; init; }
}

/// <summary>Represents a membership signal.</summary>
public sealed class AquariMembershipSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the optional role definition identifier.</summary>
    public string? DefinitionId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariIssuerIdentity Issuer { get; init; }

    /// <summary>Gets the presentation type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the role presentation label.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the formal role definition, when assigned.</summary>
    public AquariMembershipRole? RoleDefinition { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets whether the signal is effective.</summary>
    public required bool Effective { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the revoke time, when present.</summary>
    public DateTimeOffset? RevokedAt { get; init; }
}

/// <summary>Represents membership issuance.</summary>
public sealed class AquariMembershipIssueResult
{
    /// <summary>Gets the issued signal.</summary>
    public required AquariMembershipSignal Signal { get; init; }

    /// <summary>Gets whether the result is an idempotent replay.</summary>
    public required bool IdempotentReplay { get; init; }
}

/// <summary>Input for stamp issuance.</summary>
public sealed class AquariStampIssueInput
{
    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the scope type.</summary>
    public required string ScopeType { get; init; }

    /// <summary>Gets the scope identifier.</summary>
    public required string ScopeId { get; init; }

    /// <summary>Gets the occurrence time.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>Represents a stamp scope.</summary>
public sealed class AquariStampScope
{
    /// <summary>Gets the scope type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the scope identifier.</summary>
    public required string Id { get; init; }
}

/// <summary>Represents a stamp signal.</summary>
public sealed class AquariStampSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the definition identifier.</summary>
    public required string DefinitionId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the issuer.</summary>
    public required AquariIssuerIdentity Issuer { get; init; }

    /// <summary>Gets the definition key.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the stamp name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the description, when present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the image URL, when present.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Gets the scope.</summary>
    public required AquariStampScope Scope { get; init; }

    /// <summary>Gets the occurrence time.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the default visibility.</summary>
    public required string DefaultVisibility { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets whether the signal is effective.</summary>
    public required bool Effective { get; init; }

    /// <summary>Gets the revoke time, when present.</summary>
    public DateTimeOffset? RevokedAt { get; init; }
}

/// <summary>Represents stamp issuance.</summary>
public sealed class AquariStampIssueResult
{
    /// <summary>Gets the issued signal.</summary>
    public required AquariStampSignal Signal { get; init; }

    /// <summary>Gets whether the result is an idempotent replay.</summary>
    public required bool IdempotentReplay { get; init; }
}
