using System.Text.Json;

namespace HaidersAPI.Middleware;

/// <summary>
/// Middleware for comprehensive security headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);

        // Check for suspicious requests
        if (IsSuspiciousRequest(context))
        {
            _logger.LogWarning("Suspicious request detected from IP: {IP}, Path: {Path}, UserAgent: {UserAgent}", 
                context.Connection.RemoteIpAddress, 
                context.Request.Path, 
                context.Request.Headers["User-Agent"]);
            
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad Request");
            return;
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent XSS attacks
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["X-XSS-Protection"] = "1; mode=block";

        // Content Security Policy for API
        headers["Content-Security-Policy"] = 
            "default-src 'none'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Referrer Policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Feature Policy
        headers["Permissions-Policy"] = 
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "speaker=()";

        // HSTS (HTTP Strict Transport Security)
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Remove server information
        headers.Remove("Server");
        headers["Server"] = "HaidersAPI";

        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }
    }

    private bool IsSuspiciousRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

        // Check for common attack patterns
        var suspiciousPatterns = new[]
        {
            "script", "javascript", "vbscript", "onload", "onerror",
            "eval(", "alert(", "confirm(", "prompt(",
            "../", "..\\", "/etc/passwd", "/proc/",
            "union select", "drop table", "delete from",
            "cmd.exe", "powershell", "bash", "/bin/sh"
        };

        foreach (var pattern in suspiciousPatterns)
        {
            if (path.Contains(pattern) || userAgent.Contains(pattern))
            {
                return true;
            }
        }

        // Check query string
        var queryString = context.Request.QueryString.Value?.ToLower() ?? "";
        foreach (var pattern in suspiciousPatterns)
        {
            if (queryString.Contains(pattern))
            {
                return true;
            }
        }

        // Check for empty or suspicious user agents
        if (string.IsNullOrEmpty(userAgent) || 
            userAgent.Contains("bot") && !IsLegitimateBot(userAgent))
        {
            return true;
        }

        return false;
    }

    private bool IsLegitimateBot(string userAgent)
    {
        var legitimateBots = new[]
        {
            "googlebot", "bingbot", "slurp", "duckduckbot",
            "baiduspider", "yandexbot", "facebookexternalhit"
        };

        return legitimateBots.Any(bot => userAgent.Contains(bot));
    }

    private bool IsSensitiveEndpoint(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/auth/",
            "/api/financial/",
            "/api/tax/",
            "/api/invoice/",
            "/api/client/",
            "/api/report/"
        };

        return sensitiveEndpoints.Any(endpoint => 
            path.Value?.ToLower().StartsWith(endpoint) == true);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}