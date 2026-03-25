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

Dictionary<string, int> counter = new();

for (int i = 0; i <20; i++)
{
    //create unique application-generated session id to group messages into a session
    var sessionId = random.Next(0, 3).ToString();
    
    if(counter.ContainsKey(sessionId))
        counter[sessionId]++;
    else
        counter.Add(sessionId, 1);
    
    messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, false);

    var message = new ServiceBusMessage($"{i}") { SessionId = sessionId, TimeToLive = TimeSpan.FromSeconds(3)};
    
    //indicates last message in the session
    message.ApplicationProperties.Add("IsLast", i == 19);

    // Use the producer client to send the batch of messages to the Service Bus queue
    await sender.SendMessageAsync(message);

    Console.WriteLine($"Message with sessionId {sessionId} sent to queue with body {message.Body}");
    
}

foreach (var c in counter)
{
   Console.WriteLine(c.Key + ": " + c.Value);
}

Console.ReadKey();