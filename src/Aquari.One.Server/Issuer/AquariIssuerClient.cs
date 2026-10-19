using System.Globalization;
using Aquari.One.Server.Internal;
using Aquari.One.Transport;

namespace Aquari.One.Server.Issuer;

/// <summary>Groups the five supported machine issuer families.</summary>
public sealed class AquariIssuerClient
{
    internal AquariIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
    {
        Badges =
            new AquariBadgeIssuerClient(
                transport,
                headers);
        Credentials =
            new AquariCredentialIssuerClient(
                transport,
                headers);
        Entitlements =
            new AquariEntitlementIssuerClient(
                transport,
                headers);
        Memberships =
            new AquariMembershipIssuerClient(
                transport,
                headers);
        Stamps =
            new AquariStampIssuerClient(
                transport,
                headers);
    }

    /// <summary>Gets badge issuer operations.</summary>
    public AquariBadgeIssuerClient Badges { get; }

    /// <summary>Gets credential issuer operations.</summary>
    public AquariCredentialIssuerClient Credentials { get; }

    /// <summary>Gets entitlement issuer operations.</summary>
    public AquariEntitlementIssuerClient Entitlements { get; }

    /// <summary>Gets membership issuer operations.</summary>
    public AquariMembershipIssuerClient Memberships { get; }

    /// <summary>Gets stamp issuer operations.</summary>
    public AquariStampIssuerClient Stamps { get; }
}

/// <summary>Provides shared issuer-family mechanics.</summary>
public abstract class AquariIssuerFamilyClient
{
    private readonly AquariHttpTransport _transport;
    private readonly IReadOnlyDictionary<string, string> _headers;

    internal AquariIssuerFamilyClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
    {
        _transport = transport;
        _headers = headers;
    }

    /// <summary>Validates and encodes a route identifier.</summary>
    protected static string Encode(string value)
    {
        return Uri.EscapeDataString(
            Required(value, nameof(value)));
    }

    /// <summary>Validates a required normalized value.</summary>
    protected static string Required(
        string value,
        string label)
    {
        return AquariServerWire.Required(
            value,
            label);
    }

    /// <summary>Sends an idempotent issuer operation.</summary>
    protected async Task<T> IssueAsync<T>(
        string path,
        object json,
        AquariIssueOptions? options,
        CancellationToken cancellationToken)
    {
        var headers =
            new Dictionary<string, string>(
                _headers,
                StringComparer.OrdinalIgnoreCase)
            {
                ["Idempotency-Key"] =
                    AquariServerWire.Idempotency(
                        options?.IdempotencyKey),
            };

        using var document =
            await _transport.SendJsonAsync(
                path,
                HttpMethod.Post,
                json,
                headers,
                AquariRetryMode.Idempotent,
                cancellationToken).ConfigureAwait(false);

        return AquariServerWire.Deserialize<T>(
            document.RootElement,
            "issuer");
    }

    /// <summary>Sends an issuer read.</summary>
    protected async Task<T> GetAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var document =
            await _transport.GetJsonAsync(
                path,
                _headers,
                cancellationToken).ConfigureAwait(false);

        return AquariServerWire.Deserialize<T>(
            document.RootElement,
            "issuer");
    }

    /// <summary>Sends a non-retried issuer mutation.</summary>
    protected async Task<T> MutationAsync<T>(
        string path,
        HttpMethod method,
        object? json,
        CancellationToken cancellationToken)
    {
        using var document =
            await _transport.SendJsonAsync(
                path,
                method,
                json,
                _headers,
                AquariRetryMode.Never,
                cancellationToken).ConfigureAwait(false);

        return AquariServerWire.Deserialize<T>(
            document.RootElement,
            "issuer");
    }
}

/// <summary>Provides badge issuer operations.</summary>
public sealed class AquariBadgeIssuerClient : AquariIssuerFamilyClient
{
    internal AquariBadgeIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
        : base(transport, headers)
    {
    }

    /// <summary>Issues a badge signal.</summary>
    public Task<AquariBadgeIssueResult> IssueAsync(
        string definitionId,
        AquariBadgeIssueInput input,
        AquariIssueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return IssueAsync<AquariBadgeIssueResult>(
            $"/api/v1/machine/issuer/badge-definitions/{Encode(definitionId)}/signals",
            new
            {
                subjectId =
                    Required(
                        input.SubjectId,
                        nameof(input.SubjectId)),
            },
            options,
            cancellationToken);
    }

    /// <summary>Gets a badge signal.</summary>
    public Task<AquariBadgeSignal> GetAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<AquariBadgeSignal>(
            $"/api/v1/machine/issuer/badge-signals/{Encode(signalId)}",
            cancellationToken);
    }

    /// <summary>Revokes a badge signal.</summary>
    public Task<AquariBadgeSignal> RevokeAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariBadgeSignal>(
            $"/api/v1/machine/issuer/badge-signals/{Encode(signalId)}/revoke",
            HttpMethod.Post,
            null,
            cancellationToken);
    }
}

