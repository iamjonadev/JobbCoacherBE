using HaidersAPI.Helpers;

namespace HaidersAPI.Middleware;

/// <summary>
/// Middleware to validate permissions
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionMiddleware> _logger;

    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AdoteamAuthHelper authHelper)
    {
        // Skip for non-authenticated endpoints
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Skip for auth and health endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path?.Contains("/auth/") == true || path?.Contains("/health") == true || path?.Contains("/api/config/") == true)
        {
            await _next(context);
            return;
        }

        // Check if user can access fakturering system
        if (!authHelper.CanAccessAdoteam(context))
        {
            _logger.LogWarning("Access denied to fakturering system for user {UserEmail}", 
                authHelper.GetUserEmailFromContext(context));
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied to fakturering system");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class PermissionMiddlewareExtensions
{
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionMiddleware>();
    }
}