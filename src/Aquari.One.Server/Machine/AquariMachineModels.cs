namespace Aquari.One.Server.Machine;

/// <summary>Represents the authenticated machine context.</summary>
public sealed class AquariMachineContext
{
    /// <summary>Gets the actor type.</summary>
    public required string ActorType { get; init; }

    /// <summary>Gets the API key identifier.</summary>
    public required string ApiKeyId { get; init; }

    /// <summary>Gets the non-secret key prefix.</summary>
    public required string KeyPrefix { get; init; }

    /// <summary>Gets the key display name.</summary>
    public required string KeyName { get; init; }

    /// <summary>Gets the organization identifier.</summary>
    public required string OrganizationId { get; init; }

    /// <summary>Gets the application identifier.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Gets the environment identifier.</summary>
    public required string EnvironmentId { get; init; }

    /// <summary>Gets the environment display name.</summary>
    public required string EnvironmentName { get; init; }

    /// <summary>Gets the environment key.</summary>
    public required string Environment { get; init; }

    /// <summary>Gets the machine scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }
}

/// <summary>Represents machine API health.</summary>
public sealed class AquariMachineHealth
{
    /// <summary>Gets whether the machine context is healthy.</summary>
    public required bool Ok { get; init; }

    /// <summary>Gets the actor type.</summary>
    public required string ActorType { get; init; }

    /// <summary>Gets the API key identifier.</summary>
    public required string ApiKeyId { get; init; }

    /// <summary>Gets the application identifier.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Gets the environment key.</summary>
    public required string Environment { get; init; }
}
