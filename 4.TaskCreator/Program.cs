using System.Text;
using RabbitMQ.Client;

var connectionFactory = new ConnectionFactory{HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq"};
using var connection = await connectionFactory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

//declare a durable queue so that the queue will survive a broker restart
//this does not have any effect on the published messages   
await channel.QueueDeclareAsync("taskqueue",durable:true,autoDelete: false,arguments: null, exclusive: false);

int i = 1;

while (i < 100)
{
    var body = Encoding.UTF8.GetBytes($"message-{i}");

    //set the delivery mode to persistent so that the message will survive a broker restart
    var basicProperties = new BasicProperties() { Persistent = true };

    //routing key matches the queue name, used to route the message to the correct queue
    await channel.BasicPublishAsync(exchange:string.Empty, "taskqueue", mandatory:true, body: body, basicProperties: basicProperties );

    Console.WriteLine($"Send message message-{i}");
    i++;
    await Task.Delay(1000);
}