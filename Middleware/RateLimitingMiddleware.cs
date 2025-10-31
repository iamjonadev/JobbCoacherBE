using System.Security.Claims;
using System.Text.Json;

namespace HaidersAPI.Middleware;

/// <summary>
/// Middleware for rate limiting requests per IP and user
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, List<DateTime>> _ipRequests = new();
    private readonly Dictionary<string, List<DateTime>> _userRequests = new();
    private readonly object _lockObject = new();

    // Rate limiting configuration
    private readonly int _maxRequestsPerMinute = 60;
    private readonly int _maxRequestsPerHour = 1000;
    private readonly int _maxAuthRequestsPerMinute = 10;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIP = GetClientIP(context);
        var userId = GetUserId(context);
        var path = context.Request.Path.Value?.ToLower();
        var isAuthEndpoint = path?.Contains("/auth/") == true;

        lock (_lockObject)
        {
            CleanupOldRequests();

            // Check IP-based rate limiting
            if (!IsIPAllowed(clientIP, isAuthEndpoint))
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IP}, Path: {Path}", clientIP, path);
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                return;
            }

            // Check user-based rate limiting (if authenticated)
            if (!string.IsNullOrEmpty(userId) && !IsUserAllowed(userId, isAuthEndpoint))
            {
                _logger.LogWarning("Rate limit exceeded for User: {UserId}, Path: {Path}", userId, path);
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                return;
            }

            // Record the request
            RecordRequest(clientIP, userId);
        }

        await _next(context);
    }

    private string GetClientIP(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    private bool IsIPAllowed(string ip, bool isAuthEndpoint)
    {
        if (!_ipRequests.ContainsKey(ip))
            _ipRequests[ip] = new List<DateTime>();

        var requests = _ipRequests[ip];
        var now = DateTime.UtcNow;
        var recentRequests = requests.Count(r => r > now.AddMinutes(-1));
        var hourlyRequests = requests.Count(r => r > now.AddHours(-1));

        var limit = isAuthEndpoint ? _maxAuthRequestsPerMinute : _maxRequestsPerMinute;
        
        return recentRequests < limit && hourlyRequests < _maxRequestsPerHour;
    }

    private bool IsUserAllowed(string userId, bool isAuthEndpoint)
    {
        if (!_userRequests.ContainsKey(userId))
            _userRequests[userId] = new List<DateTime>();

        var requests = _userRequests[userId];
        var now = DateTime.UtcNow;
        var recentRequests = requests.Count(r => r > now.AddMinutes(-1));

        var limit = isAuthEndpoint ? _maxAuthRequestsPerMinute : _maxRequestsPerMinute * 2; // Users get higher limits
        
        return recentRequests < limit;
    }

    private void RecordRequest(string ip, string userId)
    {
        var now = DateTime.UtcNow;
        
        if (!_ipRequests.ContainsKey(ip))
            _ipRequests[ip] = new List<DateTime>();
        _ipRequests[ip].Add(now);

        if (!string.IsNullOrEmpty(userId))
        {
            if (!_userRequests.ContainsKey(userId))
                _userRequests[userId] = new List<DateTime>();
            _userRequests[userId].Add(now);
        }
    }

    private void CleanupOldRequests()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        
        foreach (var key in _ipRequests.Keys.ToList())
        {
            _ipRequests[key] = _ipRequests[key].Where(r => r > cutoff).ToList();
            if (!_ipRequests[key].Any())
                _ipRequests.Remove(key);
        }

        foreach (var key in _userRequests.Keys.ToList())
        {
            _userRequests[key] = _userRequests[key].Where(r => r > cutoff).ToList();
            if (!_userRequests[key].Any())
                _userRequests.Remove(key);
        }
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}