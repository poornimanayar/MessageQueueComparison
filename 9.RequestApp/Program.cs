using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using RandomString4Net;

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));
var requestqueueName = "requestqueue";
var replyQueueName = "replyqueue";
string[] requestIds = [];

//create queue
if (!await adminClient.QueueExistsAsync(requestqueueName))
{
    await adminClient.CreateQueueAsync(new CreateQueueOptions(requestqueueName)
    {
        LockDuration = TimeSpan.FromSeconds(2)
    });
}

if (!await adminClient.QueueExistsAsync(replyQueueName))
{
    await adminClient.CreateQueueAsync(new CreateQueueOptions(replyQueueName)
    {
        LockDuration = TimeSpan.FromSeconds(2)
    });
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender(requestqueueName);
ServiceBusReceiver receiver = client.CreateReceiver(replyQueueName);

//configure the behavior of the ServiceBusProcessor
ServiceBusProcessorOptions options = new()
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 1,
    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
    ReceiveMode = ServiceBusReceiveMode.PeekLock,
    Identifier = "1.Receiver" //id of the ServiceBusProcessor
};

string messageBody = string.Empty;

messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true);

var messageId = Guid.NewGuid().ToString();
//Send a message to the queue, serialize before sending an object
var message = new ServiceBusMessage($"{messageBody}")
{
    MessageId = messageId,
    ReplyTo = replyQueueName
};

//add custom metadata
message.ApplicationProperties.Add("user-property-1", "user-property-value");

// Use the producer client to send the batch of messages to the Service Bus queue
await sender.SendMessageAsync(message);

Console.WriteLine($"Message with body {messageBody} sent to queue");

requestIds.Append(messageId);


var reply = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));

if (reply != null && reply.CorrelationId == messageId)
{
    string body = reply.Body.ToString();
    Console.WriteLine($"Received: {body}");
   await receiver.CompleteMessageAsync(reply);
}
else
{
    Console.WriteLine("No match found");
}

Console.ReadKey();