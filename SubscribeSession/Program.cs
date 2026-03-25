using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

Console.WriteLine("Enter a name for the subscription");
var subscriberName = Console.ReadLine();

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

var topicName = "topic-session";

ServiceBusSessionProcessorOptions sessionProcessorOptions = new()
{
    AutoCompleteMessages = false,
    MaxConcurrentSessions = 1,
    ReceiveMode = ServiceBusReceiveMode.PeekLock,
    SessionIdleTimeout = TimeSpan.FromSeconds(3),
    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
    PrefetchCount = 2
    //SessionIds = use a list of sessionIds to filter the sessions to process or leave empty to process all sessions
};


if (!await adminClient.SubscriptionExistsAsync(topicName, subscriberName))
{
    await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicName, subscriberName)
        { RequiresSession = true });
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusSessionProcessor
    processor = client.CreateSessionProcessor(topicName, subscriberName, sessionProcessorOptions);

// add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// add handler to process any errors
processor.ProcessErrorAsync += ErrorHandler;

//processor.SessionInitializingAsync += SessionInitializing;

Task SessionInitializing(ProcessSessionEventArgs arg)
{
    Console.WriteLine("Session initializing");
    return Task.CompletedTask;
}

//processor.SessionClosingAsync += SessionClosing;

Task SessionClosing(ProcessSessionEventArgs arg)
{
    Console.WriteLine("Session closing");
    arg.SetSessionStateAsync(null);
    arg.ReleaseSession();
    return Task.CompletedTask;
}

// start processing 
await processor.StartProcessingAsync();

Console.ReadKey();

await processor.StopProcessingAsync();


// handle received messages
async Task MessageHandler(ProcessSessionMessageEventArgs args)
{
    string body = args.Message.Body.ToString();

    Console.WriteLine($"Received message with sessionId {args.Message.SessionId} and content {body}. ");

    //check whether this is the last message in the session
    //var isLast = args.Message.ApplicationProperties["IsLast"];

    //var randomMessage =random.Next(0, 3).ToString();

    // if (randomMessage == "2")
    // {
    //     Console.WriteLine($"Abandoned message with body {body}");
    //     await args.DeadLetterMessageAsync(args.Message);
    // }
    // else
    // {
    //     await args.CompleteMessageAsync(args.Message);
    // }

    //args.ReleaseSession();

    // if (bool.Parse(isLast.ToString() ?? string.Empty)) 
    // {
    //     Console.WriteLine($"Last message in the session {args.Message.SessionId}");
    //    await args.SetSessionStateAsync(null);
    //     args.ReleaseSession();
    // }

    var sessionState = await args.GetSessionStateAsync();
    
    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
    
    args.ReleaseSession();
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}