using System.Collections.Concurrent;

namespace Aquari.One.AspNetCore.Session;

/// <summary>
/// Provides a process-local Aquari session store for development and tests.
/// </summary>
/// <remarks>
/// Production multi-instance applications must use a shared durable implementation.
/// </remarks>
public sealed class InMemoryAquariSessionStore : IAquariSessionStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<string?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (!_entries.TryGetValue(
                key,
                out var entry))
        {
            return Task.FromResult<string?>(null);
        }

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(
                key,
                out _);

            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(
            entry.Value);
    }

    /// <inheritdoc />
    public Task SetAsync(
        string key,
        string value,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Aquari session-store expiration must be in the future.");
        }

        _entries[key] =
            new Entry(
                value,
                expiresAt);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);

        _entries.TryRemove(
            key,
            out _);

        return Task.CompletedTask;
    }

    private sealed record Entry(
        string Value,
        DateTimeOffset ExpiresAt);
}
