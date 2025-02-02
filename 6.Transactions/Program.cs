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

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

await using ServiceBusSender sender = client.CreateSender(queueName);

//without transactions
var message = new ServiceBusMessage("Message 1 without transactions");
await sender.SendMessageAsync(message);

var queueInfo  = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
Console.WriteLine($"Number of messages in the queue: {queueInfo.Value.ActiveMessageCount}");

message = new ServiceBusMessage("Message 1 without transactions");
await sender.SendMessageAsync(message);

queueInfo = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
Console.WriteLine($"Number of messages in the queue: {queueInfo.Value.ActiveMessageCount}");


//with transactions
using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    message = new ServiceBusMessage("Message 1 with transactions");
    await sender.SendMessageAsync(message);

    queueInfo = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
    Console.WriteLine($"Number of messages in the queue after first send with transactions: {queueInfo.Value.ActiveMessageCount}");

    message = new ServiceBusMessage("Message 2 with transactions");
    await sender.SendMessageAsync(message);

    queueInfo = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
    Console.WriteLine($"Number of messages in the queue after second send with transactions {queueInfo.Value.ActiveMessageCount}");

    ts.Complete();
}

queueInfo = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
Console.WriteLine($"Number of messages in the queue after completing transactions: {queueInfo.Value.ActiveMessageCount}");

Console.ReadKey();