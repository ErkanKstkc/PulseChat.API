using MongoDB.Driver;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Documents;

namespace PulseChat.Infrastructure.Persistence.Mongo;

public class MongoChatRepository : IMongoChatRepository
{
    private readonly MongoDbContext _context;

    public MongoChatRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task CreateRoomAsync(RoomDocument room, CancellationToken cancellationToken = default)
    {
        await _context.Rooms.InsertOneAsync(room, cancellationToken: cancellationToken);
    }

    public async Task<RoomDocument?> GetRoomByIdAsync(string roomId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .Find(r => r.Id == roomId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<RoomDocument>> GetUserRoomsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RoomDocument>.Filter.ElemMatch(r => r.Members, m => m.UserId == userId);
        return await _context.Rooms
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserInRoomAsync(string roomId, Guid userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RoomDocument>.Filter.And(
            Builders<RoomDocument>.Filter.Eq(r => r.Id, roomId),
            Builders<RoomDocument>.Filter.ElemMatch(r => r.Members, m => m.UserId == userId)
        );

        return await _context.Rooms.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task AddMemberToRoomAsync(string roomId, RoomMemberDocument member, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RoomDocument>.Filter.Eq(r => r.Id, roomId);
        var update = Builders<RoomDocument>.Update.Push(r => r.Members, member);
        await _context.Rooms.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task UpdateMemberLastReadAsync(string roomId, Guid userId, DateTime lastReadAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RoomDocument>.Filter.And(
            Builders<RoomDocument>.Filter.Eq(r => r.Id, roomId),
            Builders<RoomDocument>.Filter.ElemMatch(r => r.Members, m => m.UserId == userId)
        );

        var update = Builders<RoomDocument>.Update.Set("Members.$.LastReadAt", lastReadAt);
        await _context.Rooms.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string roomId, Guid userId, DateTime? lastReadAt, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<MessageDocument>.Filter;
        var filter = filterBuilder.Eq(m => m.RoomId, roomId) & filterBuilder.Ne(m => m.SenderId, userId);

        if (lastReadAt.HasValue)
        {
            filter &= filterBuilder.Gt(m => m.CreatedAt, lastReadAt.Value);
        }

        var count = await _context.Messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return (int)count;
    }

    public async Task<MessageDocument?> GetLatestMessageAsync(string roomId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Find(m => m.RoomId == roomId)
            .SortByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveMessageAsync(MessageDocument message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }

    public async Task<List<MessageDocument>> GetRoomMessagesAsync(string roomId, int limit = 50, DateTime? before = null, CancellationToken cancellationToken = default)
    {
        var builder = Builders<MessageDocument>.Filter;
        var filter = builder.Eq(m => m.RoomId, roomId);

        if (before.HasValue)
        {
            filter &= builder.Lt(m => m.CreatedAt, before.Value);
        }

        return await _context.Messages
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }
}
