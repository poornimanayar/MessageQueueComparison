using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var topicName = "pubsubwithfilter";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));


//create topic
if (!await adminClient.TopicExistsAsync(topicName))
{
    await adminClient.CreateTopicAsync(topicName);
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender(topicName);

var colours = new string[] { "red", "blue" , "green"};
Random random = new();

for (int i = 0; i < 100; i++)
{
    var chosenColour = colours[random.Next(0, 3)];
    var message = new ServiceBusMessage($"Message-{i}"){ Subject = chosenColour };

    //add custom metadata
    message.ApplicationProperties.Add("colour", chosenColour);

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message {i} with subject {chosenColour} sent to topic");
}

Console.ReadKey();