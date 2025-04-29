using System.Text;
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

int i = 0;

while (i < 10)
{
    messageBody = RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, false);

    //Send a message to the queue, serialize before sending an object
    var message = new ServiceBusMessage($"{messageBody}");

    //add custom metadata
    message.ApplicationProperties.Add("user-property-1", "user-property-value");
    if (i == 0)
    {
        message.ScheduledEnqueueTime = DateTimeOffset.UtcNow.AddMinutes(1);
        // Use the producer client to send the batch of messages to the Service Bus queue
        await sender.SendMessageAsync(message);

        Console.WriteLine($"Message with body {messageBody} sent to queue");
    }
   else if (i % 2 == 0)
    {
        // Use the producer client to send the batch of messages to the Service Bus queue
        await sender.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddMinutes(1));

        Console.WriteLine($"Message with body {messageBody} sent to queue");
    }
    else
    {
        // Use the producer client to send the batch of messages to the Service Bus queue
        await sender.SendMessageAsync(message);

        Console.WriteLine($"Message with body {messageBody} sent to queue");
    }

    Thread.Sleep(3000);

    i++;
}