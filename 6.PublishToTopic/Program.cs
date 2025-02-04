using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

var regionId = Environment.GetEnvironmentVariable("AWS_REGION");
var topicArn = Environment.GetEnvironmentVariable("AWS_TOPIC");
var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(regionId));

while (true)
{
    //create message attributes
    Dictionary<string, MessageAttributeValue> messageAttributes = new()
    { { "my-attribute", new()
        {
            DataType = "String", //can be Number, String, Binary
            StringValue = "hello"
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

    var publishRequest = new PublishRequest
    {
        TopicArn = topicArn,
        Message = $"Hello from SNS Publisher App! {Guid.NewGuid().ToString()}",
        MessageAttributes = messageAttributes
    };


    var response = await snsClient.PublishAsync(publishRequest);

    Console.WriteLine($"Message sent! Message ID: {response.MessageId}");
    
    Thread.Sleep(3000);
}