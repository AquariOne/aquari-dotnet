namespace Aquari.One.Server;

/// <summary>Provides the frozen Aquari machine-scope names.</summary>
public static class AquariMachineScopes
{
    /// <summary>Gets machine context read access.</summary>
    public const string ContextRead = "machine.context.read";

    /// <summary>Gets machine health read access.</summary>
    public const string HealthRead = "machine.health.read";

    /// <summary>Gets badge issue access.</summary>
    public const string BadgesIssue = "issuer.badges.issue";

    /// <summary>Gets badge read access.</summary>
    public const string BadgesRead = "issuer.badges.read";

    /// <summary>Gets badge revoke access.</summary>
    public const string BadgesRevoke = "issuer.badges.revoke";

    /// <summary>Gets credential issue access.</summary>
    public const string CredentialsIssue = "issuer.credentials.issue";

    /// <summary>Gets credential read access.</summary>
    public const string CredentialsRead = "issuer.credentials.read";

    /// <summary>Gets credential update access.</summary>
    public const string CredentialsUpdate = "issuer.credentials.update";

    /// <summary>Gets credential revoke access.</summary>
    public const string CredentialsRevoke = "issuer.credentials.revoke";

    /// <summary>Gets entitlement issue access.</summary>
    public const string EntitlementsIssue = "issuer.entitlements.issue";

    /// <summary>Gets entitlement read access.</summary>
    public const string EntitlementsRead = "issuer.entitlements.read";

    /// <summary>Gets entitlement update access.</summary>
    public const string EntitlementsUpdate = "issuer.entitlements.update";

    /// <summary>Gets entitlement revoke access.</summary>
    public const string EntitlementsRevoke = "issuer.entitlements.revoke";

    /// <summary>Gets membership issue access.</summary>
    public const string MembershipsIssue = "issuer.memberships.issue";

    /// <summary>Gets membership read access.</summary>
    public const string MembershipsRead = "issuer.memberships.read";

    /// <summary>Gets membership update access.</summary>
    public const string MembershipsUpdate = "issuer.memberships.update";

    /// <summary>Gets membership revoke access.</summary>
    public const string MembershipsRevoke = "issuer.memberships.revoke";

    /// <summary>Gets stamp issue access.</summary>
    public const string StampsIssue = "issuer.stamps.issue";

    /// <summary>Gets stamp read access.</summary>
    public const string StampsRead = "issuer.stamps.read";

    /// <summary>Gets stamp revoke access.</summary>
    public const string StampsRevoke = "issuer.stamps.revoke";
}
