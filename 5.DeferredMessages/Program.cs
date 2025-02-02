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

ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

// add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// add handler to process any errors
processor.ProcessErrorAsync += ErrorHandler;

// start processing 
await processor.StartProcessingAsync();

Console.ReadKey();

await processor.StopProcessingAsync();

// handle received messages
async Task MessageHandler(ProcessMessageEventArgs args)
{
    //use the sequence number to retrieve the message later
    var seqNumber = args.Message.SequenceNumber;

    await args.DeferMessageAsync(args.Message);


    //use the ServiceBusReceiver to receive the deferred message
    //ServiceBusReceiver receiver = client.CreateReceiver(queueName);
    //var deferredMessage = await receiver.ReceiveDeferredMessageAsync(seqNumber);
    //process message and complete it

}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}