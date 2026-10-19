namespace Aquari.One;

/// <summary>
/// Provides standard Aquari runtime scopes.
/// </summary>
public static class AquariScopes
{
    /// <summary>Gets the OpenID Connect identity scope.</summary>
    public const string OpenId = "openid";

    /// <summary>Gets the standard profile scope.</summary>
    public const string Profile = "profile";

    /// <summary>Gets the standard email scope.</summary>
    public const string Email = "email";

    /// <summary>Gets the OAuth refresh-token scope.</summary>
    public const string OfflineAccess = "offline_access";

    /// <summary>Gets connected Passport read access.</summary>
    public const string PassportRead = "passport.read";
}
