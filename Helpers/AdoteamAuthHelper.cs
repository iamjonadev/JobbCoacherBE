using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HaidersAPI.Data;

namespace HaidersAPI.Helpers;

/// <summary>
/// Authentication helper for HaidersAPI that integrates with InventoryBE users
/// </summary>
public class AdoteamAuthHelper
{
    private readonly IConfiguration _configuration;
    private readonly AdoteamDataContext _dataContext;
    
    public AdoteamAuthHelper(IConfiguration configuration, AdoteamDataContext dataContext)
    {
        _configuration = configuration;
        _dataContext = dataContext;
    }

    /// <summary>
    /// Create JWT token compatible with InventoryBE authentication
    /// </summary>
    public string CreateToken(int userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenKey = Environment.GetEnvironmentVariable("TOKEN_KEY");
        if (string.IsNullOrEmpty(tokenKey))
        {
            throw new InvalidOperationException("TOKEN_KEY is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate token and extract user information
    /// </summary>
    public Task<(bool IsValid, int UserId, string Email, string Role)> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenKey = Environment.GetEnvironmentVariable("TOKEN_KEY");
            if (string.IsNullOrEmpty(tokenKey))
            {
                return Task.FromResult((false, 0, "", ""));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var userIdClaim = principal.FindFirst("user_id")?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return Task.FromResult((true, userId, emailClaim ?? "", roleClaim ?? ""));
            }

            return Task.FromResult((false, 0, "", ""));
        }
        catch
        {
            return Task.FromResult((false, 0, "", ""));
        }
    }

    /// <summary>
    /// Extract user ID from JWT token claims
    /// </summary>
    public int? GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("user_id")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extract user role from JWT token claims
    /// </summary>
    public string? GetUserRoleFromContext(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Extract user email from JWT token claims
    /// </summary>
    public string? GetUserEmailFromContext(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Check if user has permission for specific action on client
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, Guid? clientId, string action, string resource)
    {
        return await _dataContext.ValidateUserPermissionAsync(userId, clientId, action, resource);
    }

    /// <summary>
    /// Check if user has global admin permissions
    /// </summary>
    public bool IsGlobalAdmin(HttpContext context)
    {
        var role = GetUserRoleFromContext(context);
        return role is "Admin" or "Owner" or "AdoteamOwner";
    }

    /// <summary>
    /// Check if user can access fakturering system
    /// </summary>
    public bool CanAccessAdoteam(HttpContext context)
    {
        var role = GetUserRoleFromContext(context);
        return role is "Admin" or "Owner" or "AdoteamOwner" or "AdoteamAccountant" 
                    or "AdoteamClient" or "AdoteamAuditor";
    }
}