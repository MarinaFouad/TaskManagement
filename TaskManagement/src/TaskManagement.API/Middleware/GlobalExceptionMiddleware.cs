using System.Net;
using System.Text.Json;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message, (IDictionary<string, string[]>?)null),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message, null),
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Message, null),
            ConflictException e => (HttpStatusCode.Conflict, e.Message, null),
            Domain.Exceptions.ValidationException e => (HttpStatusCode.BadRequest, e.Message, (IDictionary<string, string[]>?)e.Errors),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception [{Status}]: {Message}", statusCode, exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(
            message,
            errors as IReadOnlyDictionary<string, string[]>);

        var json = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
