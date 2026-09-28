using System.Security.Claims;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using PulseChat.API.Hubs;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Events;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;
using Xunit;

namespace PulseChat.UnitTests.Hubs;

public class ChatHubTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<IMongoChatRepository> _chatRepositoryMock = new();
    private readonly Mock<ILogger<ChatHub>> _loggerMock = new();

    private readonly Mock<HubCallerContext> _hubCallerContextMock = new();
    private readonly Mock<IHubCallerClients> _clientsMock = new();
    private readonly Mock<IGroupManager> _groupsMock = new();
    private readonly Mock<ISingleClientProxy> _callerProxyMock = new();
    private readonly Mock<IClientProxy> _groupProxyMock = new();
    private readonly Mock<IClientProxy> _othersInGroupProxyMock = new();

    private readonly Guid _currentUserId = Guid.NewGuid();
    private const string ConnectionId = "conn_12345";

    private ChatHub CreateHubWithMocks()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
            new(ClaimTypes.Name, "test_alice")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _hubCallerContextMock.Setup(c => c.ConnectionId).Returns(ConnectionId);
        _hubCallerContextMock.Setup(c => c.User).Returns(claimsPrincipal);

        _clientsMock.Setup(c => c.Caller).Returns(_callerProxyMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupProxyMock.Object);
        _clientsMock.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(_othersInGroupProxyMock.Object);

        return new ChatHub(
            _publishEndpointMock.Object,
            _idempotencyServiceMock.Object,
            _presenceServiceMock.Object,
            _chatRepositoryMock.Object,
            _loggerMock.Object
        )
        {
            Context = _hubCallerContextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object
        };
    }

    [Fact]
    public async Task JoinRoom_WhenUserIsNotMember_ShouldReturnForbiddenFailure()
    {
        // Arrange
        const string roomId = "room_unauthorized";
        _chatRepositoryMock
            .Setup(r => r.IsUserInRoomAsync(roomId, _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var hub = CreateHubWithMocks();

        // Act
        var result = await hub.JoinRoom(roomId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotRoomMember);
        _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task JoinRoom_WhenUserIsMember_ShouldAddConnectionToGroup()
    {
        // Arrange
        const string roomId = "room_authorized";
        _chatRepositoryMock
            .Setup(r => r.IsUserInRoomAsync(roomId, _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _groupsMock
            .Setup(g => g.AddToGroupAsync(ConnectionId, roomId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = CreateHubWithMocks();

        // Act
        var result = await hub.JoinRoom(roomId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _groupsMock.Verify(g => g.AddToGroupAsync(ConnectionId, roomId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WhenDuplicateClientMessageId_ShouldIgnoreAndNotPublish()
    {
        // Arrange
        var payload = new SendMessagePayload(
            RoomId: "room_1",
            ClientMessageId: "duplicate_msg_id",
            Content: "Tekrarlanan mesaj"
        );

        _idempotencyServiceMock
            .Setup(i => i.TryAcquireMessageLockAsync("duplicate_msg_id", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Duplicate!

        var hub = CreateHubWithMocks();

        // Act
        var result = await hub.SendMessage(payload);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Idempotent success
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<MessageCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _groupProxyMock.Verify(g => g.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessage_WhenValid_ShouldBroadcastAndPublishEventToRabbitMq()
    {
        // Arrange
        var payload = new SendMessagePayload(
            RoomId: "room_pulse",
            ClientMessageId: "unique_client_id_999",
            Content: "Selam ekip!",
            Type: MessageType.Text
        );

        _idempotencyServiceMock
            .Setup(i => i.TryAcquireMessageLockAsync("unique_client_id_999", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _chatRepositoryMock
            .Setup(r => r.IsUserInRoomAsync("room_pulse", _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _publishEndpointMock
            .Setup(p => p.Publish(It.IsAny<MessageCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = CreateHubWithMocks();

        // Act
        var result = await hub.SendMessage(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verifies broadcast to Group
        _clientsMock.Verify(c => c.Group("room_pulse"), Times.Once);

        // Verifies ACK to Caller
        _clientsMock.Verify(c => c.Caller, Times.Once);

        // Verifies RabbitMQ Event Published
        _publishEndpointMock.Verify(
            p => p.Publish(It.Is<MessageCreatedEvent>(e =>
                e.RoomId == "room_pulse" &&
                e.SenderId == _currentUserId &&
                e.ClientMessageId == "unique_client_id_999" &&
                e.Content == "Selam ekip!"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendTyping_ShouldSetPresenceAndBroadcastToGroup()
    {
        // Arrange
        const string roomId = "room_typing_test";
        _presenceServiceMock
            .Setup(p => p.SetTypingAsync(roomId, _currentUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = CreateHubWithMocks();

        // Act
        await hub.SendTyping(roomId);

        // Assert
        _presenceServiceMock.Verify(p => p.SetTypingAsync(roomId, _currentUserId, It.IsAny<CancellationToken>()), Times.Once);
        _clientsMock.Verify(c => c.OthersInGroup(roomId), Times.Once);
    }
}
