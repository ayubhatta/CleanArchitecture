using System.Text;
using System.Text.Json;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CleanArchitecture.Infrastructure.Messaging;

public class RabbitMqPublisher(
    IConnection connection,
    ILogger<RabbitMqPublisher> logger) : IMessagePublisher
{
    public async Task PublishAsync<T>(string queueName, T message)
    {
        try
        {
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            logger.LogInformation("RABBITMQ PUBLISHED: queue={Queue}, type={Type}", queueName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RABBITMQ PUBLISH FAILED: queue={Queue}", queueName);
        }
    }
}