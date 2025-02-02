using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "messagesessionssample";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

//create topic
if (await adminClient.QueueExistsAsync(queueName))
{
    ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

    ServiceBusSessionProcessorOptions sessionProcessorOptions = new()
    {
        AutoCompleteMessages = false,
        MaxConcurrentSessions = 1,
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        SessionIdleTimeout = TimeSpan.FromMinutes(3),
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
        //SessionIds = use a list of sessionIds to filter the sessions to process or leave empty to process all sessions
    };

    ServiceBusSessionProcessor processor = client.CreateSessionProcessor(queueName, sessionProcessorOptions);

    // add handler to process messages
    processor.ProcessMessageAsync += MessageHandler;

    // add handler to process any errors
    processor.ProcessErrorAsync += ErrorHandler;

    // start processing 
    await processor.StartProcessingAsync();

    Console.ReadKey();

    await processor.StopProcessingAsync();
}

// handle received messages
async Task MessageHandler(ProcessSessionMessageEventArgs args)
{
    string body = args.Message.Body.ToString();

    Console.WriteLine($"Received message with sessionId {args.Message.SessionId} and content {body}. ");

    //check whether this is the last message in the session
    var isLast = args.Message.ApplicationProperties["IsLast"];

    if (isLast != null && (bool)isLast)
    {
        Console.WriteLine($"Last message in the session {args.Message.SessionId}");
        await args.SetSessionStateAsync(null);
        args.ReleaseSession();
    }

    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}