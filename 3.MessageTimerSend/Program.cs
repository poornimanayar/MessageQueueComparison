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

while (true)
{
    //create a random message description of max 10 characters and random length
    var description = $"{RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true)}";

    Console.WriteLine($"Sending message with description : {description}");

    MySqsMessage message = new(description);
    
    //send the message to the queue, get the system assigned message id, max 100 characters
    var sendMessageResponse = await sqsClient.SendMessageAsync(new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = JsonSerializer.Serialize(message),
        DelaySeconds = 5 //set message timer to 5 seconds
    });

    Console.WriteLine($"Sent message successfully, message id : {sendMessageResponse.MessageId}");

    Thread.Sleep(2000);
}
//Run the 1.StandardQueueReceive project first to receive the messages              


public record MySqsMessage(string Description);


