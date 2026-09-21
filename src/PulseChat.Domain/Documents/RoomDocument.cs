using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PulseChat.Domain.Enums;

namespace PulseChat.Domain.Documents;

public class RoomDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public RoomType Type { get; set; } = RoomType.Direct;

    public string Title { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RoomMemberDocument> Members { get; set; } = new();
}
