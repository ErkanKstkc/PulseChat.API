using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Events;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;

namespace PulseChat.API.Hubs;

public record SendMessagePayload(
    string RoomId,
    string ClientMessageId,
    string Content,
    MessageType Type = MessageType.Text,
    string? MediaUrl = null
);

[Authorize]
public class ChatHub : Hub
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IPresenceService _presenceService;
    private readonly IMongoChatRepository _chatRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IPublishEndpoint publishEndpoint,
        IIdempotencyService idempotencyService,
        IPresenceService presenceService,
        IMongoChatRepository chatRepository,
        ILogger<ChatHub> logger)
    {
        _publishEndpoint = publishEndpoint;
        _idempotencyService = idempotencyService;
        _presenceService = presenceService;
        _chatRepository = chatRepository;
        _logger = logger;
    }

    private Guid CurrentUserId
    {
        get
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                        ?? Context.User?.FindFirst("sub");
            return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;
        if (userId != Guid.Empty)
        {
            await _presenceService.SetUserOnlineAsync(userId, Context.ConnectionId);
            _logger.LogInformation("User {UserId} connected with ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

            // Broadcast presence to others
            await Clients.Others.SendAsync("UserPresenceChanged", new
            {
                UserId = userId,
                Status = "online",
                Timestamp = DateTime.UtcNow
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        if (userId != Guid.Empty)
        {
            await _presenceService.SetUserOfflineAsync(userId, Context.ConnectionId);
            _logger.LogInformation("User {UserId} disconnected (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);

            var isStillOnline = await _presenceService.IsUserOnlineAsync(userId);
            if (!isStillOnline)
            {
                await Clients.Others.SendAsync("UserPresenceChanged", new
                {
                    UserId = userId,
                    Status = "offline",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Result> JoinRoom(string roomId)
    {
        var isMember = await _chatRepository.IsUserInRoomAsync(roomId, CurrentUserId);
        if (!isMember)
        {
            return Result.Failure(ErrorCodes.NotRoomMember, "Bu odaya katılma yetkiniz yok.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("User {UserId} joined SignalR group {RoomId}", CurrentUserId, roomId);
        return Result.Success();
    }

    public async Task<Result> LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        return Result.Success();
    }

    public async Task<Result> SendMessage(SendMessagePayload payload)
    {
        var senderId = CurrentUserId;
        if (senderId == Guid.Empty)
        {
            return Result.Failure(ErrorCodes.Unauthorized, "Kimlik doğrulaması gereklidir.");
        }

        // 1. Idempotency check via Redis
        var isNewMessage = await _idempotencyService.TryAcquireMessageLockAsync(payload.ClientMessageId);
        if (!isNewMessage)
        {
            _logger.LogWarning("Duplicate message ignored for ClientMessageId: {ClientMessageId}", payload.ClientMessageId);
            return Result.Success(); // Mükerrer istek sessizce onaylanır
        }

        // 2. Room membership check
        var isMember = await _chatRepository.IsUserInRoomAsync(payload.RoomId, senderId);
        if (!isMember)
        {
            return Result.Failure(ErrorCodes.NotRoomMember, "Bu odaya mesaj gönderme yetkiniz yok.");
        }

        var createdAt = DateTime.UtcNow;

        var messageBroadcast = new
        {
            RoomId = payload.RoomId,
            SenderId = senderId,
            SenderUsername = Context.User?.Identity?.Name,
            ClientMessageId = payload.ClientMessageId,
            Type = payload.Type.ToString(),
            Content = payload.Content,
            MediaUrl = payload.MediaUrl,
            CreatedAt = createdAt
        };

        // 3. Live In-Memory Delivery (SignalR Group Broadcast)
        await Clients.Group(payload.RoomId).SendAsync("ReceiveMessage", messageBroadcast);

        // 4. Delivery ACK to Sender
        await Clients.Caller.SendAsync("MessageDeliveredAck", new
        {
            ClientMessageId = payload.ClientMessageId,
            DeliveredAt = createdAt
        });

        // 5. Asynchronous Event Publishing to RabbitMQ
        await _publishEndpoint.Publish(new MessageCreatedEvent(
            payload.RoomId,
            senderId,
            payload.ClientMessageId,
            payload.Type,
            payload.Content,
            payload.MediaUrl,
            createdAt
        ));

        return Result.Success();
    }

    public async Task SendTyping(string roomId)
    {
        var senderId = CurrentUserId;
        if (senderId != Guid.Empty)
        {
            await _presenceService.SetTypingAsync(roomId, senderId);
            await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new
            {
                RoomId = roomId,
                UserId = senderId,
                Username = Context.User?.Identity?.Name
            });
        }
    }
}
