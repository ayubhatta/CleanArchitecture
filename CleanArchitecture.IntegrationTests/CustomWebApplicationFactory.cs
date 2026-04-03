using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace CleanArchitecture.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true ||
                    (d.ServiceType == typeof(AppDbContext)) ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_"));

            services.RemoveAll<ICacheService>();
            services.RemoveAll(typeof(StackExchange.Redis.IConnectionMultiplexer));
            services.AddScoped<ICacheService, NoCacheService>();

            services.RemoveAll<IMessagePublisher>();
            services.RemoveAll<RabbitMQ.Client.IConnection>();
            var mockPublisher = new Mock<IMessagePublisher>();
            mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
                         .Returns(Task.CompletedTask);
            services.AddScoped<IMessagePublisher>(_ => mockPublisher.Object);

            services.RemoveAll<IEmailService>();
            var mockEmail = new Mock<IEmailService>();
            services.AddScoped<IEmailService>(_ => mockEmail.Object);

            var hostedServices = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var descriptor in hostedServices)
                services.Remove(descriptor);
        });

        builder.UseEnvironment("Testing");
    }
}