using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


var regionId = Environment.GetEnvironmentVariable("AWS_REGION");
var topicArn = Environment.GetEnvironmentVariable("AWS_TOPIC");

var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(regionId));

var filterPolicy = "{\"my-attribute-int\": [5]}";

var subscribeRequest = new SubscribeRequest
{
    TopicArn = topicArn,
    Protocol = "sqs",
    Endpoint = "arn:aws:sqs:eu-west-1:099229970436:sub3",
    Attributes = new Dictionary<string, string>
    {
       //{"FilterPolicy", filterPolicy},
        //{"FilterPolicyScope", "MessageAttributes"}, //defaults to MessageAtributes, can be set to MessageBody
        {"RawMessageDelivery", "true"}
    }
};

var response = await snsClient.SubscribeAsync(subscribeRequest);

Console.WriteLine($"Subscribed! Subscription ARN: {response.SubscriptionArn}");