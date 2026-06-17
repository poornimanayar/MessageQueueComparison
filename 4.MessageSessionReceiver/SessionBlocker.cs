using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;


public class SessionBlocker
{
    private readonly ConcurrentDictionary<string, SessionBlockerEntry> entries = new();
    private readonly ServiceBusClient client;
    private readonly string queueOrTopicName;
    private readonly string? subscriptionName;

    public SessionBlocker(
        ServiceBusClient client,
        string queueOrTopicName,
        string? subscriptionName = null)
    {
        this.client = client;
        this.queueOrTopicName = queueOrTopicName;
        this.subscriptionName = subscriptionName;
    }

    public async Task ReleaseSessions(DateTimeOffset now)
    {
        var toRelease = entries.Values.Where(x => x.BlockUntil < now).ToArray();

        foreach (var entry in toRelease)
        {
            Console.WriteLine("SB: Releasing block on session" + entry.SessionId);

            await entry.Receiver.DisposeAsync();
            entries.Remove(entry.SessionId, out _);
        }
    }

    public async Task BlockSessionUntil(string sessionId, DateTimeOffset blockUntil)
    {
        Console.WriteLine("SB: Blocking session" + sessionId);

        var receiver = await AcceptSessionAsync(sessionId, CancellationToken.None);

        Console.WriteLine("SB: Blocked session" + sessionId);

        entries.AddOrUpdate(sessionId, s => new SessionBlockerEntry(s, receiver, blockUntil), (s, entry) => entry);
    }

    Task<ServiceBusSessionReceiver> AcceptSessionAsync(
        string sessionId,
        CancellationToken ct)
    {
        if (subscriptionName is null)
        {
            return client.AcceptSessionAsync(
                queueOrTopicName,
                sessionId,
                cancellationToken: ct);
        }

        return client.AcceptSessionAsync(
            queueOrTopicName,
            subscriptionName,
            sessionId,
            cancellationToken: ct);
    }
}

public class SessionBlockerEntry(string sessionId, ServiceBusSessionReceiver receiver, DateTimeOffset blockUntil)
{
    public string SessionId { get; } = sessionId;
    public ServiceBusSessionReceiver Receiver { get; } = receiver;
    public DateTimeOffset BlockUntil { get; } = blockUntil;
}
