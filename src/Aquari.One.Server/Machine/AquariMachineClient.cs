using Aquari.One.Server.Internal;
using Aquari.One.Transport;

namespace Aquari.One.Server.Machine;

/// <summary>Provides authenticated machine context and health operations.</summary>
public sealed class AquariMachineClient
{
    private readonly AquariHttpTransport _transport;
    private readonly IReadOnlyDictionary<string, string> _headers;

    internal AquariMachineClient(
        AquariHttpTransport transport,
        IReadOnlyDictionary<string, string> headers)
    {
        _transport = transport;
        _headers = headers;
    }

    /// <summary>Loads the authenticated machine context.</summary>
    public async Task<AquariMachineContext> ContextAsync(
        CancellationToken cancellationToken = default)
    {
        using var document =
            await _transport.GetJsonAsync(
                "/api/v1/machine/context",
                _headers,
                cancellationToken).ConfigureAwait(false);

        return AquariServerWire.Deserialize<AquariMachineContext>(
            document.RootElement,
            "machine context");
    }

    /// <summary>Loads machine health.</summary>
    public async Task<AquariMachineHealth> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var document =
            await _transport.GetJsonAsync(
                "/api/v1/machine/health",
                _headers,
                cancellationToken).ConfigureAwait(false);

        return AquariServerWire.Deserialize<AquariMachineHealth>(
            document.RootElement,
            "machine health");
    }
}
