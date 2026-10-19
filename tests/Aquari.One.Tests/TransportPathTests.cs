using System.Net;
using System.Text;
using System.Text.Json;
using Aquari.One.Transport;

namespace Aquari.One.Tests;

public sealed class TransportPathTests
{
    [Fact]
    public async Task RootRelativePathResolvesAgainstConfiguredOrigin()
    {
        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    Assert.Equal(
                        "https://dev.api.aquari.one/.well-known/openid-configuration",
                        request.RequestUri?.AbsoluteUri);

                    return Task.FromResult(
                        JsonResponse());
                });

        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/base/"));

        using var document =
            await transport.GetJsonAsync(
                "/.well-known/openid-configuration");

        Assert.Equal(
            JsonValueKind.Object,
            document.RootElement.ValueKind);
        Assert.Equal(
            1,
            handler.Calls);
    }

    [Fact]
    public async Task RelativePathPreservesConfiguredBasePath()
    {
        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    Assert.Equal(
                        "https://dev.api.aquari.one/base/api/v1/passport",
                        request.RequestUri?.AbsoluteUri);

                    return Task.FromResult(
                        JsonResponse());
                });

        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/base/"));

        using var document =
            await transport.GetJsonAsync(
                "api/v1/passport");

        Assert.Equal(
            JsonValueKind.Object,
            document.RootElement.ValueKind);
        Assert.Equal(
            1,
            handler.Calls);
    }

    [Fact]
    public async Task AbsoluteRequestUriIsRejectedBeforeNetworkUse()
    {
        using var handler =
            NeverCalledHandler();
        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/"));

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () =>
                transport.GetJsonAsync(
                    "https://evil.example/steal"));

        Assert.Equal(
            0,
            handler.Calls);
    }

    [Fact]
    public async Task NetworkPathReferenceIsRejectedBeforeNetworkUse()
    {
        using var handler =
            NeverCalledHandler();
        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/"));

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () =>
                transport.GetJsonAsync(
                    "//evil.example/steal"));

        Assert.Equal(
            0,
            handler.Calls);
    }

    [Fact]
    public async Task BackslashPathIsRejectedBeforeNetworkUse()
    {
        using var handler =
            NeverCalledHandler();
        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/"));

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () =>
                transport.GetJsonAsync(
                    @"\evil.example\steal"));

        Assert.Equal(
            0,
            handler.Calls);
    }

    [Fact]
    public async Task FragmentIsRejectedBeforeNetworkUse()
    {
        using var handler =
            NeverCalledHandler();
        using var client =
            new HttpClient(handler);

        var transport =
            CreateTransport(
                client,
                new Uri(
                    "https://dev.api.aquari.one/"));

        await Assert.ThrowsAsync<AquariConfigurationException>(
            () =>
                transport.GetJsonAsync(
                    "/api/v1/passport#not-sent"));

        Assert.Equal(
            0,
            handler.Calls);
    }

    private static AquariHttpTransport CreateTransport(
        HttpClient client,
        Uri baseUri)
    {
        return new AquariHttpTransport(
            client,
            baseUri,
            TimeSpan.FromSeconds(1),
            maxRetries: 0,
            TimeSpan.Zero);
    }

    private static StubHttpMessageHandler NeverCalledHandler()
    {
        return new StubHttpMessageHandler(
            (_, _) =>
                throw new InvalidOperationException(
                    "HTTP handler must not be called."));
    }

    private static HttpResponseMessage JsonResponse()
    {
        return new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    "{}",
                    Encoding.UTF8,
                    "application/json"),
        };
    }
}
