using System.Net;
using System.Text.Json;
using FluentValidation;
using PulseChat.Domain.Common;

namespace PulseChat.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            var validationResponse = new
            {
                isSuccess = false,
                errorCode = ErrorCodes.ValidationFailed,
                message = "Gönderilen veriler doğrulama kurallarına uymuyor.",
                errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(validationResponse));
            return;
        }

        _logger.LogError(exception, "Sunucu tarafında işlenmemiş bir hata meydana geldi: {Message}", exception.Message);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errorResponse = new
        {
            isSuccess = false,
            errorCode = ErrorCodes.InternalServerError,
            message = "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz."
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
