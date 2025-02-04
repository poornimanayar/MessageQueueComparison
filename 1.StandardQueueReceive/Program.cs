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

    var response = await sqsClient.ReceiveMessageAsync(request);
while (true)
{
    foreach (var message in response.Messages)
    {
        //print the message attributes
        foreach (var messageAttribute in message.MessageAttributes)
        {
            Console.WriteLine($"Message attribute key : {messageAttribute.Key}, value : {messageAttribute.Value.StringValue}");
        }

        var myAttributeHeader = message.MessageAttributes["my-attribute"];
        var receivedMessage = JsonSerializer.Deserialize<MySqsMessage>(message.Body);

        //change visibility timeout if needed periodically using a heartbeat mechanism
       // await sqsClient.ChangeMessageVisibilityAsync(queueUrl, message.ReceiptHandle, 60);

        //process the message based on the attribute value; use MediatR or other patterns to decouple the processing logic
        switch (myAttributeHeader.StringValue)
        {
            case "value1":
                //process the message a handler or a service
                Console.WriteLine("Processing message with attribute value1");
                Console.WriteLine($"Received message, description: {receivedMessage.Description}, messageId : {message.MessageId}");
                break;
            case "value2":
                //process the message a handler or a service
                Console.WriteLine("Processing message with attribute value2");
                Console.WriteLine($"Received message, description: {receivedMessage.Description}, messageId : {message.MessageId}");
                break;
            case "value3":
                //process the message a handler or a service
                Console.WriteLine("Processing message with attribute value3");
                Console.WriteLine($"Received message, description: {receivedMessage.Description}, messageId : {message.MessageId}");
                break;
        }

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