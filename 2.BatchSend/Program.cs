using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using RandomString4Net;

var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var queueUrl = Environment.GetEnvironmentVariable("AWS_QUEUE_URL");
var regionId = Environment.GetEnvironmentVariable("AWS_REGION");

var sqsClient = new AmazonSQSClient(accessKey, secret, RegionEndpoint.GetBySystemName(regionId));

var batchedMessages = new List<SendMessageBatchRequestEntry>();

while (true)
{
    for (int i = 1; i <= 10; i++)
    {
        //create a random message description of max 10 characters and random length
        var description = $"{RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true)}";

        MySqsMessage message = new(description);

        batchedMessages.Add(new SendMessageBatchRequestEntry($"message-{i}", JsonSerializer.Serialize(message)));
    }

//send batch to the queue
    var sendMessageBatchResponse = await sqsClient.SendMessageBatchAsync(new SendMessageBatchRequest()
    {
        QueueUrl = queueUrl,
        Entries = batchedMessages
    });

//batch can succeed with individual messages failing
//check the response for successful and failed messages
    foreach (var result in sendMessageBatchResponse.Successful)
    {
        Console.WriteLine($"Message {result.Id} successfully queued.");
    }

    foreach (var error in sendMessageBatchResponse.Failed)
    {
        Console.WriteLine($"Failed to send message {error.Id}: {error.Message}");
    }

    batchedMessages.Clear();

    Thread.Sleep(2000);
}

public record MySqsMessage(string Description);