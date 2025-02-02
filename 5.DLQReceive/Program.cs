using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var queueUrl = Environment.GetEnvironmentVariable("AWS_QUEUE_URL");
var regionId = Environment.GetEnvironmentVariable("AWS_REGION");

var sqsClient = new AmazonSQSClient(accessKey, secret, RegionEndpoint.GetBySystemName(regionId));

var request = new ReceiveMessageRequest
{
    QueueUrl = queueUrl,
    MessageAttributeNames = new List<string>{"All"}
};
while (true)
{
    var response = await sqsClient.ReceiveMessageAsync(request);

    foreach (var message in response.Messages)
    {
        throw new Exception("An exception occurred while processing the message"); //simulate an exception
    }
}

//Run the 1.StandardQueueReceive project first to receive the messages