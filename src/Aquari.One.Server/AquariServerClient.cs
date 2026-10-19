using Aquari.One.Internal;
using Aquari.One.Server.Configuration;
using Aquari.One.Server.Internal;
using Aquari.One.Server.Issuer;
using Aquari.One.Server.Machine;
using Aquari.One.Transport;

namespace Aquari.One.Server;

/// <summary>Provides trusted-server Aquari operations.</summary>
public sealed class AquariServerClient : IDisposable
{
    private readonly HttpClient? _ownedHttpClient;
    private bool _disposed;

    /// <summary>Initializes a server client with an SDK-owned HTTP client.</summary>
    public AquariServerClient(
        AquariServerClientOptions options)
        : this(options, null)
    {
    }

    /// <summary>Initializes a server client with a caller-owned HTTP client.</summary>
    public AquariServerClient(
        AquariServerClientOptions options,
        HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (httpClient is null)
        {
            _ownedHttpClient =
                new HttpClient
                {
                    Timeout = Timeout.InfiniteTimeSpan,
                };

            httpClient = _ownedHttpClient;
        }

        var apiUrl =
            AquariUriPolicy.NormalizeBaseUri(
                options.ApiUrl,
                "API URL");

        var transport =
            new AquariHttpTransport(
                httpClient,
                apiUrl,
                options.Timeout,
                options.MaxRetries,
                options.MaxRetryDelay);

        var headers =
            AquariServerWire.Bearer(options.ApiKey);

        Machine =
            new AquariMachineClient(
                transport,
                headers);

        Issuer =
            new AquariIssuerClient(
                transport,
                headers);
    }

    /// <summary>Gets machine context/health operations.</summary>
    public AquariMachineClient Machine { get; }

    /// <summary>Gets the five supported issuer families.</summary>
    public AquariIssuerClient Issuer { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ownedHttpClient?.Dispose();
        _disposed = true;
    }
}
