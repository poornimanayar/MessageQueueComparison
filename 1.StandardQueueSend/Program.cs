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

while (true)
{
    //create a random message description of max 10 characters and random length
    var description = $"{RandomString.GetString(Types.ALPHANUMERIC_MIXEDCASE_WITH_SYMBOLS, 10, true)}";

    Console.WriteLine($"Sending message with description : {description}");

    MySqsMessage message = new(description);

    //create message attributes
    Dictionary<string, MessageAttributeValue> messageAttributes = new()
    { { "my-attribute", new()
    {
        DataType = "String", //can be Number, String, Binary
        StringValue = attributeValues[random.Next(0, attributeValues.Length)]
    } },
    { "my-attribute-int", new()
    {
        DataType = "Number",
        StringValue = "5" //the StringValue is interpreted and validated as a number when sending
    } },
    { "my-attribute-custom", new()
    {
        DataType = "Number.my-custom-label",//create a custom data type by appending the label to the data type
        StringValue = "5" //the StringValue is interpreted and validated as a number when sending
    } }};

    //send the message to the queue, get the system assigned message id, max 100 characters
    var sendMessageResponse = await sqsClient.SendMessageAsync(new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = JsonSerializer.Serialize(message),
        MessageAttributes = messageAttributes
    });

    Console.WriteLine($"Sent message successfully, message id : {sendMessageResponse.MessageId}");

    Thread.Sleep(2000);
}


public record MySqsMessage(string Description);


