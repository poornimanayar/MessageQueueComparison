using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

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

//configure the behavior of the ServiceBusProcessor
ServiceBusProcessorOptions options = new()
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 1,
    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
    ReceiveMode = ServiceBusReceiveMode.PeekLock,
    PrefetchCount = 10,
    Identifier = "1.Receiver" //id of the ServiceBusProcessor
};

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

// create a processor that we can use to process the messages
// Abstraction around the `ServiceBusReceiver` that allows using an event based model for processing received message
ServiceBusProcessor processor = client.CreateProcessor(queueName, options);

// add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// add handler to process any errors, mandatory to implement
processor.ProcessErrorAsync += ErrorHandler;

// start processing 
await processor.StartProcessingAsync();

Console.WriteLine("Wait for a minute and then press any key to end the processing");
Console.ReadKey();

await processor.StopProcessingAsync();

// handle received messages
async Task MessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");
    Console.WriteLine("==================METADATA=================================");
    foreach (var applicationProperty in args.Message.ApplicationProperties)
    {
        Console.WriteLine($"{applicationProperty.Key} - {applicationProperty.Value}");
    }
    Console.WriteLine($"{args.Message.MessageId}");
    Console.WriteLine("===================================================");
    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}