using FluentAssertions;
using Moq;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.GetMessages;
using PulseChat.Domain.Common;
using PulseChat.Domain.Documents;
using PulseChat.Domain.Enums;
using Xunit;

namespace PulseChat.UnitTests.Features.Chat;

public class GetRoomMessagesQueryTests
{
    private readonly Mock<IMongoChatRepository> _chatRepositoryMock = new();

    [Fact]
    public async Task Handle_WhenUserIsNotRoomMember_ShouldReturnForbiddenFailure()
    {
        // Arrange
        var roomId = "room_123";
        var unauthorizedUserId = Guid.NewGuid();

        _chatRepositoryMock
            .Setup(r => r.IsUserInRoomAsync(roomId, unauthorizedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new GetRoomMessagesQueryHandler(_chatRepositoryMock.Object);
        var query = new GetRoomMessagesQuery(roomId, unauthorizedUserId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotRoomMember);
        result.Message.Should().Contain("yetkiniz yok");

        _chatRepositoryMock.Verify(
            r => r.GetRoomMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsRoomMember_ShouldReturnMessages()
    {
        // Arrange
        var roomId = "room_456";
        var memberUserId = Guid.NewGuid();
        var messageId = "msg_789";

        var fakeMessages = new List<MessageDocument>
        {
            new()
            {
                Id = messageId,
                RoomId = roomId,
                SenderId = memberUserId,
                ClientMessageId = Guid.NewGuid().ToString(),
                Type = MessageType.Text,
                Content = "Merhaba Dünya!",
                MediaUrl = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        _chatRepositoryMock
            .Setup(r => r.IsUserInRoomAsync(roomId, memberUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _chatRepositoryMock
            .Setup(r => r.GetRoomMessagesAsync(roomId, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeMessages);

        var handler = new GetRoomMessagesQueryHandler(_chatRepositoryMock.Object);
        var query = new GetRoomMessagesQuery(roomId, memberUserId, Limit: 50, Before: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(messageId);
        result.Data[0].Content.Should().Be("Merhaba Dünya!");

        _chatRepositoryMock.Verify(
            r => r.GetRoomMessagesAsync(roomId, 50, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
