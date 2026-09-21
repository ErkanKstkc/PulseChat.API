using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PulseChat.Domain.Documents;

namespace PulseChat.Infrastructure.Persistence.Mongo;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<RoomDocument> Rooms =>
        _database.GetCollection<RoomDocument>("rooms");

    public IMongoCollection<MessageDocument> Messages =>
        _database.GetCollection<MessageDocument>("messages");

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Compound Index: room_id + created_at (descending for history queries)
        var messageCompoundIndex = new CreateIndexModel<MessageDocument>(
            Builders<MessageDocument>.IndexKeys
                .Ascending(m => m.RoomId)
                .Descending(m => m.CreatedAt),
            new CreateIndexOptions { Name = "idx_messages_room_created_at" }
        );

        // 2. Unique Index: client_message_id (idempotency key)
        var messageIdempotencyIndex = new CreateIndexModel<MessageDocument>(
            Builders<MessageDocument>.IndexKeys
                .Ascending(m => m.ClientMessageId),
            new CreateIndexOptions { Unique = true, Name = "idx_messages_client_message_id_unique" }
        );

        // 3. Sender Index
        var messageSenderIndex = new CreateIndexModel<MessageDocument>(
            Builders<MessageDocument>.IndexKeys
                .Ascending(m => m.SenderId),
            new CreateIndexOptions { Name = "idx_messages_sender_id" }
        );

        await Messages.Indexes.CreateManyAsync([messageCompoundIndex, messageIdempotencyIndex, messageSenderIndex], cancellationToken);

        // 4. Room Members User Id index
        var roomMemberIndex = new CreateIndexModel<RoomDocument>(
            Builders<RoomDocument>.IndexKeys.Ascending("Members.UserId"),
            new CreateIndexOptions { Name = "idx_rooms_members_user_id" }
        );

        await Rooms.Indexes.CreateOneAsync(roomMemberIndex, cancellationToken: cancellationToken);
    }
}
