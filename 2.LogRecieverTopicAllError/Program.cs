using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: "topic_logs", type:ExchangeType.Topic);

QueueDeclareOk queueDeclare = await channel.QueueDeclareAsync();
string queueName = queueDeclare.QueueName;

//recieve all error logs from any facility
await channel.QueueBindAsync(queue: queueName, exchange: "topic_logs", routingKey: "*.error");

Console.WriteLine("Waiting for logs... ");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}-{ea.RoutingKey}");
    return Task.CompletedTask;
};

await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();