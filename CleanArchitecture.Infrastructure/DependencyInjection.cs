using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Application.Settings;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Messaging;
using CleanArchitecture.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

        var cachingEnabled = configuration.GetValue<bool>("Caching:Enabled");

        if (cachingEnabled)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connectionString = config.GetConnectionString("Redis");
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("Redis connection string is missing.");

                var options = StackExchange.Redis.ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 1000;      
                options.SyncTimeout = 1000;      
                options.ReconnectRetryPolicy = new ExponentialRetry(5000);

                return StackExchange.Redis.ConnectionMultiplexer.Connect(options);
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, NoCacheService>();
        }

        // RabbitMQ
        var messagingEnabled = configuration.GetValue<bool>("Messaging:Enabled");

        if (messagingEnabled)
        {
            services.AddSingleton<IConnection>(_ =>
            {
                var host = configuration["Messaging:RabbitMq:Host"] ?? "localhost";
                var username = configuration["Messaging:RabbitMq:Username"] ?? "guest";
                var password = configuration["Messaging:RabbitMq:Password"] ?? "guest";

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    UserName = username,
                    Password = password
                };

                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddHostedService<EnrollmentCreatedConsumer>();
            services.AddHostedService<StudentRegisteredConsumer>();
            services.AddHostedService<EnrollmentCancelledConsumer>();
            services.AddHostedService<CourseUpdatedConsumer>();
            services.AddHostedService<AuditLogConsumer>();
        }
        else
        {
            services.AddScoped<IMessagePublisher, NoOpMessagePublisher>();
            services.AddScoped<IEmailService, EmailService>();
        }
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        if (cachingEnabled)
        {
            services.Decorate<ICourseService, CachedCourseService>();
            services.Decorate<IEnrollmentService, CachedEnrollmentService>();
            services.Decorate<IStudentService, CachedStudentService>();
        }

        return services;
    }
}