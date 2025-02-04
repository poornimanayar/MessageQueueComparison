using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

//declare a direct exchange
await channel.ExchangeDeclareAsync(exchange: "direct_logs", type:ExchangeType.Direct);

int i = 1;
string[] routingKeys = ["info", "warning", "error"];

var random = new Random();

while (true)
{
    var routingKey = routingKeys[random.Next(0, 3)];
    var body = Encoding.UTF8.GetBytes($"message-{i}");

    //publish the message to the exchange with a routing key
    await channel.BasicPublishAsync(exchange:"direct_logs", routingKey,  body: body );

    Console.WriteLine($"Sent message-{i}-{routingKey}");
    i++;

    Thread.Sleep(3000);
}