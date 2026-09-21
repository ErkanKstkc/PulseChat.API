using System.Text.Json;
using PulseChat.Application.Common.Interfaces;
using StackExchange.Redis;

namespace PulseChat.Infrastructure.Caching.Redis;

public class RedisPresenceService : IPresenceService
{
    private readonly IDatabase _db;

    public RedisPresenceService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string UserStatusKey(Guid userId) => $"presence:user:{userId}";
    private static string ConnectionKey(string connectionId) => $"presence:conn:{connectionId}";
    private static string UserConnectionsKey(Guid userId) => $"presence:user_conns:{userId}";
    private static string TypingKey(string roomId, Guid userId) => $"typing:room:{roomId}:user:{userId}";

    public async Task SetUserOnlineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var status = new { status = "online", lastSeen = DateTime.UtcNow };
        var statusJson = JsonSerializer.Serialize(status);

        var batch = _db.CreateBatch();
        var t1 = batch.StringSetAsync(UserStatusKey(userId), statusJson);
        var t2 = batch.StringSetAsync(ConnectionKey(connectionId), userId.ToString(), TimeSpan.FromDays(1));
        var t3 = batch.SetAddAsync(UserConnectionsKey(userId), connectionId);
        batch.Execute();

        await Task.WhenAll(t1, t2, t3);
    }

    public async Task SetUserOfflineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        await _db.SetRemoveAsync(UserConnectionsKey(userId), connectionId);
        await _db.KeyDeleteAsync(ConnectionKey(connectionId));

        var remainingConnections = await _db.SetLengthAsync(UserConnectionsKey(userId));
        if (remainingConnections == 0)
        {
            var status = new { status = "offline", lastSeen = DateTime.UtcNow };
            var statusJson = JsonSerializer.Serialize(status);
            await _db.StringSetAsync(UserStatusKey(userId), statusJson, TimeSpan.FromDays(30));
        }
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connectionCount = await _db.SetLengthAsync(UserConnectionsKey(userId));
        return connectionCount > 0;
    }

    public async Task<IReadOnlyList<string>> GetUserConnectionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(UserConnectionsKey(userId));
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task SetTypingAsync(string roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _db.StringSetAsync(TypingKey(roomId, userId), "1", TimeSpan.FromSeconds(5));
    }

    public async Task<bool> IsUserTypingAsync(string roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(TypingKey(roomId, userId));
    }
}
