using FluentAssertions;
using Moq;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.MarkAsRead;
using PulseChat.Domain.Common;
using Xunit;

namespace PulseChat.UnitTests.Features.Chat;

public class MarkRoomAsReadCommandTests
{
    private readonly Mock<IMongoChatRepository> _chatRepositoryMock = new();
    private readonly MarkRoomAsReadCommandValidator _validator = new();

    [Fact]
    public void Validator_WhenValidDataProvided_ShouldPassValidation()
    {
        var command = new MarkRoomAsReadCommand("507f1f77bcf86cd799439011", Guid.NewGuid());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validator_WhenRoomIdIsEmpty_ShouldFailValidation(string invalidRoomId)
    {
        var command = new MarkRoomAsReadCommand(invalidRoomId, Guid.NewGuid());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MarkRoomAsReadCommand.RoomId));
    }

    [Fact]
    public void Validator_WhenUserIdIsEmpty_ShouldFailValidation()
    {
        var command = new MarkRoomAsReadCommand("507f1f77bcf86cd799439011", Guid.Empty);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MarkRoomAsReadCommand.UserId));
    }

    [Fact]
    public async Task Handle_WhenUserIsNotRoomMember_ShouldReturnFailure()
    {
        var roomId = "507f1f77bcf86cd799439011";
        var userId = Guid.NewGuid();
        var command = new MarkRoomAsReadCommand(roomId, userId);

        _chatRepositoryMock
            .Setup(x => x.IsUserInRoomAsync(roomId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new MarkRoomAsReadCommandHandler(_chatRepositoryMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotRoomMember);
        _chatRepositoryMock.Verify(x => x.UpdateMemberLastReadAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsMember_ShouldUpdateLastReadAndReturnSuccess()
    {
        var roomId = "507f1f77bcf86cd799439011";
        var userId = Guid.NewGuid();
        var command = new MarkRoomAsReadCommand(roomId, userId);

        _chatRepositoryMock
            .Setup(x => x.IsUserInRoomAsync(roomId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _chatRepositoryMock
            .Setup(x => x.UpdateMemberLastReadAsync(roomId, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new MarkRoomAsReadCommandHandler(_chatRepositoryMock.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _chatRepositoryMock.Verify(x => x.UpdateMemberLastReadAsync(roomId, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
