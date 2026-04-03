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

public class StudentRegisteredConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<StudentRegisteredConsumer> logger) : BackgroundService
{
    private const string QueueName = "student.registered";
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
                var evt = JsonSerializer.Deserialize<StudentRegisteredEvent>(json);
                if (evt is not null)
                {
                    logger.LogInformation("RABBITMQ CONSUMED: StudentRegisteredEvent for student {StudentId}", evt.StudentId);

                    using var scope = scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendWelcomeEmailAsync(evt.StudentEmail, evt.StudentName);
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RABBITMQ CONSUMER ERROR: StudentRegisteredConsumer");
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