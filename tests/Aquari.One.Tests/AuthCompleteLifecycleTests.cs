using System.Net;
using System.Text;
using System.Text.Json;
using Aquari.One.Auth;
using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class AuthCompleteLifecycleTests
{
    private const string Issuer =
        "https://dev.api.aquari.one/";

    private const string Subject =
        "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task PublicClientCompletesWithoutClientSecret()
    {
        using var signing =
            new JwtTestMaterial();

        string? idToken = null;
        string? tokenBody = null;

        using var handler =
            new StubHttpMessageHandler(
                async (request, cancellationToken) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return TestDiscoveryDocument.Success();
                    }

                    if (path ==
                        "/connect/token")
                    {
                        tokenBody =
                            await request.Content!
                                .ReadAsStringAsync(cancellationToken);

                        return Json(
                            new
                            {
                                token_type = "Bearer",
                                access_token = "access",
                                id_token = idToken,
                                refresh_token = "refresh-1",
                                expires_in = 3600,
                                scope =
                                    "openid profile offline_access",
                            });
                    }

                    if (path == "/.well-known/jwks")
                    {
                        return RawJson(signing.JwksJson);
                    }

                    throw new InvalidOperationException(
                        $"Unexpected path: {path}");
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var begin =
            await client.Auth.BeginAuthorizationAsync();

        idToken =
            signing.CreateIdToken(
                Issuer,
                "public-client",
                Subject,
                begin.Transaction.Nonce);

        var completed =
            await client.Auth.CompleteAuthorizationAsync(
                Callback(begin.Transaction),
                begin.Transaction);

        Assert.Equal(
            Subject,
            completed.Identity.Subject);
        Assert.Equal(
            "access",
            completed.Tokens.AccessToken);
        Assert.Equal(
            "refresh-1",
            completed.Tokens.RefreshToken);

        Assert.NotNull(tokenBody);
        Assert.Contains(
            "code_verifier=",
            tokenBody,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "client_secret=",
            tokenBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongNonceFailsClosed()
    {
        using var signing =
            new JwtTestMaterial();

        string? idToken = null;

        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return Task.FromResult(
                            TestDiscoveryDocument.Success());
                    }

                    if (path == "/connect/token")
                    {
                        return Task.FromResult(
                            Json(
                                new
                                {
                                    token_type = "Bearer",
                                    access_token = "access",
                                    id_token = idToken,
                                    expires_in = 3600,
                                }));
                    }

                    if (path == "/.well-known/jwks")
                    {
                        return Task.FromResult(
                            RawJson(signing.JwksJson));
                    }

                    throw new InvalidOperationException();
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var begin =
            await client.Auth.BeginAuthorizationAsync();

        idToken =
            signing.CreateIdToken(
                Issuer,
                "public-client",
                Subject,
                "wrong-nonce");

        var error =
            await Assert.ThrowsAsync<AquariOAuthException>(
                () =>
                    client.Auth.CompleteAuthorizationAsync(
                        Callback(begin.Transaction),
                        begin.Transaction));

        Assert.Equal(
            "invalid_id_token",
            error.Code);
    }

    [Fact]
    public async Task UserInfoSubjectMismatchIsRejected()
    {
        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    var path =
                        request.RequestUri?.AbsolutePath;

                    if (path ==
                        "/.well-known/openid-configuration")
                    {
                        return Task.FromResult(
                            TestDiscoveryDocument.Success());
                    }

                    if (path == "/connect/userinfo")
                    {
                        return Task.FromResult(
                            Json(
                                new
                                {
                                    sub =
                                        "22222222-2222-2222-2222-222222222222",
                                }));
                    }

                    throw new InvalidOperationException();
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var error =
            await Assert.ThrowsAsync<AquariOAuthException>(
                () =>
                    client.Auth.UserInfoAsync(
                        TokenSet()));

        Assert.Equal(
            "userinfo_subject_mismatch",
            error.Code);
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
                        "https://app.example/auth/callback"),
                Scopes =
                    ["openid", "profile", "offline_access"],
                MaxRetries = 0,
            },
            httpClient);
    }

    private static AquariTokenSet TokenSet()
    {
        return new AquariTokenSet
        {
            Subject = Subject,
            AccessToken = "access-old",
            IdToken = "id-old",
            RefreshToken = "refresh-old",
            ExpiresIn = TimeSpan.FromHours(1),
            ExpiresAt =
                DateTimeOffset.UtcNow.AddHours(1),
            Scopes =
                ["openid", "offline_access"],
        };
    }

    private static Uri Callback(
        AquariAuthorizationTransaction transaction)
    {
        return new Uri(
            $"{transaction.RedirectUri.AbsoluteUri}?code=authorization-code&state={transaction.State}&iss={Uri.EscapeDataString(transaction.Issuer.AbsoluteUri)}");
    }

    private static HttpResponseMessage Json(
        object value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    JsonSerializer.Serialize(value),
                    Encoding.UTF8,
                    "application/json"),
        };
    }

    private static HttpResponseMessage RawJson(
        string value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    value,
                    Encoding.UTF8,
                    "application/json"),
        };
    }
}
