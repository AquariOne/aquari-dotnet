namespace Aquari.One.Server.Configuration;

/// <summary>
/// Configures the trusted-server Aquari One client.
/// </summary>
public sealed class AquariServerClientOptions
{
    /// <summary>
    /// Gets the Aquari API base URI.
    /// </summary>
    public required Uri ApiUrl { get; init; }

    /// <summary>
    /// Gets the server-only Aquari machine API key.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Gets the default HTTP request timeout.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the maximum number of transport retries for eligible requests.
    /// </summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Gets the maximum delay permitted between eligible retries.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Validates this trusted-server configuration.
    /// </summary>
    /// <exception cref="AquariConfigurationException">
    /// Thrown when a required value is missing or invalid.
    /// </exception>
    public void Validate()
    {
        if (!ApiUrl.IsAbsoluteUri ||
            (ApiUrl.Scheme != Uri.UriSchemeHttps &&
             ApiUrl.Scheme != Uri.UriSchemeHttp))
        {
            throw new AquariConfigurationException(
                "Aquari API URL must be an absolute HTTP(S) URI.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new AquariConfigurationException(
                "Aquari API key must not be empty.");
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
