using FluentAssertions;
using PulseChat.Application.Features.Auth.Register;
using Xunit;

namespace PulseChat.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validator_WhenValidDataProvided_ShouldPassValidation()
    {
        // Arrange
        var command = new RegisterCommand("alice@pulsechat.com", "alice_99", "SuperPassword123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "alice", "password123")]
    [InlineData("not-an-email", "alice", "password123")]
    [InlineData("alice@pulsechat.com", "ab", "password123")] // username too short
    [InlineData("alice@pulsechat.com", "alice with space", "password123")] // invalid characters
    [InlineData("alice@pulsechat.com", "alice", "123")] // password too short
    public void Validator_WhenInvalidDataProvided_ShouldFailValidation(string email, string username, string password)
    {
        // Arrange
        var command = new RegisterCommand(email, username, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