/// <summary>Provides credential issuer operations.</summary>
public sealed class AquariCredentialIssuerClient : AquariIssuerFamilyClient
{
    internal AquariCredentialIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
        : base(transport, headers)
    {
    }

    /// <summary>Issues a credential signal.</summary>
    public Task<AquariCredentialIssueResult> IssueAsync(
        string definitionId,
        AquariCredentialIssueInput input,
        AquariIssueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return IssueAsync<AquariCredentialIssueResult>(
            $"/api/v1/machine/issuer/credential-definitions/{Encode(definitionId)}/signals",
            CredentialBody(
                input.SubjectId,
                input.ExpiresAt,
                input.Evidence),
            options,
            cancellationToken);
    }

    /// <summary>Gets a credential signal.</summary>
    public Task<AquariCredentialSignal> GetAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<AquariCredentialSignal>(
            $"/api/v1/machine/issuer/credential-signals/{Encode(signalId)}",
            cancellationToken);
    }

    /// <summary>Updates a credential signal.</summary>
    public Task<AquariCredentialSignal> UpdateAsync(
        string signalId,
        AquariCredentialUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return MutationAsync<AquariCredentialSignal>(
            $"/api/v1/machine/issuer/credential-signals/{Encode(signalId)}",
            HttpMethod.Patch,
            CredentialBody(
                null,
                input.ExpiresAt,
                input.Evidence),
            cancellationToken);
    }

    /// <summary>Expires a credential signal.</summary>
    public Task<AquariCredentialSignal> ExpireAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariCredentialSignal>(
            $"/api/v1/machine/issuer/credential-signals/{Encode(signalId)}/expire",
            HttpMethod.Post,
            null,
            cancellationToken);
    }

    /// <summary>Revokes a credential signal.</summary>
    public Task<AquariCredentialSignal> RevokeAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariCredentialSignal>(
            $"/api/v1/machine/issuer/credential-signals/{Encode(signalId)}/revoke",
            HttpMethod.Post,
            null,
            cancellationToken);
    }

    private static Dictionary<string, object?> CredentialBody(
        string? subjectId,
        DateTimeOffset? expiresAt,
        IReadOnlyList<AquariCredentialEvidenceInput>? evidence)
    {
        var body =
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["expiresAt"] =
                    expiresAt?.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                ["evidence"] =
                    evidence?.Select(
                        static item =>
                            new
                            {
                                type =
                                    Required(
                                        item.Type,
                                        nameof(item.Type)),
                                reference =
                                    Required(
                                        item.Reference,
                                        nameof(item.Reference)),
                                verifiedAt =
                                    item.VerifiedAt.ToString(
                                        "O",
                                        CultureInfo.InvariantCulture),
                            })
                        .ToArray(),
            };

        if (subjectId is not null)
        {
            body["subjectId"] =
                Required(
                    subjectId,
                    nameof(subjectId));
        }

        return body;
    }
}

/// <summary>Provides entitlement issuer operations.</summary>
public sealed class AquariEntitlementIssuerClient : AquariIssuerFamilyClient
{
    internal AquariEntitlementIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
        : base(transport, headers)
    {
    }

    /// <summary>Issues an entitlement signal.</summary>
    public Task<AquariEntitlementIssueResult> IssueAsync(
        string definitionId,
        AquariEntitlementIssueInput input,
        AquariIssueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return IssueAsync<AquariEntitlementIssueResult>(
            $"/api/v1/machine/issuer/entitlement-definitions/{Encode(definitionId)}/signals",
            EntitlementBody(
                input.SubjectId,
                input.PlanId,
                input.Status,
                input.StartsAt,
                input.ExpiresAt),
            options,
            cancellationToken);
    }

