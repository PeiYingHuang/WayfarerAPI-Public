using System.Net;
using System.Text.Json;

namespace WayfarerAPI.Api.Middlewares;

public sealed class GlobalExceptionMiddleware
{   
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

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
            _logger.LogError(
                ex,
                "Unhandled exception occurred. Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ArgumentException =>
                (HttpStatusCode.BadRequest, exception.Message),

            InvalidOperationException =>
                (HttpStatusCode.Conflict, exception.Message),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, exception.Message),

            _ =>
                (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            message
        });

        return context.Response.WriteAsync(payload);
    }
}
