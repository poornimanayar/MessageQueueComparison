using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "messagesessionssample-20260327.0";
var desiredConcurrency = 2;
var currentConcurrency = desiredConcurrency;

var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
var clientOptions = new ServiceBusAdministrationClientOptions();
ServiceBusAdministrationClient adminClient = new(connectionString, clientOptions);
var serviceBusClient = new ServiceBusClient(connectionString);
SessionBlocker sessionBlocker = new SessionBlocker(serviceBusClient, queueName);
Random random = new();

//create topic
if (await adminClient.QueueExistsAsync(queueName))
{
    ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

    ServiceBusSessionProcessorOptions sessionProcessorOptions = new()
    {
        AutoCompleteMessages = false,
        MaxConcurrentSessions = desiredConcurrency,
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        SessionIdleTimeout = TimeSpan.FromMinutes(3),
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
        PrefetchCount = 0,
        //SessionIds = use a list of sessionIds to filter the sessions to process or leave empty to process all sessions
    };

    ServiceBusSessionProcessor processor = serviceBusClient.CreateSessionProcessor(queueName, sessionProcessorOptions);

    // add handler to process messages
    processor.ProcessMessageAsync += eventArgs => MessageHandler(eventArgs, sender, processor);
    

    // add handler to process any errors
    processor.ProcessErrorAsync += ErrorHandler;

    //processor.SessionInitializingAsync += SessionInitializing;

    // Task SessionInitializing(ProcessSessionEventArgs arg)
    // {
    //     Console.WriteLine("Session initializing");
    //     return Task.CompletedTask;
    // }

    //processor.SessionClosingAsync += SessionClosing;

    // Task SessionClosing(ProcessSessionEventArgs arg)
    // {
    //     Console.WriteLine("Session closing");
    //     arg.SetSessionStateAsync(null);
    //     arg.ReleaseSession();
    //     return Task.CompletedTask;
    // }

    // start processing 
    await processor.StartProcessingAsync();

    var tokenSource = new CancellationTokenSource();
    var releaseTask = Task.Run(async () =>
    {
        while (!tokenSource.IsCancellationRequested)
        {
            await sessionBlocker.ReleaseSessions(DateTimeOffset.UtcNow);
            await Task.Delay(3000, tokenSource.Token);
        }
    });

    Console.ReadKey();
    tokenSource.Cancel();

    await releaseTask;

    await processor.StopProcessingAsync();
}

// handle received messages
async Task MessageHandler(ProcessSessionMessageEventArgs args, ServiceBusSender sender,
    ServiceBusSessionProcessor processor)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Delivery count {args.Message.DeliveryCount} for session {args.SessionId}");
    if (args.Message.ApplicationProperties.ContainsKey("PauseRetries") &&
        Convert.ToBoolean(args.Message.ApplicationProperties["PauseRetries"]))
    {
        var start = (DateTimeOffset)args.Message.ApplicationProperties["PauseTimeStart"];
        var duration = (TimeSpan)args.Message.ApplicationProperties["PauseDuration"];

        var waitUntilTime = start.Add(duration);
        Console.WriteLine($"Detected pause retries for session {args.SessionId} message {body}, waiting until {waitUntilTime}");

        if (waitUntilTime > DateTimeOffset.Now)
        {
            Console.WriteLine($"Not time to consume this just yet, abandoning message {body} and blocking the session...");

            args.ReleaseSession();

            lock (processor)
            {
                currentConcurrency++;
                processor.UpdateConcurrency(currentConcurrency, 1);
            }

            try
            {
                Console.WriteLine("Sleeping for 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10));
                Console.WriteLine("Waking up and abandoning...");

                await args.AbandonMessageAsync(args.Message);
            }
            finally
            {
                lock (processor)
                {
                    currentConcurrency--;
                    processor.UpdateConcurrency(currentConcurrency, 1);
                }
            }
            return;
        }
        Console.WriteLine($"Time to consume this now, let's try again... {body}" + DateTime.UtcNow);

    }
    try
    {
       
        Console.WriteLine($"Received message with sessionId {args.Message.SessionId} and content {body} at {DateTime.UtcNow}");

        if (args.Message.SessionId == "7" || args.Message.SessionId == "15")
        {
            throw new Exception("kaboom happening at " + DateTime.UtcNow);
        }

        await Task.Delay(2000);
    
        args.ReleaseSession();
        // complete the message. message is deleted from the queue. 
        await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Abandoning message with sessionId {args.Message.SessionId} and content {body} at {DateTime.UtcNow}");
        // somehow keep track of the session id so we can block further processing
        args.ReleaseSession();
        var properties = new Dictionary<string, object>
        {
            { "PauseRetries", true },
            { "PauseTimeStart", DateTimeOffset.UtcNow },
            { "PauseDuration", TimeSpan.FromSeconds(600) },
            
        };
        await args.AbandonMessageAsync(args.Message,properties);
       // throw;
    }
    
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine("==========I am here at error handler==============");
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}