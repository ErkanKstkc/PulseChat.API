using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Chat.Common;

public record RoomMemberDto(
    Guid UserId,
    string? Username,
    string? AvatarUrl,
    RoomRole Role,
    DateTime JoinedAt,
    DateTime? LastReadAt
);

public record RoomDto(
    string Id,
    RoomType Type,
    string Title,
    string? AvatarUrl,
    Guid CreatedBy,
    DateTime CreatedAt,
    List<RoomMemberDto> Members
);

public record MessageDto(
    string Id,
    string RoomId,
    Guid SenderId,
    string? SenderUsername,
    string ClientMessageId,
    MessageType Type,
    string Content,
    string? MediaUrl,
    DateTime CreatedAt
);
