using System.Net;
using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class ErrorHierarchyTests
{
    private static readonly string[] DiscoveryBucket =
        ["discovery"];

    private static readonly string[] RequiredValidationMessage =
        ["Required."];

    [Fact]
    public async Task RateLimitResponseMapsToRateLimitException()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                {
                    var response =
                        TestDiscoveryDocument.Json(
                            HttpStatusCode.TooManyRequests,
                            new
                            {
                                error = new
                                {
                                    code = "RateLimitExceeded",
                                    message = "Slow down.",
                                    details = new
                                    {
                                        bucket =
                                            DiscoveryBucket,
                                    },
                                },
                            });

                    response.Headers.RetryAfter =
                        new System.Net.Http.Headers.RetryConditionHeaderValue(
                            TimeSpan.FromSeconds(7));

                    return Task.FromResult(response);
                });

        using var client =
            CreateClient(
                new HttpClient(handler));

        var error =
            await Assert.ThrowsAsync<AquariRateLimitException>(
                () => client.Auth.DiscoverAsync());

        Assert.Equal(429, error.StatusCode);
        Assert.Equal(
            "RateLimitExceeded",
            error.Code);
        Assert.Equal(
            TimeSpan.FromSeconds(7),
            error.RetryAfter);
    }

    [Fact]
    public async Task ValidationResponseMapsStructuredDetails()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        TestDiscoveryDocument.Json(
                            HttpStatusCode.BadRequest,
                            new
                            {
                                error = new
                                {
                                    code = "ValidationFailed",
                                    message = "Invalid.",
                                    details = new
                                    {
                                        clientId =
                                            RequiredValidationMessage,
                                    },
                                },
                            })));

        using var client =
            CreateClient(
                new HttpClient(handler));

        var error =
            await Assert.ThrowsAsync<AquariValidationException>(
                () => client.Auth.DiscoverAsync());

        Assert.Equal(
            "Required.",
            Assert.Single(
                error.Fields["clientId"]));
    }

    [Fact]
    public async Task CallerCancellationIsDistinctFromTimeout()
    {
        using var handler =
            new StubHttpMessageHandler(
                async (_, cancellationToken) =>
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);

                    throw new InvalidOperationException(
                        "Unreachable.");
                });

        using var client =
            CreateClient(
                new HttpClient(handler),
                timeout:
                    TimeSpan.FromSeconds(30));

        using var cancellationSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(20));

        var error =
            await Assert.ThrowsAsync<AquariTransportException>(
                () => client.Auth.DiscoverAsync(
                    cancellationToken:
                        cancellationSource.Token));

        Assert.True(error.IsCancelled);
        Assert.IsNotType<AquariTimeoutException>(error);
    }

    [Fact]
    public async Task RequestTimeoutMapsToTimeoutException()
    {
        using var handler =
            new StubHttpMessageHandler(
                async (_, cancellationToken) =>
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);

                    throw new InvalidOperationException(
                        "Unreachable.");
                });

        using var client =
            CreateClient(
                new HttpClient(handler),
                timeout:
                    TimeSpan.FromMilliseconds(20));

        var error =
            await Assert.ThrowsAsync<AquariTimeoutException>(
                () => client.Auth.DiscoverAsync());

        Assert.Equal(
            TimeSpan.FromMilliseconds(20),
            error.Timeout);
    }

    [Fact]
    public void QuotaExceptionParsesTypedDetails()
    {
        var details =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["resource"] = new[] { "resource.application" },
                ["current"] = new[] { "4" },
                ["limit"] = new[] { "5" },
                ["periodFrom"] =
                    new[] { "2026-09-01T00:00:00Z" },
            };

        var error =
            new AquariQuotaException(
                429,
                "QuotaExceeded",
                "Quota reached.",
                details);

        Assert.Equal(
            "resource.application",
            error.Resource);
        Assert.Equal(4, error.Current);
        Assert.Equal(5, error.Limit);
        Assert.NotNull(error.PeriodFrom);
    }

    private static AquariClient CreateClient(
        HttpClient httpClient,
        TimeSpan? timeout = null)
    {
        return new AquariClient(
            new AquariClientOptions
            {
                Issuer =
                    new Uri("https://dev.api.aquari.one/"),
                ClientId = "test-client",
                RedirectUri =
                    new Uri("https://app.example/auth/callback"),
                Scopes = ["openid"],
                Timeout =
                    timeout ??
                    TimeSpan.FromSeconds(1),
                MaxRetries = 0,
            },
            httpClient);
    }
}
