using Azure.Messaging.ServiceBus;

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender("sendrecieve");

for (int i = 0; i < 10; i++)
{
    var message = new ServiceBusMessage($"sendrecieve-{i}");

//add custom metadata
    message.ApplicationProperties.Add("user-property-1", "user-property-value");

// Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message {i} sent to topic");
}

Console.ReadKey();