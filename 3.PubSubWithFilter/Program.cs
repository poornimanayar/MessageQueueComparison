using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

Console.WriteLine("Enter a name for the subscription");
var subscriberName = Console.ReadLine();

Console.WriteLine("Enter the subject of messages to recieve - 0 = All, 1 = Red or 2 = Blue");
var ruleName = Console.ReadLine();

var topicName = "pubsubwithfilter";

ServiceBusAdministrationClient adminClient = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

//create topic
if (!await adminClient.TopicExistsAsync(topicName))
{
    await adminClient.CreateTopicAsync(topicName);
}

//create subscriptionRules
if (ruleName == "0")
{
    if (!await adminClient.SubscriptionExistsAsync(topicName, subscriberName))
    {
        //demo 2-step process of creating a subscription and then adding a rule
        //create subscription
        await adminClient.CreateSubscriptionAsync(topicName, subscriberName);

        //delete the default rule and re-add it with a boolean true filter  
        await adminClient.DeleteRuleAsync(topicName, subscriberName, RuleProperties.DefaultRuleName);
        await adminClient.CreateRuleAsync(topicName, subscriberName,
            new CreateRuleOptions(RuleProperties.DefaultRuleName, new TrueRuleFilter()));
    }
}
else if (ruleName == "1")
{
    if (!await adminClient.SubscriptionExistsAsync(topicName, subscriberName))
    {
        //create subscription and add a filter for messages with colour = red
        await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicName, subscriberName),
            new CreateRuleOptions { Name = "RedSqlRule", Filter = new SqlRuleFilter("colour = 'red'") });
    }
}
else if (ruleName == "2")
{
    if (!await adminClient.SubscriptionExistsAsync(topicName, subscriberName))
    {
        //create subscription and add a filter for messages with subject = blue
        await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicName, subscriberName),
            new CreateRuleOptions { 
                Name = "BlueCorrelationRule", 
                Filter = new CorrelationRuleFilter() { Subject = "blue" } , 
                Action = new SqlRuleAction("SET actionset = 'yup!'" )});
    }
}

ServiceBusClient client = new(Environment.GetEnvironmentVariable("ASB:ConnectionString"));

ServiceBusProcessor processor = client.CreateProcessor(topicName, subscriberName, new ServiceBusProcessorOptions());

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
    string body = args.Message.Body.ToString();
    Console.WriteLine(
        $"Received message with subject {args.Message.Body} and content {body}. The application property colour is {args.Message.ApplicationProperties.GetValueOrDefault("colour")}. The application property colour is {args.Message.ApplicationProperties.GetValueOrDefault("actionset")}");


    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}