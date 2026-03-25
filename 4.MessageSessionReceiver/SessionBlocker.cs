using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

public sealed class SessionBlocker : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly string _queueOrTopicName;
    private readonly string? _subscriptionName;
    private readonly ConcurrentDictionary<string, BlockEntry> _entries = new();
    private readonly CancellationTokenSource _disposeCts = new();

    public SessionBlocker(
        ServiceBusClient client,
        string queueOrTopicName,
        string? subscriptionName = null)
    {
        _client = client;
        _queueOrTopicName = queueOrTopicName;
        _subscriptionName = subscriptionName;
    }

    public void BlockUntil(string sessionId, DateTimeOffset lockUntil)
    {
        if (lockUntil <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(sessionId, out _);
            return;
        }

        _entries.AddOrUpdate(
            sessionId,
            _ => BlockEntry.Start(sessionId, lockUntil, RunBlockLoopAsync),
            (_, existing) =>
            {
                existing.Extend(lockUntil);
                return existing;
            });

        // local function to capture this instance cleanly
        async Task RunBlockLoopAsync(string sid, CancellationToken ct)
        {
            await HoldSessionUntilAsync(sid, ct).ConfigureAwait(false);
        }
    }

    private async Task HoldSessionUntilAsync(string sessionId, CancellationToken callerToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            callerToken, _disposeCts.Token);

        var ct = linkedCts.Token;
        ServiceBusSessionReceiver? receiver = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!_entries.TryGetValue(sessionId, out var entry))
                {
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                if (entry.LockUntil <= now)
                {
                    _entries.TryRemove(sessionId, out _);
                    return;
                }

                if (receiver is null)
                {
                    try
                    {
                        receiver = await AcceptSessionAsync(sessionId, ct).ConfigureAwait(false);
                    }
                    catch (ServiceBusException ex) when (
                        ex.Reason == ServiceBusFailureReason.SessionCannotBeLocked)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                        continue;
                    }
                }

                // We have the session lock now. Keep renewing until the deadline.
                while (!ct.IsCancellationRequested)
                {
                    if (!_entries.TryGetValue(sessionId, out entry))
                    {
                        return;
                    }

                    now = DateTimeOffset.UtcNow;
                    if (entry.LockUntil <= now)
                    {
                        _entries.TryRemove(sessionId, out _);
                        return;
                    }

                    // Renew somewhat periodically. You can tune this.
                    var delay = TimeSpan.FromSeconds(20);
                    var remaining = entry.LockUntil - now;
                    if (remaining < delay)
                    {
                        delay = remaining;
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }

                    // Re-check after delay.
                    if (!_entries.TryGetValue(sessionId, out entry) ||
                        entry.LockUntil <= DateTimeOffset.UtcNow)
                    {
                        _entries.TryRemove(sessionId, out _);
                        return;
                    }

                    try
                    {
                        await receiver.RenewSessionLockAsync(ct).ConfigureAwait(false);
                    }
                    catch (ServiceBusException ex) when (
                        ex.Reason == ServiceBusFailureReason.SessionLockLost ||
                        ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                    {
                        await receiver.DisposeAsync().ConfigureAwait(false);
                        receiver = null;

                        // Try to reacquire on the outer loop.
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected on shutdown/unblock
        }
        finally
        {
            if (receiver is not null)
            {
                await receiver.DisposeAsync().ConfigureAwait(false);
            }

            _entries.TryRemove(sessionId, out _);
        }
    }

    private Task<ServiceBusSessionReceiver> AcceptSessionAsync(
        string sessionId,
        CancellationToken ct)
    {
        if (_subscriptionName is null)
        {
            return _client.AcceptSessionAsync(
                _queueOrTopicName,
                sessionId,
                cancellationToken: ct);
        }

        return _client.AcceptSessionAsync(
            _queueOrTopicName,
            _subscriptionName,
            sessionId,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        _disposeCts.Cancel();

        foreach (var entry in _entries.Values)
        {
            entry.Cancel();
        }

        // Give workers a moment to notice cancellation if desired.
        await Task.Yield();

        _disposeCts.Dispose();
    }

    private sealed class BlockEntry
    {
        private long _lockUntilUnixMs;
        private readonly CancellationTokenSource _cts;
        private readonly Task _worker;

        private BlockEntry(
            string sessionId,
            DateTimeOffset lockUntil,
            Func<string, CancellationToken, Task> workerFactory)
        {
            _lockUntilUnixMs = lockUntil.ToUnixTimeMilliseconds();
            _cts = new CancellationTokenSource();
            _worker = workerFactory(sessionId, _cts.Token);
        }

        public DateTimeOffset LockUntil =>
            DateTimeOffset.FromUnixTimeMilliseconds(
                Interlocked.Read(ref _lockUntilUnixMs));

        public static BlockEntry Start(
            string sessionId,
            DateTimeOffset lockUntil,
            Func<string, CancellationToken, Task> workerFactory) =>
            new(sessionId, lockUntil, workerFactory);

        public void Extend(DateTimeOffset newLockUntil)
        {
            while (true)
            {
                var current = Interlocked.Read(ref _lockUntilUnixMs);
                var proposed = newLockUntil.ToUnixTimeMilliseconds();

                if (proposed <= current)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _lockUntilUnixMs, proposed, current) == current)
                {
                    return;
                }
            }
        }

        public void Cancel() => _cts.Cancel();
    }
}