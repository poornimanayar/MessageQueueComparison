using RabbitMQ.Client;

//connecting to a local instance of RabbitMQ https://www.rabbitmq.com/docs/download#docker
var connectionFactory = new ConnectionFactory{HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq"};

//Abstracts socket connection to RabbitMQ, protocol version negotiation, and authentication
using var connection = await connectionFactory.CreateConnectionAsync();

using var channel = await connection.CreateChannelAsync();

//declare a queue, idempotent operation
await channel.QueueDeclareAsync("hello",durable:false,autoDelete: false,arguments: null, exclusive: false);

//message body is byte array
var body = "Hello World"u8.ToArray();

await channel.BasicPublishAsync(string.Empty, "hello", body: body);

for (int i = 0; i < 10; i++)
{
    Thread.Sleep(2000);
    Console.WriteLine($" [x] Sent 'hello' to {0}", channel);
}

Console.ReadLine();