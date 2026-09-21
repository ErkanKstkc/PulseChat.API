namespace PulseChat.Application.Common.Interfaces;

public interface IPresenceService
{
    Task SetUserOnlineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);
    Task SetUserOfflineAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);
    Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserConnectionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetTypingAsync(string roomId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserTypingAsync(string roomId, Guid userId, CancellationToken cancellationToken = default);
}
