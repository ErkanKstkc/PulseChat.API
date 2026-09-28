using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Events;
using PulseChat.Domain.Documents;
using PulseChat.Domain.Enums;
using PulseChat.Workers.Consumers;
using Xunit;

namespace PulseChat.UnitTests.Workers;

public class WorkerConsumersTests
{
    private readonly Mock<IMongoChatRepository> _chatRepositoryMock = new();
    private readonly Mock<IPresenceService> _presenceServiceMock = new();
    private readonly Mock<ILogger<MongoMessagePersistenceConsumer>> _mongoLoggerMock = new();
    private readonly Mock<ILogger<PushNotificationConsumer>> _pushLoggerMock = new();

    [Fact]
    public async Task MongoConsumer_ShouldPersistMessageSuccessfully()
    {
        // Arrange
        var messageEvent = new MessageCreatedEvent(
            RoomId: "room_1",
            SenderId: Guid.NewGuid(),
            ClientMessageId: "client_uuid_123",
            Type: MessageType.Text,
            Content: "RabbitMQ mesaj testi",
            MediaUrl: null,
            CreatedAt: DateTime.UtcNow
        );

        var contextMock = new Mock<ConsumeContext<MessageCreatedEvent>>();
        contextMock.Setup(c => c.Message).Returns(messageEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        MessageDocument? savedDocument = null;
        _chatRepositoryMock
            .Setup(r => r.SaveMessageAsync(It.IsAny<MessageDocument>(), It.IsAny<CancellationToken>()))
            .Callback<MessageDocument, CancellationToken>((doc, _) => savedDocument = doc)
            .Returns(Task.CompletedTask);

        var consumer = new MongoMessagePersistenceConsumer(_chatRepositoryMock.Object, _mongoLoggerMock.Object);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        savedDocument.Should().NotBeNull();
        savedDocument!.RoomId.Should().Be("room_1");
        savedDocument.SenderId.Should().Be(messageEvent.SenderId);
        savedDocument.ClientMessageId.Should().Be("client_uuid_123");
        savedDocument.Content.Should().Be("RabbitMQ mesaj testi");

        _chatRepositoryMock.Verify(r => r.SaveMessageAsync(It.IsAny<MessageDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MongoConsumer_WhenDatabaseFails_ShouldRethrowForMassTransitRetry()
    {
        // Arrange
        var messageEvent = new MessageCreatedEvent(
            RoomId: "room_err",
            SenderId: Guid.NewGuid(),
            ClientMessageId: "client_err",
            Type: MessageType.Text,
            Content: "Hata testi",
            MediaUrl: null,
            CreatedAt: DateTime.UtcNow
        );

        var contextMock = new Mock<ConsumeContext<MessageCreatedEvent>>();
        contextMock.Setup(c => c.Message).Returns(messageEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        _chatRepositoryMock
            .Setup(r => r.SaveMessageAsync(It.IsAny<MessageDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MongoDB bağlantısı koptu"));

        var consumer = new MongoMessagePersistenceConsumer(_chatRepositoryMock.Object, _mongoLoggerMock.Object);

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MongoDB bağlantısı koptu");
    }

    [Fact]
    public async Task PushNotificationConsumer_WhenRecipientIsOffline_ShouldDispatchNotification()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var offlineRecipientId = Guid.NewGuid();
        var onlineRecipientId = Guid.NewGuid();
        const string roomId = "room_notifications";

        var fakeRoom = new RoomDocument
        {
            Id = roomId,
            Title = "PulseChat Genel",
            Type = RoomType.Group,
            Members = new List<RoomMemberDocument>
            {
                new() { UserId = senderId, Role = RoomRole.Admin },
                new() { UserId = offlineRecipientId, Role = RoomRole.Member },
                new() { UserId = onlineRecipientId, Role = RoomRole.Member }
            }
        };

        var messageEvent = new MessageCreatedEvent(
            RoomId: roomId,
            SenderId: senderId,
            ClientMessageId: "client_push_1",
            Type: MessageType.Text,
            Content: "Yeni bildirim içeriği",
            MediaUrl: null,
            CreatedAt: DateTime.UtcNow
        );

        var contextMock = new Mock<ConsumeContext<MessageCreatedEvent>>();
        contextMock.Setup(c => c.Message).Returns(messageEvent);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        _chatRepositoryMock
            .Setup(r => r.GetRoomByIdAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeRoom);

        _presenceServiceMock
            .Setup(p => p.IsUserOnlineAsync(offlineRecipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _presenceServiceMock
            .Setup(p => p.IsUserOnlineAsync(onlineRecipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var consumer = new PushNotificationConsumer(
            _chatRepositoryMock.Object,
            _presenceServiceMock.Object,
            _pushLoggerMock.Object
        );

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        // Sender should NOT be checked for presence
        _presenceServiceMock.Verify(p => p.IsUserOnlineAsync(senderId, It.IsAny<CancellationToken>()), Times.Never);

        // Offline and online members should be checked
        _presenceServiceMock.Verify(p => p.IsUserOnlineAsync(offlineRecipientId, It.IsAny<CancellationToken>()), Times.Once);
        _presenceServiceMock.Verify(p => p.IsUserOnlineAsync(onlineRecipientId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
