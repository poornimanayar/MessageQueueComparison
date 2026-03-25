using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "messagesessionssample";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));
Random random = new();

//create topic
if (await adminClient.QueueExistsAsync(queueName))
{
    ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

    ServiceBusSessionProcessorOptions sessionProcessorOptions = new()
    {
        AutoCompleteMessages = false,
        MaxConcurrentSessions = 2,
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        SessionIdleTimeout = TimeSpan.FromSeconds(3),
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
        PrefetchCount = 2
        //SessionIds = use a list of sessionIds to filter the sessions to process or leave empty to process all sessions
    };

    ServiceBusSessionProcessor processor = client.CreateSessionProcessor(queueName, sessionProcessorOptions);

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
}

// handle received messages
async Task MessageHandler(ProcessSessionMessageEventArgs args)
{
    if (args.Message.ApplicationProperties.ContainsKey("PauseRetries") &&
        Convert.ToBoolean(args.Message.ApplicationProperties["PauseRetries"]))
    {
        var start = (DateTimeOffset)args.Message.ApplicationProperties["PauseTimeStart"];
        var duration = (TimeSpan)args.Message.ApplicationProperties["PauseDuration"];

        var dateTimeOffset = start.Add(duration);
        Console.WriteLine($"Detected pause retries for session {args.SessionId}, waiting until {dateTimeOffset}");

        if (dateTimeOffset < DateTimeOffset.Now)
        {
            Console.WriteLine("Not time to consume this just yet, abandoning...");
            args.ReleaseSession();
            await args.AbandonMessageAsync(args.Message);
        }

        Console.WriteLine("Time to consume this now, let's try again...");
    }
    
    try
    {
        string body = args.Message.Body.ToString();
        Console.WriteLine($"Received message with sessionId {args.Message.SessionId} and content {body}. ");
        
        //check whether this is the last message in the session
        var isLast = args.Message.ApplicationProperties["IsLast"];
    
        var randomMessage =random.Next(0, 3).ToString();
        if (randomMessage == "1")
            throw new Exception("kaboom");
    
        args.ReleaseSession();
        // complete the message. message is deleted from the queue. 
        await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        // somehow keep track of the session id so we can block further processing
        args.ReleaseSession();
        var properties = new Dictionary<string, object>
        {
            { "PauseRetries", true },
            { "PauseTimeStart", DateTimeOffset.UtcNow },
            { "PauseDuration", TimeSpan.FromSeconds(10) },
            
        };
        await args.AbandonMessageAsync(args.Message, properties);
        throw;
    }
    
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}