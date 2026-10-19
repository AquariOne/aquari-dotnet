using System.Net;
using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Session;
using Aquari.One.Configuration;
using Microsoft.AspNetCore.Http;

namespace Aquari.One.AspNetCore.Tests;

public sealed class AspNetCoreAdapterTests
{
    private const string Issuer =
        "https://dev.api.aquari.one/";

    [Fact]
    public async Task SignInCreatesOpaqueTransactionCookie()
    {
        var store =
            new InMemoryAquariSessionStore();

        using var handler =
            new AspNetCoreTestHttp(
                (request, _) =>
                {
                    Assert.Equal(
                        "/.well-known/openid-configuration",
                        request.RequestUri?.AbsolutePath);

                    return Task.FromResult(
                        Discovery());
                });

        using var httpClient =
            new HttpClient(handler);
        using var client =
            CreateClient(httpClient);

        var adapter =
            CreateAdapter(
                store,
                new InMemoryAquariRefreshLock(),
                client);

        var context =
            new DefaultHttpContext();

        context.Response.Body =
            new MemoryStream();

        await adapter.SignInAsync(
            context,
            "/after");

        Assert.Equal(
            StatusCodes.Status302Found,
            context.Response.StatusCode);
        Assert.NotEmpty(
            context.Response.Headers.Location.ToString());

        var setCookie =
            context.Response.Headers.SetCookie
                .ToString();

        Assert.Contains(
            "aquari_auth=",
            setCookie,
            StringComparison.Ordinal);
        Assert.Contains(
            "httponly",
            setCookie,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "samesite=lax",
            setCookie,
            StringComparison.OrdinalIgnoreCase);

        var transactionId =
            ExtractCookieValue(
                setCookie,
                "aquari_auth");

        Assert.Equal(
            32,
            transactionId.Length);
        Assert.All(
            transactionId,
            static character =>
                Assert.True(
                    Uri.IsHexDigit(
                        character)));

        Assert.NotNull(
            await store.GetAsync(
                $"aquari:transaction:{transactionId}"));
    }

