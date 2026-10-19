namespace Aquari.One.AspNetCore.Concurrency;

/// <summary>
/// Provides a process-local keyed refresh lock.
/// </summary>
/// <remarks>
/// Multi-instance production deployments must supply a distributed/shared lock.
/// </remarks>
public sealed class InMemoryAquariRefreshLock : IAquariRefreshLock
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Entry> _entries =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> AcquireAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        Entry entry;

        lock (_sync)
        {
            if (_entries.TryGetValue(
                    key,
                    out var existing))
            {
                entry = existing;
            }
            else
            {
                entry = new Entry();
                _entries.Add(
                    key,
                    entry);
            }

            entry.References++;
        }

        try
        {
            await entry.Gate.WaitAsync(
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ReleaseReference(
                key,
                entry,
                releaseGate: false);

            throw;
        }

        return new Releaser(
            this,
            key,
            entry);
    }

    private void ReleaseReference(
        string key,
        Entry entry,
        bool releaseGate)
    {
        if (releaseGate)
        {
            entry.Gate.Release();
        }

        lock (_sync)
        {
            entry.References--;

            if (entry.References == 0 &&
                _entries.TryGetValue(
                    key,
                    out var current) &&
                ReferenceEquals(
                    current,
                    entry))
            {
                _entries.Remove(key);
                entry.Gate.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        internal SemaphoreSlim Gate { get; } =
            new(1, 1);

        internal int References { get; set; }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private InMemoryAquariRefreshLock? _owner;
        private readonly string _key;
        private readonly Entry _entry;

        internal Releaser(
            InMemoryAquariRefreshLock owner,
            string key,
            Entry entry)
        {
            _owner = owner;
            _key = key;
            _entry = entry;
        }

        public ValueTask DisposeAsync()
        {
            var owner =
                Interlocked.Exchange(
                    ref _owner,
                    null);

            owner?.ReleaseReference(
                _key,
                _entry,
                releaseGate: true);

            return ValueTask.CompletedTask;
        }
    }
}
