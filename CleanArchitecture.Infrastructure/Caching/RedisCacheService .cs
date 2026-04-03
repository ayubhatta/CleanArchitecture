using CleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using RedisDatabase = StackExchange.Redis.IDatabase;

namespace CleanArchitecture.Infrastructure.Caching;

public class RedisCacheService(
    IConnectionMultiplexer redis,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly RedisDatabase _database = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisException)
        {
            logger.LogWarning(ex, "Redis GET failed for key '{Key}'. Falling back to source.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisException)
        {
            logger.LogWarning(ex, "Redis SET failed for key '{Key}'. Cache write skipped.", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisException)
        {
            logger.LogWarning(ex, "Redis DELETE failed for key '{Key}'. Invalidation skipped.", key);
        }
    }
}