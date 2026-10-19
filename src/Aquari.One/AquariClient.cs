using Aquari.One.Auth;
using Aquari.One.Configuration;
using Aquari.One.Internal;
using Aquari.One.Passport;
using Aquari.One.Transport;
using Aquari.One.Trust;

namespace Aquari.One;

/// <summary>Provides the primary Aquari One runtime SDK entry point.</summary>
public sealed class AquariClient : IDisposable
{
    private readonly HttpClient? _ownedHttpClient;
    private bool _disposed;

    /// <summary>Initializes a client with an SDK-owned HTTP client.</summary>
    public AquariClient(AquariClientOptions options)
        : this(options, null)
    {
    }

    /// <summary>Initializes a client with a caller-owned HTTP client.</summary>
    public AquariClient(
        AquariClientOptions options,
        HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var issuer =
            AquariUriPolicy.NormalizeBaseUri(
                options.Issuer,
                "issuer URI");

        if (httpClient is null)
        {
            _ownedHttpClient =
                new HttpClient
                {
                    Timeout = Timeout.InfiniteTimeSpan,
                };

            httpClient = _ownedHttpClient;
        }

        var transport =
            new AquariHttpTransport(
                httpClient,
                issuer,
                options.Timeout,
                options.MaxRetries,
                options.MaxRetryDelay);

        Auth =
            new AquariAuthClient(
                transport,
                new AquariOAuthHttpClient(
                    httpClient,
                    options.Timeout),
                issuer,
                options.ClientId,
                options.ClientSecret,
                options.RedirectUri,
                options.Scopes);

        Passport =
            new AquariPassportClient(transport);

        Trust =
            new AquariTrustClient(transport);
    }

    /// <summary>Gets OAuth/OpenID Connect operations.</summary>
    public AquariAuthClient Auth { get; }

    /// <summary>Gets Passport operations.</summary>
    public AquariPassportClient Passport { get; }

    /// <summary>Gets trust-evaluation operations.</summary>
    public AquariTrustClient Trust { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Auth.Dispose();
        _ownedHttpClient?.Dispose();
        _disposed = true;
    }
}
