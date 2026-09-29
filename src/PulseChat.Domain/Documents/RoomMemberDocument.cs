using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PulseChat.Domain.Enums;

namespace PulseChat.Domain.Documents;

public class RoomMemberDocument
{
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public RoomRole Role { get; set; } = RoomRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastReadAt { get; set; }
}
