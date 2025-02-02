using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

var regionId = Environment.GetEnvironmentVariable("AWS_REGION");
var topicArn = Environment.GetEnvironmentVariable("AWS_TOPIC");
var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(regionId));

while (true)
{

    var publishRequest = new PublishRequest
    {
        TopicArn = topicArn,
        Message = $"Hello from SNS Publisher App! {Guid.NewGuid().ToString()}"
    };

    var response = await snsClient.PublishAsync(publishRequest);

    Console.WriteLine($"Message sent! Message ID: {response.MessageId}");
    
    Thread.Sleep(3000);
}