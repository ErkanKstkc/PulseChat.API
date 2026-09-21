using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Friendships.Common;

public record FriendDto(
    Guid FriendshipId,
    Guid UserId,
    string Username,
    string? AvatarUrl,
    FriendshipStatus Status,
    bool IsRequester,
    DateTime CreatedAt
);
