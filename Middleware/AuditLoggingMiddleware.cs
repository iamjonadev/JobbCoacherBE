using System.Text.Json;
using System.Security.Claims;

namespace HaidersAPI.Middleware;

/// <summary>
/// Middleware for comprehensive audit logging of all requests and actions
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        // Capture request details
        var requestDetails = await CaptureRequestDetails(context, requestId);

        // Add request ID to response headers for tracing
        context.Response.Headers["X-Request-ID"] = requestId;

        // Capture response
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogError(requestDetails, ex, startTime);
            throw;
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            var responseDetails = await CaptureResponseDetails(context, responseBodyStream, originalResponseBody);
            
            // Log the audit entry
            LogAuditEntry(requestDetails, responseDetails, startTime, endTime);
        }
    }

    private async Task<AuditRequestDetails> CaptureRequestDetails(HttpContext context, string requestId)
    {
        var request = context.Request;
        var user = context.User;

        // Capture request body for POST/PUT requests (but exclude sensitive data)
        string requestBody = "";
        if (request.Method == "POST" || request.Method == "PUT")
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            requestBody = System.Text.Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            // Sanitize sensitive data
            requestBody = SanitizeSensitiveData(requestBody);
        }

        return new AuditRequestDetails
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.Value ?? "",
            UserAgent = request.Headers["User-Agent"].ToString(),
            IPAddress = GetClientIP(context),
            UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserEmail = user?.FindFirst(ClaimTypes.Email)?.Value,
            UserRole = user?.FindFirst(ClaimTypes.Role)?.Value,
            RequestBody = requestBody,
            ContentType = request.ContentType ?? "",
            Headers = GetSafeHeaders(request.Headers)
        };
    }

    private async Task<AuditResponseDetails> CaptureResponseDetails(HttpContext context, MemoryStream responseBodyStream, Stream originalResponseBody)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;

        return new AuditResponseDetails
        {
            StatusCode = context.Response.StatusCode,
            ResponseBody = SanitizeSensitiveData(responseBody),
            ContentType = context.Response.ContentType ?? "",
            Headers = GetSafeHeaders(context.Response.Headers)
        };
    }

    private void LogAuditEntry(AuditRequestDetails request, AuditResponseDetails response, DateTime startTime, DateTime endTime)
    {
        var duration = endTime - startTime;
        var isSuccess = response.StatusCode >= 200 && response.StatusCode < 400;
        var isSensitiveOperation = IsSensitiveOperation(request.Path, request.Method);

        var auditEntry = new
        {
            RequestId = request.RequestId,
            Timestamp = request.Timestamp,
            Duration = duration.TotalMilliseconds,
            Success = isSuccess,
            IsSensitive = isSensitiveOperation,
            Request = new
            {
                request.Method,
                request.Path,
                request.QueryString,
                request.IPAddress,
                request.UserAgent,
                request.UserId,
                request.UserEmail,
                request.UserRole,
                request.ContentType,
                BodyLength = request.RequestBody?.Length ?? 0,
                HasBody = !string.IsNullOrEmpty(request.RequestBody)
            },
            Response = new
            {
                response.StatusCode,
                response.ContentType,
                BodyLength = response.ResponseBody?.Length ?? 0,
                HasBody = !string.IsNullOrEmpty(response.ResponseBody)
            }
        };

        if (isSuccess)
        {
            if (isSensitiveOperation)
            {
                _logger.LogWarning("AUDIT: Sensitive operation completed - {AuditEntry}", JsonSerializer.Serialize(auditEntry));
            }
            else
            {
                _logger.LogInformation("AUDIT: Request completed - {AuditEntry}", JsonSerializer.Serialize(auditEntry));
            }
        }
        else
        {
            _logger.LogError("AUDIT: Request failed - {AuditEntry}", JsonSerializer.Serialize(auditEntry));
        }

        // Log detailed information for sensitive operations
        if (isSensitiveOperation)
        {
            var detailedEntry = new
            {
                RequestId = request.RequestId,
                DetailedRequest = request,
                DetailedResponse = response,
                Duration = duration.TotalMilliseconds
            };
            _logger.LogWarning("AUDIT_DETAILED: Sensitive operation details - {DetailedEntry}", JsonSerializer.Serialize(detailedEntry));
        }
    }

    private void LogError(AuditRequestDetails request, Exception exception, DateTime startTime)
    {
        var errorEntry = new
        {
            RequestId = request.RequestId,
            Timestamp = request.Timestamp,
            Duration = (DateTime.UtcNow - startTime).TotalMilliseconds,
            Request = request,
            Error = new
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            }
        };

        _logger.LogError(exception, "AUDIT_ERROR: Request failed with exception - {ErrorEntry}", JsonSerializer.Serialize(errorEntry));
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

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var excludedHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };

        foreach (var header in headers)
        {
            if (!excludedHeaders.Contains(header.Key.ToLower()))
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    private string SanitizeSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        // Remove or mask sensitive information
        var sensitiveFields = new[] { "password", "token", "secret", "key", "ssn", "personalNumber", "creditCard" };
        
        foreach (var field in sensitiveFields)
        {
            // Simple regex to mask sensitive JSON fields
            data = System.Text.RegularExpressions.Regex.Replace(
                data, 
                $@"""({field})""\s*:\s*""([^""]*?)""", 
                $@"""{field}"":""***MASKED***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return data;
    }

    private bool IsSensitiveOperation(string path, string method)
    {
        var sensitiveOperations = new[]
        {
            "/api/auth/",
            "/api/client/",
            "/api/financial/",
            "/api/tax/",
            "/api/invoice/",
            "/api/report/",
            "/api/admin/",
            "/api/user/"
        };

        var sensitiveActions = new[] { "POST", "PUT", "DELETE" };

        return sensitiveOperations.Any(op => path.ToLower().Contains(op)) ||
               sensitiveActions.Contains(method.ToUpper());
    }
}

public class AuditRequestDetails
{
    public string RequestId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string QueryString { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string IPAddress { get; set; } = "";
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string RequestBody { get; set; } = "";
    public string ContentType { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class AuditResponseDetails
{
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = "";
    public string ContentType { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
}

public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}