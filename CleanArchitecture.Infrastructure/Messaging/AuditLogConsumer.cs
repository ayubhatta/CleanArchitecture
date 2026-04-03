using System.Text;
using System.Text.Json;
using CleanArchitecture.Application.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CleanArchitecture.Infrastructure.Messaging;

public class AuditLogConsumer(
    IConnection connection,
    ILogger<AuditLogConsumer> logger) : BackgroundService
{
    private const string QueueName = "audit.log";
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var evt = JsonSerializer.Deserialize<AuditEvent>(json);
                if (evt is not null)
                {
                    logger.LogInformation(
                        "AUDIT: Entity={Entity}, Action={Action}, Id={Id}, Time={Time}",
                        evt.Entity, evt.Action, evt.EntityId, evt.Timestamp);
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RABBITMQ CONSUMER ERROR: AuditLogConsumer");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}