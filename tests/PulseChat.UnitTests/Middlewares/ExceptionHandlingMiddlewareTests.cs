using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PulseChat.API.Middlewares;
using PulseChat.Domain.Common;
using Xunit;

namespace PulseChat.UnitTests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var wasNextCalled = false;
        RequestDelegate next = _ =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        wasNextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_ShouldReturn400BadRequestWithErrors()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Geçerli bir e-posta giriniz."),
            new("Password", "Parola en az 8 karakter olmalıdır.")
        };

        RequestDelegate next = _ => throw new ValidationException(validationFailures);
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/json");

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be(ErrorCodes.ValidationFailed);
        root.GetProperty("errors").GetProperty("Email")[0].GetString().Should().Be("Geçerli bir e-posta giriniz.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_ShouldReturn500InternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        RequestDelegate next = _ => throw new InvalidOperationException("Kritik veritabanı çöküşü!");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().Be("application/json");

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be(ErrorCodes.InternalServerError);
    }
}
