using PulseChat.Domain.Enums;

namespace PulseChat.Domain.Documents;

public class RoomMemberDocument
{
    public Guid UserId { get; set; }
    public RoomRole Role { get; set; } = RoomRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
}
