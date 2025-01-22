using Common.Dtos;
using Common.Interfaces;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ConsumerApp.Services
{
    public class RabbitMqConsumerService : BackgroundService, IMessageBroker
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _queueName = "demo-queue";
        private readonly Action<MessageDto> _onMessageReceived;

        public RabbitMqConsumerService(Action<MessageDto> onMessageReceived)
        {
            // Set AMQP connection parameters here.
            _factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMq__HostName") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RabbitMq__UserName") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "guest",
                Port = 5672 // Default AMQP port
            };

            _onMessageReceived = onMessageReceived;
        }

        // Not implemented for the consumer
        public Task Publish(MessageDto message) =>
            throw new NotImplementedException("Publish is not implemented in Consumer.");

        // Not implemented since we're starting the consumer on startup
        public Task Subscribe(Action<MessageDto> onMessageReceived) =>
            throw new NotImplementedException("Subscribe is not implemented in Consumer. Use the background service.");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create connection and channel asynchronously.
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare the queue; similar to the publisher declaration.
            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Use an AsyncEventingBasicConsumer for asynchronous event handling.
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<MessageDto>(json);
                Console.WriteLine($"[Consumer] Received: {json}");

                if (message is not null)
                {
                    _onMessageReceived(message);
                }

                // Since autoAck is true, acknowledgment is automatically handled.
                await Task.Yield();
            };

            // Start consuming messages asynchronously.
            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            // Keep the background service running until a cancellation is requested.
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        public override void Dispose()
        {
            // Cleanly close the channel and connection.
            // Note: In asynchronous code, consider using IAsyncDisposable if needed.
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }
}
