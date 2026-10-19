using Aquari.One.Internal;
using Aquari.One.Oidc;
using Aquari.One.Transport;

namespace Aquari.One.Auth;

/// <summary>Provides Aquari OAuth and OpenID Connect operations.</summary>
public sealed class AquariAuthClient : IDisposable
{
    private readonly AquariHttpTransport _transport;
    private readonly AquariOAuthHttpClient _oauthHttp;
    private readonly Uri _issuer;
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly Uri _redirectUri;
    private readonly IReadOnlyList<string> _scopes;
    private readonly SemaphoreSlim _discoveryLock = new(1, 1);
    private AquariOidcDiscovery? _discovery;
    private bool _disposed;

    internal AquariAuthClient(
        AquariHttpTransport transport,
        AquariOAuthHttpClient oauthHttp,
        Uri issuer,
        string clientId,
        string? clientSecret,
        Uri redirectUri,
        IReadOnlyList<string> scopes)
    {
        _transport = transport;
        _oauthHttp = oauthHttp;
        _issuer = issuer;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _redirectUri = redirectUri;
        _scopes = scopes.ToArray();
    }

    /// <summary>Loads and validates Aquari OIDC discovery metadata.</summary>
    public async Task<AquariOidcDiscovery> DiscoverAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!forceRefresh && _discovery is not null)
        {
            return _discovery;
        }

        await _discoveryLock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (!forceRefresh && _discovery is not null)
            {
                return _discovery;
            }

            using var document =
                await _transport.GetJsonAsync(
                    "/.well-known/openid-configuration",
                    cancellationToken).ConfigureAwait(false);

            _discovery =
                AquariOidcDiscoveryParser.Parse(
                    document.RootElement,
                    _issuer);

            return _discovery;
        }
        finally
        {
            _discoveryLock.Release();
        }
    }

    /// <summary>Prepares Authorization Code + S256 PKCE + state + nonce.</summary>
    public async Task<AquariBeginAuthorizationResult> BeginAuthorizationAsync(
        AquariAuthorizationPrompt? prompt = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var discovery =
            await DiscoverAsync(
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var verifier = AquariPkce.CreateCodeVerifier();

        var transaction =
            new AquariAuthorizationTransaction
            {
                Issuer = _issuer,
                ClientId = _clientId,
                RedirectUri = _redirectUri,
                Scopes = _scopes.ToArray(),
                CodeVerifier = verifier,
                State =
                    AquariPkce.CreateSecureRandomValue(32),
                Nonce =
                    AquariPkce.CreateSecureRandomValue(32),
                CreatedAt = DateTimeOffset.UtcNow,
            };

        return new AquariBeginAuthorizationResult
        {
            AuthorizationUri =
                AquariAuthorizationFlow.BuildAuthorizationUri(
                    discovery.AuthorizationEndpoint,
                    transaction,
                    AquariPkce.CreateS256CodeChallenge(verifier),
                    prompt),
            Transaction = transaction,
        };
    }

    /// <summary>Completes a callback and validates its returned tokens.</summary>
    public async Task<AquariCompleteAuthorizationResult>
        CompleteAuthorizationAsync(
            Uri callbackUri,
            AquariAuthorizationTransaction transaction,
            CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        AquariAuthorizationFlow.ValidateTransaction(
            transaction,
            _issuer,
            _clientId,
            _redirectUri,
            _scopes,
            DateTimeOffset.UtcNow);

        var discovery =
            await DiscoverAsync(
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        var callback =
            AquariAuthorizationFlow.ParseCallback(
                callbackUri,
                transaction,
                discovery.AuthorizationResponseIssuerParameterSupported);

        return await AquariOAuthProtocol
            .ExchangeAuthorizationCodeAsync(
                _oauthHttp,
                discovery,
                _clientId,
                _clientSecret,
                _redirectUri,
                callback.Code,
                transaction.CodeVerifier,
                transaction.Scopes,
                transaction.Nonce,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Exchanges a rotating refresh token.</summary>
    public async Task<AquariTokenSet> RefreshAsync(
        AquariTokenSet tokens,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(tokens);

        var discovery =
            await DiscoverAsync(
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        return await AquariOAuthProtocol
            .RefreshAsync(
                _oauthHttp,
                discovery,
                _clientId,
                _clientSecret,
                tokens,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Loads UserInfo and requires subject continuity.</summary>
    public async Task<AquariIdentity> UserInfoAsync(
        AquariTokenSet tokens,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(tokens);

        var discovery =
            await DiscoverAsync(
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        return await AquariOAuthProtocol
            .UserInfoAsync(
                _oauthHttp,
                discovery,
                tokens,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _discoveryLock.Dispose();
        _disposed = true;
    }
}
