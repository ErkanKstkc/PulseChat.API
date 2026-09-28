using FluentAssertions;
using PulseChat.Application.Features.Auth.Login;
using Xunit;

namespace PulseChat.UnitTests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validator_WhenValidCredentialsProvided_ShouldPassValidation()
    {
        // Arrange
        var command = new LoginCommand("alice@pulsechat.com", "SecretPass123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("   ", "password123")]
    [InlineData("alice", "")]
    [InlineData("alice", "   ")]
    public void Validator_WhenCredentialsEmpty_ShouldFailValidation(string emailOrUsername, string password)
    {
        // Arrange
        var command = new LoginCommand(emailOrUsername, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
