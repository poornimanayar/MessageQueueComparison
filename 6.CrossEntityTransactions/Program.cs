using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System.Transactions;

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

if (!await adminClient.QueueExistsAsync("queue1"))
{
    await adminClient.CreateQueueAsync("queue1");
}
if (!await adminClient.QueueExistsAsync("queue2"))
{
    await adminClient.CreateQueueAsync("queue2");
}
if (!await adminClient.QueueExistsAsync("queue3"))
{
    await adminClient.CreateQueueAsync("queue3");
}
ServiceBusClient senderClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

await using ServiceBusSender sender = senderClient.CreateSender("queue1");
await sender.SendMessageAsync(new ServiceBusMessage());

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString")
    , new ServiceBusClientOptions() { EnableCrossEntityTransactions = true });

ServiceBusReceiver receiverA = client.CreateReceiver("queue1");
ServiceBusSender senderB = client.CreateSender("queue2");
ServiceBusSender senderC = client.CreateSender("queue3");

ServiceBusReceivedMessage receivedMessage = await receiverA.ReceiveMessageAsync();

using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    await senderB.SendMessageAsync(new ServiceBusMessage());
    await senderC.SendMessageAsync(new ServiceBusMessage());
    await receiverA.CompleteMessageAsync(receivedMessage);
    ts.Complete();
}

Console.ReadKey();