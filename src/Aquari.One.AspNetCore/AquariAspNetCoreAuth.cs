using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Internal;
using Aquari.One.AspNetCore.Session;
using Aquari.One.Auth;
using Microsoft.AspNetCore.Http;

namespace Aquari.One.AspNetCore;

/// <summary>
/// Provides server-side Aquari authentication operations for ASP.NET Core.
/// </summary>
public sealed class AquariAspNetCoreAuth
{
    private static readonly TimeSpan TransactionTtl =
        TimeSpan.FromMinutes(15);

    private static readonly TimeSpan AccessTokenRefreshSkew =
        TimeSpan.FromSeconds(30);

    private readonly AquariAspNetCoreOptions _options;
    private readonly AquariClient _client;
    private readonly IAquariSessionStore _store;
    private readonly IAquariRefreshLock _refreshLock;
    private readonly bool _secureCookies;
    private readonly string? _sessionCookieDomain;

    /// <summary>
    /// Initializes an ASP.NET Core adapter around an Aquari runtime client.
    /// </summary>
    public AquariAspNetCoreAuth(
        AquariAspNetCoreOptions options,
        AquariClient client)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(client);

        options.Validate();

        _options = options;
        _client = client;
        _store = options.SessionStore;
        _refreshLock =
            options.RefreshLock ??
            new InMemoryAquariRefreshLock();
        _secureCookies =
            options.ResolveSecureCookies();
        _sessionCookieDomain =
            options.ResolveSessionCookieDomain();
    }

    /// <summary>
    /// Begins Aquari authorization and redirects the response to hosted auth.
    /// </summary>
    public async Task SignInAsync(
        HttpContext context,
        string? returnTo = null,
        AquariAuthorizationPrompt? prompt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedReturnTo =
            AquariAspNetCoreState.NormalizeReturnTo(
                returnTo);

        var authorization =
            await _client.Auth.BeginAuthorizationAsync(
                prompt,
                cancellationToken).ConfigureAwait(false);

        var transactionId =
            AquariAspNetCoreState.CreateOpaqueId();

        var stored =
            new AquariStoredTransaction
            {
                Transaction =
                    authorization.Transaction,
                ReturnTo =
                    normalizedReturnTo,
            };

        await _store.SetAsync(
            AquariAspNetCoreState.TransactionKey(
                transactionId),
            AquariAspNetCoreState.Serialize(
                stored),
            DateTimeOffset.UtcNow +
            TransactionTtl,
            cancellationToken).ConfigureAwait(false);

        AquariAspNetCoreState.AppendCookie(
            context.Response,
            _options.TransactionCookieName,
            transactionId,
            TransactionTtl,
            _secureCookies);

        context.Response.Redirect(
            authorization.AuthorizationUri.AbsoluteUri);
    }

    /// <summary>
    /// Completes Aquari authorization and creates an opaque application session.
    /// </summary>
    public async Task CallbackAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Cookies.TryGetValue(
                _options.TransactionCookieName,
                out var transactionId) ||
            string.IsNullOrEmpty(
                transactionId))
        {
            throw new AquariOAuthException(
                "missing_transaction",
                "Aquari authorization transaction cookie is missing.");
        }

        var key =
            AquariAspNetCoreState.TransactionKey(
                transactionId);

        var stored =
            await AquariAspNetCoreState.ReadTransactionAsync(
                _store,
                key,
                cancellationToken).ConfigureAwait(false);

        if (stored is null)
        {
            throw new AquariOAuthException(
                "missing_transaction",
                "Aquari authorization transaction is missing or expired.");
        }

        AquariCompleteAuthorizationResult completed;

        try
        {
            completed =
                await _client.Auth.CompleteAuthorizationAsync(
                    AquariAspNetCoreState.BuildCallbackUri(
                        context.Request,
                        _options.RedirectUri),
                    stored.Transaction,
                    cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await _store.DeleteAsync(
                key,
                cancellationToken).ConfigureAwait(false);

            throw;
        }

        await _store.DeleteAsync(
            key,
            cancellationToken).ConfigureAwait(false);

        var sessionId =
            AquariAspNetCoreState.CreateOpaqueId();

        var session =
            new AquariStoredSession
            {
                Tokens = completed.Tokens,
                Identity = completed.Identity,
            };

        var ttl =
            completed.Tokens.RefreshToken is null
                ? Max(
                    TimeSpan.FromSeconds(1),
                    completed.Tokens.ExpiresAt -
                    DateTimeOffset.UtcNow)
                : _options.SessionTtl;

        await _store.SetAsync(
            AquariAspNetCoreState.SessionKey(
                sessionId),
            AquariAspNetCoreState.Serialize(
                session),
            DateTimeOffset.UtcNow +
            ttl,
            cancellationToken).ConfigureAwait(false);

        AquariAspNetCoreState.AppendCookie(
            context.Response,
            _options.SessionCookieName,
            sessionId,
            ttl,
            _secureCookies,
            _sessionCookieDomain);

        AquariAspNetCoreState.DeleteCookie(
            context.Response,
            _options.TransactionCookieName,
            _secureCookies);

        context.Response.Redirect(
            stored.ReturnTo);
    }

    /// <summary>
    /// Gets the current application session, refreshing it lazily when required.
    /// </summary>
    public async Task<AquariSession?> GetSessionAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var stored =
            await RequireSessionAsync(
                context,
                cancellationToken).ConfigureAwait(false);

        if (stored is null)
        {
            return null;
        }

        return new AquariSession
        {
            User =
                new AquariSessionUser
                {
                    Subject =
                        stored.Identity.Subject,
                    Handle =
                        stored.Identity.Handle,
                    Name =
                        stored.Identity.Name,
                    Email =
                        stored.Identity.Email,
                    EmailVerified =
                        stored.Identity.EmailVerified,
                    PictureUri =
                        stored.Identity.PictureUri,
                },
            Scopes =
                stored.Tokens.Scopes,
            ExpiresAt =
                stored.Tokens.ExpiresAt,
        };
    }

    /// <summary>
    /// Gets a usable access token, refreshing the server-side session when needed.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var stored =
            await RequireSessionAsync(
                context,
                cancellationToken).ConfigureAwait(false);

        return stored?.Tokens.AccessToken;
    }

    /// <summary>
    /// Deletes the local application session and expires its cookie.
    /// </summary>
    public async Task SignOutAsync(
        HttpContext context,
        string? returnTo = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedReturnTo =
            returnTo is null
                ? null
                : AquariAspNetCoreState.NormalizeReturnTo(
                    returnTo);

        if (context.Request.Cookies.TryGetValue(
                _options.SessionCookieName,
                out var sessionId) &&
            !string.IsNullOrEmpty(
                sessionId))
        {
            var key =
                AquariAspNetCoreState.SessionKey(
                    sessionId);

            await using var lease =
                await _refreshLock.AcquireAsync(
                    key,
                    cancellationToken).ConfigureAwait(false);

            await _store.DeleteAsync(
                key,
                cancellationToken).ConfigureAwait(false);
        }

        AquariAspNetCoreState.DeleteCookie(
            context.Response,
            _options.SessionCookieName,
            _secureCookies,
            _sessionCookieDomain);

        if (normalizedReturnTo is null)
        {
            context.Response.StatusCode =
                StatusCodes.Status204NoContent;

            return;
        }

        context.Response.Redirect(
            normalizedReturnTo);
    }

    private async Task<AquariStoredSession?> RequireSessionAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Cookies.TryGetValue(
                _options.SessionCookieName,
                out var sessionId) ||
            string.IsNullOrEmpty(
                sessionId))
        {
            return null;
        }

        var key =
            AquariAspNetCoreState.SessionKey(
                sessionId);

        var stored =
            await AquariAspNetCoreState.ReadSessionAsync(
                _store,
                key,
                cancellationToken).ConfigureAwait(false);

        if (stored is null)
        {
            return null;
        }

        if (stored.Tokens.ExpiresAt -
            DateTimeOffset.UtcNow >
            AccessTokenRefreshSkew)
        {
            return stored;
        }

        await using var lease =
            await _refreshLock.AcquireAsync(
                key,
                cancellationToken).ConfigureAwait(false);

        stored =
            await AquariAspNetCoreState.ReadSessionAsync(
                _store,
                key,
                cancellationToken).ConfigureAwait(false);

        if (stored is null)
        {
            return null;
        }

        if (stored.Tokens.ExpiresAt -
            DateTimeOffset.UtcNow >
            AccessTokenRefreshSkew)
        {
            return stored;
        }

        if (stored.Tokens.RefreshToken is null)
        {
            await _store.DeleteAsync(
                key,
                cancellationToken).ConfigureAwait(false);

            return null;
        }

        try
        {
            var refreshed =
                await _client.Auth.RefreshAsync(
                    stored.Tokens,
                    cancellationToken).ConfigureAwait(false);

            var replacement =
                new AquariStoredSession
                {
                    Tokens = refreshed,
                    Identity = stored.Identity,
                };

            await _store.SetAsync(
                key,
                AquariAspNetCoreState.Serialize(
                    replacement),
                DateTimeOffset.UtcNow +
                _options.SessionTtl,
                cancellationToken).ConfigureAwait(false);

            return replacement;
        }
        catch (AquariOAuthException exception)
            when (string.Equals(
                exception.Code,
                "invalid_grant",
                StringComparison.Ordinal))
        {
            await _store.DeleteAsync(
                key,
                cancellationToken).ConfigureAwait(false);

            return null;
        }
    }

    private static TimeSpan Max(
        TimeSpan first,
        TimeSpan second)
    {
        return first >= second
            ? first
            : second;
    }
}
