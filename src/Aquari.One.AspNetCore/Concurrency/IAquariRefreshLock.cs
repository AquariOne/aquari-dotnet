namespace Aquari.One.AspNetCore.Concurrency;

/// <summary>
/// Serializes refresh and sign-out work for one logical Aquari session.
/// </summary>
public interface IAquariRefreshLock
{
    /// <summary>Acquires exclusive access for the supplied session key.</summary>
    ValueTask<IAsyncDisposable> AcquireAsync(
        string key,
        CancellationToken cancellationToken = default);
}
