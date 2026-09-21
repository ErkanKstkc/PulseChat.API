using PulseChat.Domain.Documents;

namespace PulseChat.Application.Common.Interfaces;

public interface IMongoChatRepository
{
    Task CreateRoomAsync(RoomDocument room, CancellationToken cancellationToken = default);
    Task<RoomDocument?> GetRoomByIdAsync(string roomId, CancellationToken cancellationToken = default);
    Task<List<RoomDocument>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserInRoomAsync(string roomId, Guid userId, CancellationToken cancellationToken = default);
    Task AddMemberToRoomAsync(string roomId, RoomMemberDocument member, CancellationToken cancellationToken = default);
    Task UpdateMemberLastReadAsync(string roomId, Guid userId, DateTime lastReadAt, CancellationToken cancellationToken = default);
    Task SaveMessageAsync(MessageDocument message, CancellationToken cancellationToken = default);
    Task<List<MessageDocument>> GetRoomMessagesAsync(string roomId, int limit = 50, DateTime? before = null, CancellationToken cancellationToken = default);
}
