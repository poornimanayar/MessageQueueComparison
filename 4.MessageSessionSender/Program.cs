using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using RandomString4Net;

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

string messageBody = string.Empty;

for (int i = 0; i < 100; i++)
{
    messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, false);

    //create unique application-generated session id to group messages into a session
    var sessionId = random.Next(0, 3).ToString();

    var message = new ServiceBusMessage($"{messageBody}") { SessionId = sessionId};

    //indicates last message in the session
    message.ApplicationProperties.Add("IsLast", i == 99 );

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message with sessionId {sessionId} sent to topic");
    
    Thread.Sleep(3000);
    
}

Console.ReadKey();