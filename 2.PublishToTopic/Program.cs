using Azure.Messaging.ServiceBus;

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender("mytopic");

for (int i = 0; i < 100; i++)
{
    var message = new ServiceBusMessage($"Message-{i}");

    //add custom metadata
    message.ApplicationProperties.Add("user-property-1", "user-property-value");

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message {i} sent to topic");
}

Console.ReadKey();