using System.Net;
using Microsoft.AspNetCore.Http;

namespace Aquari.One.AspNetCore.Tests;

internal sealed class AspNetCoreTestHttp : HttpMessageHandler
{
    private readonly Func<
        HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>> _handler;

    internal AspNetCoreTestHttp(
        Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    internal int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;

        return _handler(
            request,
            cancellationToken);
    }

    internal static HttpResponseMessage Json(
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content =
                new StringContent(
                    body,
                    System.Text.Encoding.UTF8,
                    "application/json"),
        };
    }
}
