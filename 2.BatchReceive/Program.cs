using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var queueUrl = Environment.GetEnvironmentVariable("AWS_QUEUE_URL");
var regionId = Environment.GetEnvironmentVariable("AWS_REGION");

var sqsClient = new AmazonSQSClient(accessKey, secret, RegionEndpoint.GetBySystemName(regionId));

while (true)
{
    var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
    { QueueUrl = queueUrl, MaxNumberOfMessages = 5, WaitTimeSeconds = 0 });//short polling enabled, only up to 5 messages retrieved, can vary

    foreach (var message in response.Messages)
    {
        var receivedMessage = JsonSerializer.Deserialize<MySqsMessage>(message.Body);

        Console.WriteLine(
            $"Received message with message id {message.MessageId} description : {receivedMessage.Description}");

        Console.WriteLine("=====================================");

        //explicitly delete the processed message using the receipt handle
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest()
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        });
    }

    Thread.Sleep(2000);
}

public record MySqsMessage(string Description);