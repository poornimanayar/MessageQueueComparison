using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "messagesessionssample";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));


//create topic
if (!await adminClient.QueueExistsAsync(queueName))
{
    await adminClient.CreateQueueAsync(new CreateQueueOptions(queueName)
    {
        RequiresSession = true
    });
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender(queueName);

Random random = new();

for (int i = 0; i < 100; i++)
{
    var sessionId = random.Next(0, 3).ToString();

    var message = new ServiceBusMessage($"Message-{i}") { SessionId = sessionId};

    //indicates last message in the session
    message.ApplicationProperties.Add("IsLast", i == 99 ? 1 : 0);

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message {i} with sessionId {sessionId} sent to topic");
}

Console.ReadKey();