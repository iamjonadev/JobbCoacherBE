using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace HaidersAPI.Middleware;

/// <summary>
/// Global exception handling middleware with security-focused error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        var errorId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;

        // Log the error with full details
        _logger.LogError(exception, 
            "SECURITY_ERROR: Unhandled exception occurred. ErrorId: {ErrorId}, Path: {Path}, Method: {Method}, IP: {IP}, User: {User}",
            errorId,
            context.Request.Path,
            context.Request.Method,
            GetClientIP(context),
            GetUserId(context));

        // Determine response based on exception type
        var response = CreateErrorResponse(exception, errorId, timestamp);

        // Set response properties
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        // Add security headers
        context.Response.Headers["X-Error-ID"] = errorId;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Write response
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string errorId, DateTime timestamp)
    {
        return exception switch
        {
            SecurityException secEx => new ErrorResponse
            {
                StatusCode = 403,
                ErrorType = "SecurityError",
                Message = "Access denied",
                ErrorId = errorId,
                Timestamp = timestamp,
                Details = _environment.IsDevelopment() ? secEx.Message : null
            },
            
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = 401,
                ErrorType = "AuthenticationError", 
                Message = "Authentication required",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            ArgumentException argEx => new ErrorResponse
            {
                StatusCode = 400,
                ErrorType = "ValidationError",
                Message = "Invalid request parameters",
                ErrorId = errorId,
                Timestamp = timestamp,
                Details = _environment.IsDevelopment() ? argEx.Message : null
            },

            InvalidOperationException => new ErrorResponse
            {
                StatusCode = 400,
                ErrorType = "BusinessLogicError",
                Message = "Invalid operation",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            TimeoutException => new ErrorResponse
            {
                StatusCode = 408,
                ErrorType = "TimeoutError",
                Message = "Request timeout",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            NotImplementedException => new ErrorResponse
            {
                StatusCode = 501,
                ErrorType = "NotImplementedError",
                Message = "Feature not implemented",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            SqlException sqlEx when sqlEx.Number == 2 => new ErrorResponse
            {
                StatusCode = 503,
                ErrorType = "DatabaseError",
                Message = "Database connection timeout",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            SqlException => new ErrorResponse
            {
                StatusCode = 503,
                ErrorType = "DatabaseError",
                Message = "Database error occurred",
                ErrorId = errorId,
                Timestamp = timestamp
            },

            _ => new ErrorResponse
            {
                StatusCode = 500,
                ErrorType = "InternalServerError",
                Message = "An internal server error occurred",
                ErrorId = errorId,
                Timestamp = timestamp,
                Details = _environment.IsDevelopment() ? exception.Message : null
            }
        };
    }

    private string GetClientIP(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorType { get; set; } = "";
    public string Message { get; set; } = "";
    public string ErrorId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
    public string SupportMessage { get; set; } = "Please contact support if this error persists, providing the Error ID.";
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}