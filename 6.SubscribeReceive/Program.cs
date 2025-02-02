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
    MessageAttributeNames =
        new List<string>
            { "All" }, //use this to get all message attributes, can also be specific attribute names or wildcards 
    MaxNumberOfMessages = 2,
    VisibilityTimeout = 40,
    WaitTimeSeconds = 0 //set to 0 for short polling, 1-20 for long polling
};

while (true)
{
    var response = await sqsClient.ReceiveMessageAsync(request);

    foreach (var message in response.Messages)
    {
        Console.WriteLine($"Received message, description: {message.Body}, messageId : {message.MessageId}");

        Console.WriteLine("=====================================");

        //explicitly delete the processed message using the receipt handle
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest()
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        });
    }
}