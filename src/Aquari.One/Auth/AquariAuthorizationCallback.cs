namespace Aquari.One.Auth;

internal sealed class AquariAuthorizationCallback
{
    internal required string Code { get; init; }

    internal required string State { get; init; }

    internal string? Issuer { get; init; }
}
