using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

var queueName = "messagesessionssample-20260327.0";

var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
var clientOptions = new ServiceBusAdministrationClientOptions();
ServiceBusAdministrationClient adminClient = new(connectionString, clientOptions);
var options = new ServiceBusClientOptions();
var serviceBusClient = new ServiceBusClient(connectionString, options);
SessionBlocker sessionBlocker = new SessionBlocker(serviceBusClient, queueName);
Random random = new();

//create topic
if (await adminClient.QueueExistsAsync(queueName))
{
    ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

    ServiceBusSessionProcessorOptions sessionProcessorOptions = new()
    {
        AutoCompleteMessages = false,
        MaxConcurrentSessions = 6,
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
    if (args.Message.ApplicationProperties.ContainsKey("BlockSession") &&
        Convert.ToBoolean(args.Message.ApplicationProperties["BlockSession"]))
    {
        var blockUntil = (DateTimeOffset)args.Message.ApplicationProperties["BlockUntil"];
        var blockSessionId = (string)args.Message.ApplicationProperties["SessionId"];

        try
        {
            await sessionBlocker.BlockSessionUntil(blockSessionId, blockUntil);
            args.ReleaseSession();
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error blocking sessions");
            args.ReleaseSession();

            //Best effort
            await args.CompleteMessageAsync(args.Message);
        }
        return;
    }


    var body = args.Message.Body.ToString();
    
    try
    {
        Console.WriteLine($"Received message with sessionId {args.Message.SessionId} and content {body} at {DateTime.UtcNow}");

        if (args.Message.SessionId == "7")
        {
            throw new Exception("kaboom happening at " + DateTime.UtcNow);
        }
        args.ReleaseSession();
        // complete the message. message is deleted from the queue. 
        await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception e)
    {
        //Console.WriteLine($"Executing recovery for message with sessionId {args.Message.SessionId} and content {body} at {DateTime.UtcNow}");

        args.ReleaseSession();

        int immediateRetries;
        if (args.Message.ApplicationProperties.TryGetValue("ImmediateRetryAttempts", out var immediateRetriesProperty))
        {
            immediateRetries = (int)immediateRetriesProperty;
        }
        else
        {
            immediateRetries = 0;
        }

        var immediateRetryCount = 4;
        if (immediateRetries < immediateRetryCount)
        {
            Console.WriteLine($"Requesting immediate retry {immediateRetries + 1} for message with sessionId {args.Message.SessionId}");
            var immediateProps = new Dictionary<string, object>
            {
                { "ImmediateRetryAttempts", immediateRetries + 1 },

            };
            await args.AbandonMessageAsync(args.Message, immediateProps);
            return;
        }

        int delayedRetries;
        if (args.Message.ApplicationProperties.TryGetValue("DelayedRetryAttempts", out var delayedRetriesProperty))
        {
            delayedRetries = (int)delayedRetriesProperty;
        }
        else
        {
            delayedRetries = 0;
        }

        Console.WriteLine($"Scheduling delayed retry {delayedRetries + 1} for message with sessionId {args.Message.SessionId}");

        var blockDuration = TimeSpan.FromSeconds(15);
        var waitUntilTime = DateTimeOffset.UtcNow.Add(blockDuration);

        //HINT: The control message might be picked up by another instance and this is fine
        var message = new ServiceBusMessage("Control message")
        {
            //HINT: We need a new session ID so that the control message is not enqueued after the failing message
            SessionId = Guid.NewGuid().ToString(),
            ApplicationProperties =
            {
                ["BlockSession"] = "true",
                ["SessionId"] = args.SessionId,
                ["BlockUntil"] = waitUntilTime
            }
        };
        await sender.SendMessageAsync(message);

        var properties = new Dictionary<string, object>
        {
            { "DelayedRetryAttempts", delayedRetries + 1 },
            { "ImmediateRetryAttempts", 0 },
        };
        await args.AbandonMessageAsync(args.Message, properties);
    }
    
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine("==========I am here at error handler==============");
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}