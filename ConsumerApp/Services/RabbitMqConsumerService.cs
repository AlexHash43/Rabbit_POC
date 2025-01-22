using System.Text;
using System.Text.Json;
using Common.Dtos;
using Common.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerApp.Services
{
    public class RabbitMqConsumerService : BackgroundService, IMessageBroker, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _queueName = "demo-queue";
        private readonly Action<MessageDto> _onMessageReceived;

        public RabbitMqConsumerService(Action<MessageDto> onMessageReceived)
        {
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
            try
            {
                // Create connection and channel asynchronously.
                _connection = await _factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                Console.WriteLine("[Consumer] Connected to RabbitMQ");

                // Declare the queue; similar to the publisher declaration.
                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                Console.WriteLine($"[Consumer] Declared queue: {_queueName}");

                // Use an AsyncEventingBasicConsumer for asynchronous message handling.
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<MessageDto>(json);
                        Console.WriteLine($"[Consumer] Received: {json}");

                        if (message is not null)
                        {
                            _onMessageReceived(message);
                            Console.WriteLine($"[Consumer] Processed message: {message.Id}");
                        }

                        // Manually acknowledge the message
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Consumer] Error processing message: {ex.Message}");
                        // Optionally, reject the message and requeue it
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                // Start consuming messages asynchronously.
                await _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                Console.WriteLine("[Consumer] Started consuming messages.");

                // Keep the background service running until cancellation is requested.
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] Exception in ExecuteAsync: {ex.Message}");
            }
        }

        // Explicit implementation of IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("[Consumer] Disposing resources asynchronously...");

            if (_channel is not null)
            {
                try
                {
                    await _channel.CloseAsync(); // Close asynchronously
                    Console.WriteLine("[Consumer] Channel closed asynchronously.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Exception while closing channel: {ex.Message}");
                }
                finally
                {
                    _channel.Dispose();
                }
            }

            if (_connection is not null)
            {
                try
                {
                    await _connection.CloseAsync();
                    Console.WriteLine("[Consumer] Connection closed asynchronously.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Exception while closing connection: {ex.Message}");
                }
                finally
                {
                    _connection.Dispose();
                }
            }

            // Ensure any additional cleanup (if needed) is done via the synchronous Dispose
            this.Dispose();
            Console.WriteLine("[Consumer] Asynchronous disposal completed.");
        }
    }
}
