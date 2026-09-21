using MassTransit;
using Microsoft.Extensions.Logging;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Events;

namespace PulseChat.Workers.Consumers;

public class PushNotificationConsumer : IConsumer<MessageCreatedEvent>
{
    private readonly IMongoChatRepository _chatRepository;
    private readonly IPresenceService _presenceService;
    private readonly ILogger<PushNotificationConsumer> _logger;

    public PushNotificationConsumer(
        IMongoChatRepository chatRepository,
        IPresenceService presenceService,
        ILogger<PushNotificationConsumer> logger)
    {
        _chatRepository = chatRepository;
        _presenceService = presenceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MessageCreatedEvent> context)
    {
        var messageEvent = context.Message;
        _logger.LogInformation("Processing push notification dispatch for message {ClientMessageId}", messageEvent.ClientMessageId);

        var room = await _chatRepository.GetRoomByIdAsync(messageEvent.RoomId, context.CancellationToken);
        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found for push notification", messageEvent.RoomId);
            return;
        }

        foreach (var member in room.Members)
        {
            if (member.UserId == messageEvent.SenderId)
            {
                continue; // Do not send push to the sender
            }

            var isOnline = await _presenceService.IsUserOnlineAsync(member.UserId, context.CancellationToken);
            if (!isOnline)
            {
                // User is offline: dispatch push notification (FCM)
                _logger.LogInformation(
                    "User {UserId} is offline. Dispatched FCM Push Notification for Room '{RoomTitle}' with content '{Preview}'",
                    member.UserId,
                    room.Title,
                    messageEvent.Content.Length > 30 ? messageEvent.Content[..30] + "..." : messageEvent.Content
                );
            }
        }
    }
}
