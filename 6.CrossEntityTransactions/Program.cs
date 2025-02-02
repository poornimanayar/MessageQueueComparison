using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Azure.Amqp.Framing;
using System.Text;
using System.Transactions;

var queueName = "transactions";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

//create queue
if (!await adminClient.QueueExistsAsync(queueName))
{
    await adminClient.CreateQueueAsync(queueName);
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString")
    , new ServiceBusClientOptions() { EnableCrossEntityTransactions = true});

await using ServiceBusSender sender = client.CreateSender(queueName);


ServiceBusReceiver receiverA = client.CreateReceiver("queue1");
ServiceBusSender senderB = client.CreateSender("queue2");
ServiceBusSender senderC = client.CreateSender("queue3");

ServiceBusReceivedMessage receivedMessage = await receiverA.ReceiveMessageAsync();

using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    await receiverA.CompleteMessageAsync(receivedMessage);
    await senderB.SendMessageAsync(new ServiceBusMessage());
    await senderC.SendMessageAsync(new ServiceBusMessage());
    ts.Complete();
}

Console.ReadKey();