    [Fact]
    public async Task EightConcurrentCallersUseOneRefreshExchange()
    {
        var store =
            new InMemoryAquariSessionStore();
        var refreshLock =
            new InMemoryAquariRefreshLock();

        await AspNetCoreTestState.SeedSessionAsync(
            store,
            "session-1",
            AspNetCoreTestState.NearExpiryTokens(),
            AspNetCoreTestState.Identity());

        var tokenCalls = 0;

        using var handler =
            new AspNetCoreTestHttp(
                async (request, cancellationToken) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return Discovery();
                    }

                    if (path == "/connect/token")
                    {
                        Interlocked.Increment(
                            ref tokenCalls);

                        var body =
                            await request.Content!
                                .ReadAsStringAsync(
                                    cancellationToken);

                        Assert.Contains(
                            "grant_type=refresh_token",
                            body,
                            StringComparison.Ordinal);

                        await Task.Delay(
                            25,
                            cancellationToken);

                        return AspNetCoreTestHttp.Json(
                            """
                            {
                              "token_type": "Bearer",
                              "access_token": "access-new",
                              "refresh_token": "refresh-new",
                              "expires_in": 3600
                            }
                            """);
                    }

                    throw new InvalidOperationException(
                        $"Unexpected request: {path}");
                });

        using var httpClient =
            new HttpClient(handler);
        using var client =
            CreateClient(httpClient);

        var adapter =
            CreateAdapter(
                store,
                refreshLock,
                client);

        var calls =
            Enumerable.Range(0, 8)
                .Select(
                    _ =>
                        adapter.GetAccessTokenAsync(
                            AspNetCoreTestState.ContextWithSession(
                                "session-1")))
                .ToArray();

        var results =
            await Task.WhenAll(calls);

        Assert.All(
            results,
            static value =>
                Assert.Equal(
                    "access-new",
                    value));

        Assert.Equal(
            1,
            tokenCalls);
    }

    [Fact]
    public async Task InvalidGrantDeletesSession()
    {
        var store =
            new InMemoryAquariSessionStore();

        await AspNetCoreTestState.SeedSessionAsync(
            store,
            "session-2",
            AspNetCoreTestState.NearExpiryTokens(),
            AspNetCoreTestState.Identity());

        using var handler =
            new AspNetCoreTestHttp(
                (request, _) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    return Task.FromResult(
                        path switch
                        {
                            "/.well-known/openid-configuration" =>
                                Discovery(),
                            "/connect/token" =>
                                AspNetCoreTestHttp.Json(
                                    """
                                    {
                                      "error": "invalid_grant",
                                      "error_description": "Refresh token is no longer valid."
                                    }
                                    """,
                                    HttpStatusCode.BadRequest),
                            _ =>
                                throw new InvalidOperationException(),
                        });
                });

        using var httpClient =
            new HttpClient(handler);
        using var client =
            CreateClient(httpClient);

        var adapter =
            CreateAdapter(
                store,
                new InMemoryAquariRefreshLock(),
                client);

        var session =
            await adapter.GetSessionAsync(
                AspNetCoreTestState.ContextWithSession(
                    "session-2"));

        Assert.Null(session);
        Assert.Null(
            await store.GetAsync(
                "aquari:session:session-2"));
    }

    [Fact]
    public async Task SignOutCannotBeUndoneByInflightRefresh()
    {
        var store =
            new InMemoryAquariSessionStore();
        var refreshLock =
            new InMemoryAquariRefreshLock();

        await AspNetCoreTestState.SeedSessionAsync(
            store,
            "session-3",
            AspNetCoreTestState.NearExpiryTokens(),
            AspNetCoreTestState.Identity());

        var refreshStarted =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        using var handler =
            new AspNetCoreTestHttp(
                async (request, cancellationToken) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return Discovery();
                    }

                    if (path == "/connect/token")
                    {
                        refreshStarted.TrySetResult(true);

                        await releaseRefresh.Task.WaitAsync(
                            cancellationToken);

                        return AspNetCoreTestHttp.Json(
                            """
                            {
                              "token_type": "Bearer",
                              "access_token": "access-new",
                              "refresh_token": "refresh-new",
                              "expires_in": 3600
                            }
                            """);
                    }

                    throw new InvalidOperationException();
                });

        using var httpClient =
            new HttpClient(handler);
        using var client =
            CreateClient(httpClient);

        var adapter =
            CreateAdapter(
                store,
                refreshLock,
                client);

        var refreshTask =
            adapter.GetAccessTokenAsync(
                AspNetCoreTestState.ContextWithSession(
                    "session-3"));

        await refreshStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        var signOutContext =
            AspNetCoreTestState.ContextWithSession(
                "session-3");

        var signOutTask =
            adapter.SignOutAsync(
                signOutContext);

        await Task.Delay(25);

        Assert.False(
            signOutTask.IsCompleted);

        releaseRefresh.TrySetResult(true);

        Assert.Equal(
            "access-new",
            await refreshTask);

        await signOutTask;

        Assert.Null(
            await store.GetAsync(
                "aquari:session:session-3"));
    }

    [Fact]
    public async Task SharedLockCoordinatesSeparateAdapterInstances()
    {
        var store =
            new InMemoryAquariSessionStore();
        var refreshLock =
            new InMemoryAquariRefreshLock();

        await AspNetCoreTestState.SeedSessionAsync(
            store,
            "session-4",
            AspNetCoreTestState.NearExpiryTokens(),
            AspNetCoreTestState.Identity());

        var tokenCalls = 0;

        using var handler =
            new AspNetCoreTestHttp(
                async (request, cancellationToken) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return Discovery();
                    }

                    if (path == "/connect/token")
                    {
                        Interlocked.Increment(
                            ref tokenCalls);

                        await Task.Delay(
                            25,
                            cancellationToken);

                        return AspNetCoreTestHttp.Json(
                            """
                            {
                              "token_type": "Bearer",
                              "access_token": "access-shared",
                              "refresh_token": "refresh-shared",
                              "expires_in": 3600
                            }
                            """);
                    }

                    throw new InvalidOperationException();
                });

        using var httpClientOne =
            new HttpClient(
                handler,
                disposeHandler: false);
        using var httpClientTwo =
            new HttpClient(
                handler,
                disposeHandler: false);
        using var clientOne =
            CreateClient(httpClientOne);
        using var clientTwo =
            CreateClient(httpClientTwo);

        var first =
            CreateAdapter(
                store,
                refreshLock,
                clientOne);

        var second =
            CreateAdapter(
                store,
                refreshLock,
                clientTwo);

        var results =
            await Task.WhenAll(
                first.GetAccessTokenAsync(
                    AspNetCoreTestState.ContextWithSession(
                        "session-4")),
                second.GetAccessTokenAsync(
                    AspNetCoreTestState.ContextWithSession(
                        "session-4")));

        Assert.All(
            results,
            static value =>
                Assert.Equal(
                    "access-shared",
                    value));

        Assert.Equal(
            1,
            tokenCalls);
    }

    private static AquariClient CreateClient(
        HttpClient httpClient)
    {
        return new AquariClient(
            new AquariClientOptions
            {
                Issuer = new Uri(Issuer),
                ClientId = "public-client",
                RedirectUri =
                    new Uri(
                        "http://localhost:4200/auth/callback"),
                Scopes =
                    [
                        "openid",
                        "profile",
                        "email",
                        "offline_access",
                        "passport.read",
                    ],
                MaxRetries = 0,
            },
            httpClient);
    }

    private static AquariAspNetCoreAuth CreateAdapter(
        IAquariSessionStore store,
        IAquariRefreshLock refreshLock,
        AquariClient client)
    {
        return new AquariAspNetCoreAuth(
            new AquariAspNetCoreOptions
            {
                Issuer = new Uri(Issuer),
                ClientId = "public-client",
                RedirectUri =
                    new Uri(
                        "http://localhost:4200/auth/callback"),
                Scopes =
                    [
                        "openid",
                        "profile",
                        "email",
                        "offline_access",
                        "passport.read",
                    ],
                SessionStore = store,
                RefreshLock = refreshLock,
                SecureCookies = false,
                MaxRetries = 0,
            },
            client);
    }

    private static HttpResponseMessage Discovery()
    {
        return AspNetCoreTestHttp.Json(
            """
            {
              "issuer": "https://dev.api.aquari.one/",
              "authorization_endpoint": "https://dev.api.aquari.one/connect/authorize",
              "token_endpoint": "https://dev.api.aquari.one/connect/token",
              "userinfo_endpoint": "https://dev.api.aquari.one/connect/userinfo",
              "jwks_uri": "https://dev.api.aquari.one/.well-known/jwks",
              "grant_types_supported": ["authorization_code", "refresh_token"],
              "response_types_supported": ["code"],
              "code_challenge_methods_supported": ["S256"],
              "id_token_signing_alg_values_supported": ["RS256"],
              "token_endpoint_auth_methods_supported": ["none", "client_secret_post"],
              "authorization_response_iss_parameter_supported": true
            }
            """);
    }

    private static string ExtractCookieValue(
        string header,
        string name)
    {
        var prefix =
            $"{name}=";

        var start =
            header.IndexOf(
                prefix,
                StringComparison.Ordinal);

        Assert.True(start >= 0);

        start += prefix.Length;

        var end =
            header.IndexOf(
                ';',
                start);

        return end < 0
            ? header[start..]
            : header[start..end];
    }
}
