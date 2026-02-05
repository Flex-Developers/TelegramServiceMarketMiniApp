using System.Net;
using System.Text.Json;
using TelegramMarketplace.Application.Common;

namespace TelegramMarketplace.WebAPI.Middleware;

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
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message, code) = exception switch
        {
            NotFoundException ex => (HttpStatusCode.NotFound, ex.Message, "NOT_FOUND"),
            ValidationException ex => (HttpStatusCode.BadRequest, string.Join("; ", ex.Errors.SelectMany(e => e.Value)), "VALIDATION_ERROR"),
            ForbiddenException ex => (HttpStatusCode.Forbidden, ex.Message, "FORBIDDEN"),
            UnauthorizedException ex => (HttpStatusCode.Unauthorized, ex.Message, "UNAUTHORIZED"),
            PaymentException ex => (HttpStatusCode.BadRequest, ex.Message, ex.ErrorCode ?? "PAYMENT_ERROR"),
            BusinessRuleException ex => (HttpStatusCode.BadRequest, ex.Message, ex.ErrorCode ?? "BUSINESS_RULE_ERROR"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred processing your request", "INTERNAL_ERROR")
        };

        response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new
        {
            error = message,
            code,
            timestamp = DateTime.UtcNow
        });

        await response.WriteAsync(result);
    }
}
