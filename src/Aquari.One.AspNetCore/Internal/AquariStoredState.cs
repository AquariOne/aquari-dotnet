using Aquari.One.Auth;

namespace Aquari.One.AspNetCore.Internal;

internal sealed class AquariStoredTransaction
{
    internal const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public required AquariAuthorizationTransaction Transaction { get; init; }

    public required string ReturnTo { get; init; }
}

internal sealed class AquariStoredSession
{
    internal const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public required AquariTokenSet Tokens { get; init; }

    public required AquariIdentity Identity { get; init; }
}
