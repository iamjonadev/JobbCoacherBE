using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HaidersAPI.Helpers;
using HaidersAPI.Services;
using HaidersAPI.DTOs;

namespace HaidersAPI.Controllers;

/// <summary>
/// Authentication controller for HaidersAPI following InventoryBE patterns
/// Provides token validation and user information endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AdoteamAuthHelper _authHelper;
    private readonly PermissionService _permissionService;
    private readonly AdoteamSQL _usableSQL;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AdoteamAuthHelper authHelper,
        PermissionService permissionService,
        AdoteamSQL usableSQL,
        ILogger<AuthController> logger)
    {
        _authHelper = authHelper;
        _permissionService = permissionService;
        _usableSQL = usableSQL;
        _logger = logger;
    }

    /// <summary>
    /// Validate JWT token and return user information
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { message = "Token is required" });
            }

            var (isValid, userId, email, role) = await _authHelper.ValidateTokenAsync(request.Token);
            
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new
            {
                valid = true,
                userId,
                email,
                role,
                canAccessAdoteam = new[] { "Admin", "Owner", "AdoteamOwner", "AdoteamAccountant", "AdoteamClient", "AdoteamAuditor" }.Contains(role),
                validatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token: {Error}", ex.Message);
            return BadRequest(new { message = "Token validation failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate JWT token via URL parameter (GET method)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("validate-token/by-param/{token}")]
    public async Task<IActionResult> ValidateTokenByParameter(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token parameter is required" });
            }

            var (isValid, userId, email, role) = await _authHelper.ValidateTokenAsync(token);
            
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new
            {
                valid = true,
                userId,
                email,
                role,
                canAccessAdoteam = new[] { "Admin", "Owner", "AdoteamOwner", "AdoteamAccountant", "AdoteamClient", "AdoteamAuditor" }.Contains(role),
                validatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via parameter: {Error}", ex.Message);
            return BadRequest(new { message = "Token validation failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate JWT token via query parameter (GET method)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("validate-token/by-query")]
    public async Task<IActionResult> ValidateTokenByQuery([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token query parameter is required" });
            }

            var (isValid, userId, email, role) = await _authHelper.ValidateTokenAsync(token);
            
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new
            {
                valid = true,
                userId,
                email,
                role,
                canAccessAdoteam = new[] { "Admin", "Owner", "AdoteamOwner", "AdoteamAccountant", "AdoteamClient", "AdoteamAuditor" }.Contains(role),
                validatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via query: {Error}", ex.Message);
            return BadRequest(new { message = "Token validation failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current user information from JWT token
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = _authHelper.GetUserIdFromContext(HttpContext);
            var email = _authHelper.GetUserEmailFromContext(HttpContext);
            var role = _authHelper.GetUserRoleFromContext(HttpContext);

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            // Get user's accessible clients
            var clients = await _permissionService.GetUserClientsAsync(userId.Value);

            return Ok(new
            {
                userId = userId.Value,
                email,
                role,
                permissions = new
                {
                    isAdmin = _permissionService.IsAdmin(HttpContext),
                    canViewFinancials = _permissionService.CanViewFinancials(HttpContext),
                    canManageInvoices = _permissionService.CanManageInvoices(HttpContext),
                    isAuditor = _permissionService.IsAuditor(HttpContext)
                },
                accessibleClients = clients,
                lastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information");
            return BadRequest(new { message = "Failed to get user information" });
        }
    }

    /// <summary>
    /// Check user permissions for specific client and action
    /// </summary>
    [Authorize]
    [HttpPost("check-permission")]
    public async Task<IActionResult> CheckPermission([FromBody] PermissionCheckDTO request)
    {
        try
        {
            var userId = _authHelper.GetUserIdFromContext(HttpContext);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            var hasPermission = await _permissionService.ValidatePermissionAsync(
                userId.Value, request.ClientId, request.Action, request.Resource);

            return Ok(new
            {
                hasPermission,
                userId = userId.Value,
                clientId = request.ClientId,
                action = request.Action,
                resource = request.Resource,
                checkedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission");
            return BadRequest(new { message = "Permission check failed" });
        }
    }

    /// <summary>
    /// Get user's accessible clients
    /// </summary>
    [Authorize]
    [HttpGet("clients")]
    public async Task<IActionResult> GetAccessibleClients()
    {
        try
        {
            var userId = _authHelper.GetUserIdFromContext(HttpContext);
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            var clients = await _permissionService.GetUserClientsAsync(userId.Value);
            
            return Ok(new
            {
                userId = userId.Value,
                clients,
                count = clients.Count(),
                retrievedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accessible clients");
            return BadRequest(new { message = "Failed to get accessible clients" });
        }
    }

    /// <summary>
    /// Grant client access to user (admin only)
    /// </summary>
    [Authorize(Policy = "AdoteamOwner")]
    [HttpPost("grant-access")]
    public async Task<IActionResult> GrantClientAccess([FromBody] GrantAccessDTO request)
    {
        try
        {
            var currentUserId = _authHelper.GetUserIdFromContext(HttpContext);
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            var success = await _permissionService.GrantClientAccessAsync(
                request.UserId, 
                request.ClientId, 
                request.AccessLevel,
                request.CanViewFinancials,
                request.CanManageInvoices,
                request.CanManageExpenses,
                request.CanViewReports,
                currentUserId.Value);

            if (success)
            {
                _logger.LogInformation("Access granted to user {UserId} for client {ClientId} by {GrantedBy}", 
                    request.UserId, request.ClientId, currentUserId.Value);
                
                return Ok(new { message = "Access granted successfully" });
            }

            return BadRequest(new { message = "Failed to grant access" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting client access");
            return BadRequest(new { message = "Failed to grant access" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "HaidersAPI.Auth",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}