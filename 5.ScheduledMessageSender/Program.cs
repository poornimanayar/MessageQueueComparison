using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "scheduledMessages";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

//create queue
if (!await adminClient.QueueExistsAsync(queueName))
{
    await adminClient.CreateQueueAsync(queueName);
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSender sender = client.CreateSender(queueName);

var message = new ServiceBusMessage("I am a scheduled message") { Subject = "Scheduled Message" };

// Use the producer client to send the batch of messages to the Service Bus queue
var sequenceNumber = await sender.ScheduleMessageAsync(message, DateTimeOffset.Now.AddMinutes(1));

//cancel message using the sequence number
//await sender.CancelScheduledMessageAsync(sequenceNumber);

Console.WriteLine($"Scheduled Message sent to queue at {DateTime.UtcNow}");


Console.ReadKey();