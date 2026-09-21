using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PulseChat.Domain.Enums;

namespace PulseChat.Domain.Documents;

public class MessageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string RoomId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public Guid SenderId { get; set; }

    public string ClientMessageId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public MessageType Type { get; set; } = MessageType.Text;

    public string Content { get; set; } = string.Empty;

    public string? MediaUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
