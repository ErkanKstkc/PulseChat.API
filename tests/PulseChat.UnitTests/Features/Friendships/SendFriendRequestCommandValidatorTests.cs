using FluentAssertions;
using PulseChat.Application.Features.Friendships.SendRequest;
using Xunit;

namespace PulseChat.UnitTests.Features.Friendships;

public class SendFriendRequestCommandValidatorTests
{
    private readonly SendFriendRequestCommandValidator _validator = new();

    [Fact]
    public void Validator_WhenValidTargetUsernameProvided_ShouldPassValidation()
    {
        // Arrange
        var command = new SendFriendRequestCommand(Guid.NewGuid(), "bob_the_builder");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WhenTargetUsernameEmpty_ShouldFailValidation(string invalidUsername)
    {
        // Arrange
        var command = new SendFriendRequestCommand(Guid.NewGuid(), invalidUsername);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SendFriendRequestCommand.TargetUsername));
    }
}
