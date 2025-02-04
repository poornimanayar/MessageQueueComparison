using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

//declare an exchange
await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

int i = 1;

while (true)
{
    var body = Encoding.UTF8.GetBytes($"message-{i}");
    
    //published to named exchange, routing key is ignored
    await channel.BasicPublishAsync(exchange: "logs", string.Empty, body: body);
    Console.WriteLine($"Sent message-{i}");
    i++;
}