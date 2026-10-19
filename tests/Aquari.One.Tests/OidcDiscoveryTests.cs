using System.Net;
using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class OidcDiscoveryTests
{
    private static readonly string[] PlainChallengeMethods =
        ["plain"];

    [Fact]
    public async Task DiscoveryIsValidatedAndCached()
    {
        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    Assert.Equal(
                        "https://dev.api.aquari.one/.well-known/openid-configuration",
                        request.RequestUri?.AbsoluteUri);

                    return Task.FromResult(
                        TestDiscoveryDocument.Success());
                });

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(httpClient);

        var first =
            await client.Auth.DiscoverAsync();
        var second =
            await client.Auth.DiscoverAsync();

        Assert.Same(first, second);
        Assert.Equal(1, handler.Calls);
        Assert.Contains(
            "S256",
            first.CodeChallengeMethodsSupported);
    }

    [Fact]
    public async Task ForceRefreshReloadsDiscovery()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success()));

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(httpClient);

        _ = await client.Auth.DiscoverAsync();
        _ = await client.Auth.DiscoverAsync(
            forceRefresh: true);

        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task IssuerMismatchIsRejected()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success(
                            new
                            {
                                issuer =
                                    "https://other.example/",
                            })));

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(httpClient);

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () => client.Auth.DiscoverAsync());
    }

    [Fact]
    public async Task DiscoveryWithoutS256IsRejected()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success(
                            new
                            {
                                code_challenge_methods_supported =
                                    PlainChallengeMethods,
                            })));

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(httpClient);

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () => client.Auth.DiscoverAsync());
    }

    [Fact]
    public async Task InsecureRemoteDiscoveryEndpointIsRejected()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Success(
                            new
                            {
                                token_endpoint =
                                    "http://example.com/connect/token",
                            })));

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(httpClient);

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () => client.Auth.DiscoverAsync());
    }

    [Fact]
    public async Task RetryableDiscoveryResponseIsRetried()
    {
        var call = 0;

        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                {
                    call++;

                    if (call == 1)
                    {
                        var response =
                            TestDiscoveryDocument.Json(
                                HttpStatusCode.ServiceUnavailable,
                                new
                                {
                                    error = new
                                    {
                                        code = "Unavailable",
                                        message = "Try again.",
                                    },
                                });

                        response.Headers.RetryAfter =
                            new System.Net.Http.Headers.RetryConditionHeaderValue(
                                TimeSpan.Zero);

                        return Task.FromResult(response);
                    }

                    return Task.FromResult(
                        TestDiscoveryDocument.Success());
                });

        using var httpClient =
            new HttpClient(handler);

        using var client =
            CreateClient(
                httpClient,
                maxRetries: 1);

        var discovery =
            await client.Auth.DiscoverAsync();

        Assert.Equal(
            "https://dev.api.aquari.one/",
            discovery.Issuer.AbsoluteUri);
        Assert.Equal(2, handler.Calls);
    }

    private static AquariClient CreateClient(
        HttpClient httpClient,
        int maxRetries = 0)
    {
        return new AquariClient(
            new AquariClientOptions
            {
                Issuer =
                    new Uri("https://dev.api.aquari.one/"),
                ClientId = "test-client",
                RedirectUri =
                    new Uri("https://app.example/auth/callback"),
                Scopes =
                    ["openid", "profile", "offline_access"],
                MaxRetries = maxRetries,
            },
            httpClient);
    }
}
