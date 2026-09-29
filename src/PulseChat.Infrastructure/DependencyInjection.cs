using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Infrastructure.Caching.Redis;
using PulseChat.Infrastructure.Messaging;
using PulseChat.Infrastructure.Persistence.Mongo;
using PulseChat.Infrastructure.Persistence.Postgres;
using PulseChat.Infrastructure.Security;
using PulseChat.Infrastructure.Storage.Minio;
using StackExchange.Redis;

namespace PulseChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        // 1. PostgreSQL EF Core
        var postgresConnection = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5433;Database=pulsechat_db;Username=pulsechat_user;Password=pulsechat_password";

        services.AddDbContext<PulseChatDbContext>(options =>
            options.UseNpgsql(postgresConnection));

        services.AddScoped<IPulseChatDbContext>(sp => sp.GetRequiredService<PulseChatDbContext>());

        // 2. MongoDB
        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IMongoChatRepository, MongoChatRepository>();

        // 3. Redis
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisSettings.ConnectionString));

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IPresenceService, RedisPresenceService>();
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        // 4. MinIO Storage
        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.AddScoped<IStorageService, MinioStorageService>();

        // 5. Security & Tokens
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // 6. MassTransit / RabbitMQ
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        var rabbitSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitSettings.Host, rabbitSettings.Port, "/", h =>
                {
                    h.Username(rabbitSettings.Username);
                    h.Password(rabbitSettings.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
