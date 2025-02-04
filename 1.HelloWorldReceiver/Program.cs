using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory{HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq"};
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync("hello",durable:false,autoDelete: false,arguments: null, exclusive: false);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);

//provide a callback for the messages pushed asynchronously from the server
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
    await Task.CompletedTask;
};

//start a consumer and never stop it
await channel.BasicConsumeAsync("hello", autoAck: true, consumer: consumer, consumerTag:"Hello World Receiver");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();