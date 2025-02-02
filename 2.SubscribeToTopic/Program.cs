using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

Console.WriteLine("Enter a name for the subscription");
var subscriberName = Console.ReadLine();

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

var topicName = "mytopic";

//create topic
if (!await adminClient.SubscriptionExistsAsync(topicName, subscriberName))
{
    await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicName, subscriberName));
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusProcessor processor = client.CreateProcessor(topicName ,subscriberName, new ServiceBusProcessorOptions());

// add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// add handler to process any errors
processor.ProcessErrorAsync += ErrorHandler;

// start processing 
await processor.StartProcessingAsync();

Console.WriteLine("Wait for a minute and then press any key to end the processing");
Console.ReadKey();

// stop processing 
Console.WriteLine("Stopping the receiver...");

await processor.StopProcessingAsync();

// handle received messages
async Task MessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}