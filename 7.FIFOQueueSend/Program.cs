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

var attributeValues = new string[] { "value1", "value2", "value3" };
Random random = new();
int i = 1;
while (true)
{
    //create a random message description of max 10 characters and random length
    var description = $"{RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true)}";

    MySqsMessage message = new(description, i);

    var messageGroupId = random.Next(0, 3).ToString();
    //send the message to the queue, get the system assigned message id, max 100 characters
    var sendMessageResponse = await sqsClient.SendMessageAsync(new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = JsonSerializer.Serialize(message),
        MessageGroupId = messageGroupId,
        MessageDeduplicationId = Guid.NewGuid().ToString()
    });
    
    Console.WriteLine($"Sending message with description : {description}, message number {i}, message group : {messageGroupId}");

    Thread.Sleep(2000);

    i++;
}


public record MySqsMessage(string Description, int MessageNumber);


