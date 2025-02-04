using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();


await channel.QueueDeclareAsync(queue: "priority_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var priority = ea.BasicProperties.Priority;

    Console.WriteLine($"Received {message} with priority {priority}");

    await Task.Delay(1000); // Simulate async processing time
};

await channel.BasicConsumeAsync(queue: "priority_queue",
    autoAck: true,
    consumer: consumer);

Console.WriteLine("Waiting for messages...");
Console.ReadKey();