namespace Aquari.One.Trust;

/// <summary>Represents one trust evaluation.</summary>
public sealed class AquariTrustEvaluation
{
    /// <summary>Gets the evaluation identifier.</summary>
    public required string EvaluationId { get; init; }

    /// <summary>Gets the subject identifier.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the consuming application identifier.</summary>
    public required string ConsumerApplicationId { get; init; }

    /// <summary>Gets the evaluation time.</summary>
    public required DateTimeOffset EvaluatedAt { get; init; }

    /// <summary>Gets trust outcomes.</summary>
    public required IReadOnlyList<AquariTrustOutcome> Outcomes { get; init; }
}

/// <summary>Represents one trust outcome.</summary>
public sealed class AquariTrustOutcome
{
    /// <summary>Gets the outcome type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the outcome value.</summary>
    public required string Value { get; init; }

    /// <summary>Gets supporting mappings/signals.</summary>
    public required IReadOnlyList<AquariTrustSupport> Supports { get; init; }
}

/// <summary>Represents one trust support record.</summary>
public sealed class AquariTrustSupport
{
    /// <summary>Gets the mapping identifier.</summary>
    public required string MappingId { get; init; }

    /// <summary>Gets the mapping version.</summary>
    public required int MappingVersion { get; init; }

    /// <summary>Gets the trusted issuer identifier.</summary>
    public required string TrustedIssuerId { get; init; }

    /// <summary>Gets the supporting signal.</summary>
    public required AquariTrustSignal Signal { get; init; }
}

/// <summary>Represents one signal used during trust evaluation.</summary>
public sealed class AquariTrustSignal
{
    /// <summary>Gets the signal identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the signal family.</summary>
    public required string SignalFamily { get; init; }

    /// <summary>Gets the definition identifier, when present.</summary>
    public string? DefinitionId { get; init; }

    /// <summary>Gets the signal authority.</summary>
    public required AquariTrustAuthority Authority { get; init; }

    /// <summary>Gets the signal scope, when present.</summary>
    public AquariTrustScope? Scope { get; init; }

    /// <summary>Gets the issue time.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Gets the expiration time, when present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>Represents trust signal authority.</summary>
public sealed class AquariTrustAuthority
{
    /// <summary>Gets the authority type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the authority identifier.</summary>
    public required string Id { get; init; }
}

/// <summary>Represents trust signal scope.</summary>
public sealed class AquariTrustScope
{
    /// <summary>Gets the scope type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the scope identifier.</summary>
    public required string Id { get; init; }
}
