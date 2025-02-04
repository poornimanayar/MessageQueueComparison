using RabbitMQ.Client;
using System.Text;

var connectionFactory = new ConnectionFactory { HostName = "localhost", UserName = "rabbitmq", Password = "rabbitmq" };
using var connection = await connectionFactory.CreateConnectionAsync();

using var channel = await connection.CreateChannelAsync();

// Declare a queue
await channel.QueueDeclareAsync(queue: "transaction_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

// Enable transaction mode
await channel.TxSelectAsync();

try
{
    string message = "Hello, Transaction!";
    var body = Encoding.UTF8.GetBytes(message);

    // Publish a message within the transaction
    await channel.BasicPublishAsync(string.Empty, "transaction_queue", body: body);

    message = "Hello, Transaction! again";
    body = Encoding.UTF8.GetBytes(message);

    // Publish a message within the transaction
    await channel.BasicPublishAsync(string.Empty, "transaction_queue", body: body);

    message = "Hello, Transaction! yet another time";
    body = Encoding.UTF8.GetBytes(message);

    // Publish a message within the transaction
    await channel.BasicPublishAsync(string.Empty, "transaction_queue", body: body);

    Thread.Sleep(20000);

    // Commit the transaction
    await channel.TxCommitAsync();
    Console.WriteLine("Transaction committed. Message sent.");
}
catch (Exception ex)
{
    // Rollback in case of failure
    await channel.TxRollbackAsync();
    Console.WriteLine($"Transaction rolled back due to error: {ex.Message}");
}
