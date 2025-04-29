using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));
var requestqueueName = "requestqueue";
//create queue
if (!await adminClient.QueueExistsAsync(requestqueueName))
{
    await adminClient.CreateQueueAsync(new CreateQueueOptions(requestqueueName)
    {
        LockDuration = TimeSpan.FromSeconds(2)
    });
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

//configure the behavior of the ServiceBusProcessor
ServiceBusProcessorOptions options = new()
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 1,
    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
    ReceiveMode = ServiceBusReceiveMode.PeekLock,
    Identifier = "1.Receiver" //id of the ServiceBusProcessor
};

ServiceBusProcessor processor = client.CreateProcessor(requestqueueName, options);


// add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// add handler to process any errors, mandatory to implement
processor.ProcessErrorAsync += ErrorHandler;

// start processing 
await processor.StartProcessingAsync();

Console.WriteLine("Press any key to exit");
Console.ReadKey();

await processor.StopProcessingAsync();

// handle received messages
async Task MessageHandler(ProcessMessageEventArgs args)
{
    if (!await adminClient.QueueExistsAsync(args.Message.ReplyTo))
    {
        await adminClient.CreateQueueAsync(new CreateQueueOptions(args.Message.ReplyTo)
        {
            LockDuration = TimeSpan.FromSeconds(2),
            AutoDeleteOnIdle = TimeSpan.FromMinutes(5),
        });
    }
    ServiceBusSender reply = client.CreateSender(args.Message.ReplyTo);
    
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");
    Console.WriteLine("==================METADATA=================================");
    foreach (var applicationProperty in args.Message.ApplicationProperties)
    {
        Console.WriteLine($"{applicationProperty.Key} - {applicationProperty.Value}");
    }

    var messageId = args.Message.MessageId;
    Console.WriteLine($"{messageId}");
    Console.WriteLine("===================================================");
    // complete the message. message is deleted from the queue. 

    var replyMessage = new ServiceBusMessage($"Reply to {messageId}") { CorrelationId = messageId};
    
    await reply.SendMessageAsync(replyMessage);
    
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}
