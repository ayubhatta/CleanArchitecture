using System.Text;
using System.Text.Json;
using CleanArchitecture.Application.Events;
using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CleanArchitecture.Infrastructure.Messaging;

public class EnrollmentCreatedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<EnrollmentCreatedConsumer> logger) : BackgroundService
{
    private const string QueueName = "enrollment.created";
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

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            try
            {
                var evt = JsonSerializer.Deserialize<EnrollmentCreatedEvent>(json);

                if (evt is not null)
                {
                    logger.LogInformation(
                        "RABBITMQ CONSUMED: EnrollmentCreatedEvent for student {StudentId}, course {CourseId}",
                        evt.StudentId, evt.CourseId);

                    using var scope = scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendEnrollmentConfirmationAsync(
                        evt.StudentEmail,
                        evt.StudentName,
                        evt.CourseName);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RABBITMQ CONSUMER ERROR: failed to process EnrollmentCreatedEvent");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}