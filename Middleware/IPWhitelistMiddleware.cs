using System.Net;
using System.Text.Json;

namespace HaidersAPI.Middleware;

/// <summary>
/// Middleware for IP whitelisting based on configuration
/// </summary>
public class IPWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IPWhitelistMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public IPWhitelistMiddleware(RequestDelegate next, ILogger<IPWhitelistMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIP = GetClientIP(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Determine the endpoint category
        var category = GetEndpointCategory(path);
        
        if (!string.IsNullOrEmpty(category) && !IsIPAllowed(clientIP, category))
        {
            _logger.LogWarning("IP {IP} denied access to {Category} endpoint: {Path}", clientIP, category, path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new 
            { 
                error = "Access denied", 
                message = "Your IP address is not authorized to access this resource" 
            }));
            return;
        }

        await _next(context);
    }

    private string GetClientIP(HttpContext context)
    {
        // Check for forwarded IP headers (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            return realIP;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetEndpointCategory(string path)
    {
        // Map endpoints to whitelist categories
        if (path.Contains("/auth/validate-token") || path.Contains("/auth/me"))
            return ""; // No restriction for basic auth

        if (path.Contains("/auth/") && (path.Contains("reset") || path.Contains("password")))
            return "passwordreset";

        if (path.Contains("/auth/refresh"))
            return "tokenrefresh";

        if (path.Contains("/admin/") || path.Contains("/manage/"))
            return "adminoperations";

        if (path.Contains("/register") || path.Contains("/signup"))
            return "registration";

        if (path.Contains("/client/") || path.Contains("/personal/"))
            return "sensitivedata";

        if (path.Contains("/config/") || path.Contains("/system/"))
            return "systemconfig";

        if (path.Contains("/tax/") || path.Contains("/vat/"))
            return "taxoperations";

        if (path.Contains("/report/") || path.Contains("/financial/"))
            return "financialreports";

        return ""; // No restriction for other endpoints
    }

    private bool IsIPAllowed(string clientIP, string category)
    {
        if (string.IsNullOrEmpty(category))
            return true; // No restriction

        var whitelistSection = $"IpWhitelists:{category}";
        var allowedIPs = _configuration.GetSection(whitelistSection).Get<string[]>();

        if (allowedIPs == null || !allowedIPs.Any())
        {
            _logger.LogWarning("No IP whitelist configured for category: {Category}", category);
            return true; // Allow if no whitelist configured
        }

        foreach (var allowedIP in allowedIPs)
        {
            if (IsIPInRange(clientIP, allowedIP))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsIPInRange(string clientIP, string allowedIP)
    {
        try
        {
            // Handle exact match
            if (clientIP == allowedIP)
                return true;

            // Handle localhost variations
            if ((clientIP == "::1" || clientIP == "127.0.0.1") && 
                (allowedIP == "::1" || allowedIP == "127.0.0.1"))
                return true;

            // Handle wildcard patterns (e.g., 192.168.1.*)
            if (allowedIP.Contains("*"))
            {
                var pattern = allowedIP.Replace("*", ".*");
                return System.Text.RegularExpressions.Regex.IsMatch(clientIP, $"^{pattern}$");
            }

            // Handle CIDR notation (e.g., 192.168.1.0/24)
            if (allowedIP.Contains("/"))
            {
                return IsIPInCIDRRange(clientIP, allowedIP);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP range for {ClientIP} against {AllowedIP}", clientIP, allowedIP);
            return false;
        }
    }

    private bool IsIPInCIDRRange(string clientIP, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            var networkIP = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            var clientIPAddress = IPAddress.Parse(clientIP);

            // Convert to bytes for comparison
            var networkBytes = networkIP.GetAddressBytes();
            var clientBytes = clientIPAddress.GetAddressBytes();

            if (networkBytes.Length != clientBytes.Length)
                return false;

            // Calculate subnet mask
            var maskBytes = new byte[networkBytes.Length];
            var bytesToCheck = prefixLength / 8;
            var bitsToCheck = prefixLength % 8;

            // Set full bytes
            for (int i = 0; i < bytesToCheck && i < maskBytes.Length; i++)
            {
                maskBytes[i] = 0xFF;
            }

            // Set partial byte
            if (bitsToCheck > 0 && bytesToCheck < maskBytes.Length)
            {
                maskBytes[bytesToCheck] = (byte)(0xFF << (8 - bitsToCheck));
            }

            // Check if client IP is in network range
            for (int i = 0; i < networkBytes.Length; i++)
            {
                if ((networkBytes[i] & maskBytes[i]) != (clientBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class IPWhitelistMiddlewareExtensions
{
    public static IApplicationBuilder UseIPWhitelist(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IPWhitelistMiddleware>();
    }
}