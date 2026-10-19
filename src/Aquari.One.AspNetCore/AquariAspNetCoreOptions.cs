using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Session;
using Aquari.One.Configuration;
namespace Aquari.One.AspNetCore;

/// <summary>
/// Configures the Aquari One ASP.NET Core integration.
/// </summary>
public sealed class AquariAspNetCoreOptions
{
    /// <summary>Gets the exact Aquari issuer URI.</summary>
    public required Uri Issuer { get; init; }

    /// <summary>Gets the OAuth client identifier.</summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the confidential OAuth client secret. Public clients leave this unset.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>Gets the exact registered OAuth redirect URI.</summary>
    public required Uri RedirectUri { get; init; }

    /// <summary>Gets the explicit ordered OAuth scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the durable server-side session store.</summary>
    public required IAquariSessionStore SessionStore { get; init; }

    /// <summary>
    /// Gets the refresh lock. Multi-instance deployments must supply a shared lock.
    /// </summary>
    public IAquariRefreshLock? RefreshLock { get; init; }

    /// <summary>Gets the application session cookie name.</summary>
    public string SessionCookieName { get; init; } = "aquari_session";

    /// <summary>
    /// Gets the optional same-host or parent domain for the durable
    /// application-session cookie. Authorization transaction cookies remain host-only.
    /// </summary>
    public string? SessionCookieDomain { get; init; }

    /// <summary>Gets the authorization transaction cookie name.</summary>
    public string TransactionCookieName { get; init; } = "aquari_auth";

    /// <summary>
    /// Gets whether cookies require HTTPS. Null follows the redirect URI scheme.
    /// </summary>
    public bool? SecureCookies { get; init; }

    /// <summary>Gets the maximum server-side session lifetime.</summary>
    public TimeSpan SessionTtl { get; init; } = TimeSpan.FromDays(30);

    /// <summary>Gets the HTTP request timeout.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the maximum eligible transport retries.</summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>Gets the maximum eligible retry delay.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Validates the ASP.NET Core SDK configuration.</summary>
    public void Validate()
    {
        var core =
            new AquariClientOptions
            {
                Issuer = Issuer,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RedirectUri = RedirectUri,
                Scopes = Scopes,
                Timeout = Timeout,
                MaxRetries = MaxRetries,
                MaxRetryDelay = MaxRetryDelay,
            };

        core.Validate();

        ArgumentNullException.ThrowIfNull(SessionStore);

        ValidateCookieName(SessionCookieName);
        ValidateCookieName(TransactionCookieName);

        _ = ResolveSessionCookieDomain();

        if (string.Equals(
                SessionCookieName,
                TransactionCookieName,
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari session and transaction cookie names must be different.");
        }

        if (SessionTtl < TimeSpan.FromMinutes(1))
        {
            throw new AquariConfigurationException(
                "Aquari session TTL must be at least one minute.");
        }
    }

    internal AquariClientOptions ToClientOptions()
    {
        return new AquariClientOptions
        {
            Issuer = Issuer,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            RedirectUri = RedirectUri,
            Scopes = Scopes,
            Timeout = Timeout,
            MaxRetries = MaxRetries,
            MaxRetryDelay = MaxRetryDelay,
        };
    }

    internal bool ResolveSecureCookies()
    {
        return SecureCookies ??
            string.Equals(
                RedirectUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase);
    }

    internal string? ResolveSessionCookieDomain()
    {
        if (SessionCookieDomain is null)
        {
            return null;
        }

        var normalized =
            SessionCookieDomain
                .Trim()
                .ToLowerInvariant();

        if (normalized.StartsWith('.'))
        {
            normalized =
                normalized[1..];
        }

        if (normalized.Length == 0 ||
            normalized.Length > 253 ||
            normalized.EndsWith('.') ||
            normalized.Contains(
                "..",
                StringComparison.Ordinal) ||
            Uri.CheckHostName(normalized) !=
                UriHostNameType.Dns)
        {
            throw new AquariConfigurationException(
                "Aquari session cookie domain must be a valid DNS hostname.");
        }

        var redirectHost =
            RedirectUri.Host
                .ToLowerInvariant();

        if (!string.Equals(
                redirectHost,
                normalized,
                StringComparison.OrdinalIgnoreCase) &&
            !redirectHost.EndsWith(
                $".{normalized}",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new AquariConfigurationException(
                "Aquari session cookie domain must match the redirect host or one of its parent domains.");
        }

        return normalized;
    }

    private static void ValidateCookieName(
        string value)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Any(
                static character =>
                    !IsCookieTokenCharacter(character)))
        {
            throw new AquariConfigurationException(
                "Aquari cookie names must be valid HTTP cookie names.");
        }
    }

    private static bool IsCookieTokenCharacter(
        char value)
    {
        return char.IsAsciiLetterOrDigit(value) ||
            value is
                '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or
                '-' or '.' or '^' or '_' or '`' or '|' or '~';
    }
}
