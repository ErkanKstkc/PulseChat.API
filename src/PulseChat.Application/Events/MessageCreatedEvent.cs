using PulseChat.Domain.Enums;

namespace PulseChat.Application.Events;

public record MessageCreatedEvent(
    string RoomId,
    Guid SenderId,
    string ClientMessageId,
    MessageType Type,
    string Content,
    string? MediaUrl,
    DateTime CreatedAt
);
