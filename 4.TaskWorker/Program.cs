using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();


await channel.QueueDeclareAsync("taskqueue", durable: true, autoDelete: false, arguments: null, exclusive: false);

//for fair dispatch
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    int number = int.Parse(message.Split('-')[1]);

    Console.WriteLine($" [x] Received {message}");

    //add a delay; delay seconds = 2s for even, 3s for odd in the message; simulate a task
    await Task.Delay(number % 2 == 0 ? 2000 : 3000);

    //explicitly acknowledge the message; protect against message loss from worker crash
    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
};

await channel.BasicConsumeAsync("taskqueue", autoAck: false, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();