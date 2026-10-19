using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Session;

namespace Aquari.One.AspNetCore.Tests;

public sealed class SessionInfrastructureTests
{
    [Fact]
    public async Task InMemoryStoreExpiresValues()
    {
        var store =
            new InMemoryAquariSessionStore();

        await store.SetAsync(
            "key",
            "value",
            DateTimeOffset.UtcNow.AddMilliseconds(50));

        Assert.Equal(
            "value",
            await store.GetAsync("key"));

        await Task.Delay(100);

        Assert.Null(
            await store.GetAsync("key"));
    }

    [Fact]
    public async Task InMemoryRefreshLockSerializesCallers()
    {
        var refreshLock =
            new InMemoryAquariRefreshLock();

        var active = 0;
        var peak = 0;

        var tasks =
            Enumerable.Range(0, 8)
                .Select(
                    async _ =>
                    {
                        await using var lease =
                            await refreshLock.AcquireAsync(
                                "session");

                        var current =
                            Interlocked.Increment(
                                ref active);

                        UpdatePeak(
                            ref peak,
                            current);

                        await Task.Delay(10);

                        Interlocked.Decrement(
                            ref active);
                    })
                .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(
            1,
            peak);
    }

    private static void UpdatePeak(
        ref int peak,
        int current)
    {
        while (true)
        {
            var observed =
                Volatile.Read(ref peak);

            if (current <= observed)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref peak,
                    current,
                    observed) ==
                observed)
            {
                return;
            }
        }
    }
}
