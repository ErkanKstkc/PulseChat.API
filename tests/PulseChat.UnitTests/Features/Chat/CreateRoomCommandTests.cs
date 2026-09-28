using FluentAssertions;
using Moq;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.CreateRoom;
using PulseChat.Domain.Documents;
using PulseChat.Domain.Enums;
using Xunit;

namespace PulseChat.UnitTests.Features.Chat;

public class CreateRoomCommandTests
{
    private readonly Mock<IMongoChatRepository> _chatRepositoryMock = new();
    private readonly CreateRoomCommandValidator _validator = new();

    [Fact]
    public void Validator_WhenValidDataProvided_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateRoomCommand(
            CreatorId: Guid.NewGuid(),
            Type: RoomType.Group,
            Title: "Backend Ekibi",
            AvatarUrl: "https://minio.pulsechat.com/media/avatar.png",
            MemberIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WhenTitleIsEmpty_ShouldFailValidation(string invalidTitle)
    {
        // Arrange
        var command = new CreateRoomCommand(
            CreatorId: Guid.NewGuid(),
            Type: RoomType.Group,
            Title: invalidTitle,
            AvatarUrl: null,
            MemberIds: new List<Guid> { Guid.NewGuid() }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoomCommand.Title));
    }

    [Fact]
    public void Validator_WhenTitleExceeds100Characters_ShouldFailValidation()
    {
        // Arrange
        var longTitle = new string('A', 101);
        var command = new CreateRoomCommand(
            CreatorId: Guid.NewGuid(),
            Type: RoomType.Group,
            Title: longTitle,
            AvatarUrl: null,
            MemberIds: new List<Guid> { Guid.NewGuid() }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoomCommand.Title));
    }

    [Fact]
    public void Validator_WhenMemberIdsIsEmpty_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateRoomCommand(
            CreatorId: Guid.NewGuid(),
            Type: RoomType.Group,
            Title: "Test Odası",
            AvatarUrl: null,
            MemberIds: new List<Guid>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoomCommand.MemberIds));
    }

    [Fact]
    public async Task Handler_ShouldAssignAdminRoleToCreatorAndSaveRoom()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();

        var command = new CreateRoomCommand(
            CreatorId: creatorId,
            Type: RoomType.Group,
            Title: "PulseChat Mimari Ekip",
            AvatarUrl: null,
            MemberIds: new List<Guid> { memberId1, memberId2, memberId1 } // duplicate memberId1
        );

        RoomDocument? capturedRoom = null;
        _chatRepositoryMock
            .Setup(r => r.CreateRoomAsync(It.IsAny<RoomDocument>(), It.IsAny<CancellationToken>()))
            .Callback<RoomDocument, CancellationToken>((r, _) => capturedRoom = r)
            .Returns(Task.CompletedTask);

        var handler = new CreateRoomCommandHandler(_chatRepositoryMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("PulseChat Mimari Ekip");

        capturedRoom.Should().NotBeNull();
        capturedRoom!.Title.Should().Be("PulseChat Mimari Ekip");
        capturedRoom.CreatedBy.Should().Be(creatorId);

        // Admin role check for creator
        var creatorMember = capturedRoom.Members.FirstOrDefault(m => m.UserId == creatorId);
        creatorMember.Should().NotBeNull();
        creatorMember!.Role.Should().Be(RoomRole.Admin);

        // Distinct check: creator + memberId1 + memberId2 = exactly 3 members
        capturedRoom.Members.Should().HaveCount(3);
        capturedRoom.Members.Count(m => m.UserId == memberId1).Should().Be(1);

        _chatRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<RoomDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
