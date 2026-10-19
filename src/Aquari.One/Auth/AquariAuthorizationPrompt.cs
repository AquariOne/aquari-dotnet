namespace Aquari.One.Auth;

/// <summary>
/// Identifies an optional OpenID Connect authorization prompt.
/// </summary>
public enum AquariAuthorizationPrompt
{
    /// <summary>Require consent.</summary>
    Consent,

    /// <summary>Require interactive authentication.</summary>
    Login,

    /// <summary>Disallow interactive authentication.</summary>
    None,

    /// <summary>Request account selection.</summary>
    SelectAccount,
}
