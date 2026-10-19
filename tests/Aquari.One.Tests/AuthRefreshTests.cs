using System.Net;
using System.Text;
using System.Text.Json;
using Aquari.One.Auth;
using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class AuthRefreshTests
{
    private const string Issuer =
        "https://dev.api.aquari.one/";

    private const string Subject =
        "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task RefreshPreservesSubjectAndAcceptsRotatedRefreshToken()
    {
        using var signing =
            new JwtTestMaterial();

        var replacement =
            signing.CreateIdToken(
                Issuer,
                "public-client",
                Subject,
                "unused");

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

                    if (path == "/connect/token")
                    {
                        var body =
                            await request.Content!
                                .ReadAsStringAsync(
                                    cancellationToken);

                        Assert.Contains(
                            "grant_type=refresh_token",
                            body,
                            StringComparison.Ordinal);
                        Assert.Contains(
                            "refresh_token=refresh-old",
                            body,
                            StringComparison.Ordinal);

                        return Json(
                            new
                            {
                                token_type = "Bearer",
                                access_token = "access-new",
                                id_token = replacement,
                                refresh_token = "refresh-new",
                                expires_in = 3600,
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
            new AquariClient(
                new AquariClientOptions
                {
                    Issuer = new Uri(Issuer),
                    ClientId = "public-client",
                    RedirectUri =
                        new Uri(
                            "https://app.example/auth/callback"),
                    Scopes =
                        ["openid", "offline_access"],
                    MaxRetries = 0,
                },
                new HttpClient(handler));

        var refreshed =
            await client.Auth.RefreshAsync(
                new AquariTokenSet
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
                });

        Assert.Equal(
            Subject,
            refreshed.Subject);
        Assert.Equal(
            "access-new",
            refreshed.AccessToken);
        Assert.Equal(
            "refresh-new",
            refreshed.RefreshToken);
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
