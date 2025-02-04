using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

//declare a topic exchange
await channel.ExchangeDeclareAsync(exchange: "topic_logs", type:ExchangeType.Topic);

int i = 1;

string[] routingKeys = ["info", "warning", "error"];
string[] facilities = ["anonymous", "service1", "service2"];

var random = new Random();

while (true)
{
    var nextRandom = random.Next(0, 3);

    //form a composite routing key using a random string from the facilities and routingKeys arrays
    var routingKey = $"{facilities[nextRandom]}.{routingKeys[nextRandom]}";
    var body = Encoding.UTF8.GetBytes($"message-{i}");

    //publish the message to the exchange with a routing key
    await channel.BasicPublishAsync(exchange:"topic_logs", routingKey,  body: body );

    Console.WriteLine($"Sent message-{i}-{routingKey}");
    i++;
    Thread.Sleep(3000);
}