using Common.Dtos;
using Common.Interfaces;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace rabbit_test.Services
{
    public class RabbitMqPublisher : IMessageBroker
    {
        private readonly ConnectionFactory _factory;
        private readonly string _queueName = "demo-queue";

        public RabbitMqPublisher()
        {
            _factory = new ConnectionFactory() { HostName = "localhost" };
        }

        public async Task Publish(MessageDto message)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Declare the queue
            await channel.QueueDeclareAsync(queue: _queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(exchange: "",
                                 routingKey: _queueName,
                                 mandatory: true,
                                 basicProperties: new BasicProperties(),
                                 body: body);

            Console.WriteLine($"[Publisher] Sent: {json}");
        }

        // Not used on the publisher side.
        public Task Subscribe(Action<MessageDto> onMessageReceived) =>
            throw new NotImplementedException("Subscribe is not implemented in Publisher.");
    }
}
