using System.Text;
using Microsoft.VisualBasic;
using RabbitMQ.Client;

var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Declare a queue with priority support
var queueDictionary = new Dictionary<string, object>
{
    { "x-max-priority", 5 } // Max priority level (0-5)
};

channel.QueueDeclareAsync(queue: "priority_queue", durable: true, exclusive: false, autoDelete: false, arguments: queueDictionary);

Random random = new ();

for (int i = 0; i < 10; i++)
{
    var message = $"Message {i}";
    var body = Encoding.UTF8.GetBytes(message);

    var properties = new BasicProperties();
    properties.Priority = Convert.ToByte(random.Next(0, 6)); // Assign priority between 0 and 5

    channel.BasicPublishAsync( exchange:string.Empty, routingKey: "priority_queue",false,basicProperties:properties,body:body);

    Console.WriteLine($"Sent {message} with priority {properties.Priority}");
}