    /// <summary>Gets an entitlement signal.</summary>
    public Task<AquariEntitlementSignal> GetAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<AquariEntitlementSignal>(
            $"/api/v1/machine/issuer/entitlement-signals/{Encode(signalId)}",
            cancellationToken);
    }

    /// <summary>Updates an entitlement signal.</summary>
    public Task<AquariEntitlementSignal> UpdateAsync(
        string signalId,
        AquariEntitlementUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return MutationAsync<AquariEntitlementSignal>(
            $"/api/v1/machine/issuer/entitlement-signals/{Encode(signalId)}",
            HttpMethod.Patch,
            EntitlementBody(
                null,
                input.PlanId,
                input.Status,
                input.StartsAt,
                input.ExpiresAt),
            cancellationToken);
    }

    /// <summary>Expires an entitlement signal.</summary>
    public Task<AquariEntitlementSignal> ExpireAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariEntitlementSignal>(
            $"/api/v1/machine/issuer/entitlement-signals/{Encode(signalId)}/expire",
            HttpMethod.Post,
            null,
            cancellationToken);
    }

    /// <summary>Revokes an entitlement signal.</summary>
    public Task<AquariEntitlementSignal> RevokeAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariEntitlementSignal>(
            $"/api/v1/machine/issuer/entitlement-signals/{Encode(signalId)}/revoke",
            HttpMethod.Post,
            null,
            cancellationToken);
    }

    private static Dictionary<string, object?> EntitlementBody(
        string? subjectId,
        string? planId,
        string? status,
        DateTimeOffset? startsAt,
        DateTimeOffset? expiresAt)
    {
        var body =
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["planId"] =
                    planId is null
                        ? null
                        : Required(
                            planId,
                            nameof(planId)),
                ["status"] =
                    status is null
                        ? null
                        : Required(
                            status,
                            nameof(status)),
                ["startsAt"] =
                    startsAt?.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                ["expiresAt"] =
                    expiresAt?.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
            };

        if (subjectId is not null)
        {
            body["subjectId"] =
                Required(
                    subjectId,
                    nameof(subjectId));
        }

        return body;
    }
}

/// <summary>Provides membership issuer operations.</summary>
public sealed class AquariMembershipIssuerClient : AquariIssuerFamilyClient
{
    internal AquariMembershipIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
        : base(transport, headers)
    {
    }

    /// <summary>Issues an application membership.</summary>
    public Task<AquariMembershipIssueResult> IssueAsync(
        AquariMembershipIssueInput input,
        AquariIssueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var body =
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["subjectId"] =
                    Required(
                        input.SubjectId,
                        nameof(input.SubjectId)),
            };

        if (input.RoleId is not null)
        {
            body["roleId"] =
                Required(
                    input.RoleId,
                    nameof(input.RoleId));
        }

        return IssueAsync<AquariMembershipIssueResult>(
            "/api/v1/machine/issuer/memberships",
            body,
            options,
            cancellationToken);
    }

    /// <summary>Gets a membership signal.</summary>
    public Task<AquariMembershipSignal> GetAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<AquariMembershipSignal>(
            $"/api/v1/machine/issuer/membership-signals/{Encode(signalId)}",
            cancellationToken);
    }

    /// <summary>Updates or clears the formal membership role.</summary>
    public Task<AquariMembershipSignal> UpdateAsync(
        string signalId,
        AquariMembershipUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return MutationAsync<AquariMembershipSignal>(
            $"/api/v1/machine/issuer/membership-signals/{Encode(signalId)}",
            HttpMethod.Patch,
            new
            {
                roleId =
                    input.RoleId is null
                        ? null
                        : Required(
                            input.RoleId,
                            nameof(input.RoleId)),
            },
            cancellationToken);
    }

    /// <summary>Revokes a membership signal.</summary>
    public Task<AquariMembershipSignal> RevokeAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariMembershipSignal>(
            $"/api/v1/machine/issuer/membership-signals/{Encode(signalId)}/revoke",
            HttpMethod.Post,
            null,
            cancellationToken);
    }
}

/// <summary>Provides stamp issuer operations.</summary>
public sealed class AquariStampIssuerClient : AquariIssuerFamilyClient
{
    internal AquariStampIssuerClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
        : base(transport, headers)
    {
    }

    /// <summary>Issues a stamp signal.</summary>
    public Task<AquariStampIssueResult> IssueAsync(
        string definitionId,
        AquariStampIssueInput input,
        AquariIssueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return IssueAsync<AquariStampIssueResult>(
            $"/api/v1/machine/issuer/stamp-definitions/{Encode(definitionId)}/signals",
            new
            {
                subjectId =
                    Required(
                        input.SubjectId,
                        nameof(input.SubjectId)),
                scopeType =
                    Required(
                        input.ScopeType,
                        nameof(input.ScopeType)),
                scopeId =
                    Required(
                        input.ScopeId,
                        nameof(input.ScopeId)),
                occurredAt =
                    input.OccurredAt.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
            },
            options,
            cancellationToken);
    }

    /// <summary>Gets a stamp signal.</summary>
    public Task<AquariStampSignal> GetAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<AquariStampSignal>(
            $"/api/v1/machine/issuer/stamp-signals/{Encode(signalId)}",
            cancellationToken);
    }

    /// <summary>Revokes a stamp signal.</summary>
    public Task<AquariStampSignal> RevokeAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        return MutationAsync<AquariStampSignal>(
            $"/api/v1/machine/issuer/stamp-signals/{Encode(signalId)}/revoke",
            HttpMethod.Post,
            null,
            cancellationToken);
    }
}
