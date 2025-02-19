﻿using System.Text.Json;
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
    QueueUrl = queueUrl
};
while (true)
{
    var response = await sqsClient.ReceiveMessageAsync(request);

    foreach (var message in response.Messages)
    {
        var receivedMessage = JsonSerializer.Deserialize<MySqsMessage>(message.Body);
        Console.WriteLine($"Received message, description: {receivedMessage.Description}, messageId : {message.MessageId}");

        Console.WriteLine("=====================================");

        //explicitly delete the processed message using the receipt handle
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest()
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        });
    }
}

public record MySqsMessage(string Description);