using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using RandomString4Net;

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

ServiceBusSender sender = client.CreateSender(queueName);

string messageBody = string.Empty;

while (true)
{
    messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true);

    //Send a message to the queue, serialize before sending an object
    var message = new ServiceBusMessage($"{messageBody}");

    //add custom metadata
    message.ApplicationProperties.Add("user-property-1", "user-property-value");

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message with body {messageBody} sent to queue");

    Thread.Sleep(3000);
}

