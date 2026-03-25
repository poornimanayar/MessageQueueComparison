using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using RandomString4Net;

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

var topicName = "topic-session";

//create topic
if (!await adminClient.TopicExistsAsync(topicName))
{
    await adminClient.CreateTopicAsync(topicName);
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender(topicName);

string messageBody;

Random random = new Random();

while (true)
{
    var sessionId = random.Next(1, 3).ToString();
    messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS,  10, false);
    
    //Send a message to the queue, serialize before sending an object
    var message = new ServiceBusMessage($"{messageBody}"){SessionId = sessionId};

    //add custom metadata
    message.ApplicationProperties.Add("user-property-1", "user-property-value");
    
    // Use the producer client to send the batch of messages to the topic
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message with body {messageBody} and sessionId {sessionId} sent to queue");

    Thread.Sleep(2000);
}