using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));
var queueName = "sendrecieve";

//create queue
if (!await adminClient.QueueExistsAsync(queueName))
{
    await adminClient.CreateQueueAsync(new CreateQueueOptions(queueName)
    {
        LockDuration = TimeSpan.FromSeconds(2)
    });
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusReceiver receiver = client.CreateReceiver(queueName);

var messages = receiver.ReceiveMessagesAsync();
if (messages != null)
{
    await foreach (var message in messages)
    {
        string body = message.Body.ToString();
        Console.WriteLine($"Received: {body}");

        await receiver.CompleteMessageAsync(message);
    }
}