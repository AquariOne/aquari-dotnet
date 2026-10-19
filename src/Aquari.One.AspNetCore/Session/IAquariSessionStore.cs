namespace Aquari.One.AspNetCore.Session;

/// <summary>
/// Stores opaque Aquari authorization and application-session state.
/// </summary>
public interface IAquariSessionStore
{
    /// <summary>Gets a stored value, or null when absent or expired.</summary>
    Task<string?> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Stores a value until the specified UTC expiration.</summary>
    Task SetAsync(
        string key,
        string value,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored value.</summary>
    Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);
}
