using FluentAssertions;
using PulseChat.Domain.Common;
using Xunit;

namespace PulseChat.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Success_WithData_ShouldCreateSuccessfulResultWithData()
    {
        // Arrange
        const string testData = "Test payload";

        // Act
        var result = Result.Success(testData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(testData);
        result.ErrorCode.Should().BeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResultWithErrorCodeAndMessage()
    {
        // Arrange
        const string errorCode = ErrorCodes.ValidationFailed;
        const string message = "Geçersiz veri";

        // Act
        var result = Result.Failure(errorCode, message);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(errorCode);
        result.Message.Should().Be(message);
    }

    [Fact]
    public void FailureGeneric_ShouldCreateFailedResultWithDefaultData()
    {
        // Act
        var result = Result.Failure<string>(ErrorCodes.UserNotFound, "Kullanıcı bulunamadı.");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().Be(ErrorCodes.UserNotFound);
        result.Message.Should().Be("Kullanıcı bulunamadı.");
    }
}
