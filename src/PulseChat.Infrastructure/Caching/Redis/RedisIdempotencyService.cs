using PulseChat.Application.Common.Interfaces;
using StackExchange.Redis;

namespace PulseChat.Infrastructure.Caching.Redis;

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDatabase _db;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public RedisIdempotencyService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<bool> TryAcquireMessageLockAsync(string clientMessageId, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientMessageId))
        {
            return false;
        }

        var key = $"idempotency:msg:{clientMessageId}";
        // When.NotExists ensures atomic check-and-set
        return await _db.StringSetAsync(key, "1", expiry ?? DefaultExpiry, When.NotExists);
    }
}
