using CleanArchitecture.Application.Interfaces;

namespace CleanArchitecture.Infrastructure.Messaging;

public class NoOpMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<T>(string queueName, T message) => Task.CompletedTask;
}