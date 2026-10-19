using Aquari.One.Internal;

namespace Aquari.One.Configuration;

/// <summary>
/// Configures the Aquari One runtime client.
/// </summary>
public sealed class AquariClientOptions
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

    /// <summary>Gets the explicit ordered OAuth/Aquari scopes.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the default request timeout.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the maximum eligible transport retries.</summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>Gets the maximum eligible retry delay.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Validates this SDK configuration.</summary>
    /// <exception cref="AquariConfigurationException">
    /// Thrown when the configuration violates the frozen SDK contract.
    /// </exception>
    public void Validate()
    {
        _ = AquariUriPolicy.NormalizeBaseUri(Issuer, "issuer URI");

        if (string.IsNullOrWhiteSpace(ClientId) ||
            !string.Equals(
                ClientId,
                ClientId.Trim(),
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari client ID must be a non-empty normalized value.");
        }

        if (ClientSecret is not null &&
            (string.IsNullOrWhiteSpace(ClientSecret) ||
             !string.Equals(
                 ClientSecret,
                 ClientSecret.Trim(),
                 StringComparison.Ordinal)))
        {
            throw new AquariConfigurationException(
                "Aquari client secret must be omitted or a non-empty normalized value.");
        }

        AquariUriPolicy.ValidateRedirectUri(RedirectUri);

        if (Scopes.Count == 0)
        {
            throw new AquariConfigurationException(
                "Aquari scopes must contain at least one value.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var scope in Scopes)
        {
            if (string.IsNullOrWhiteSpace(scope) ||
                !string.Equals(
                    scope,
                    scope.Trim(),
                    StringComparison.Ordinal) ||
                scope.Any(char.IsWhiteSpace) ||
                !seen.Add(scope))
            {
                throw new AquariConfigurationException(
                    "Aquari scopes must be unique normalized values without whitespace.");
            }
        }

        if (!seen.Contains(AquariScopes.OpenId))
        {
            throw new AquariConfigurationException(
                "Aquari OAuth clients must explicitly request the openid scope.");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new AquariConfigurationException(
                "Aquari timeout must be greater than zero.");
        }

        if (MaxRetries < 0)
        {
            throw new AquariConfigurationException(
                "Aquari max retries must not be negative.");
        }

        if (MaxRetryDelay < TimeSpan.Zero)
        {
            throw new AquariConfigurationException(
                "Aquari max retry delay must not be negative.");
        }
    }
}
