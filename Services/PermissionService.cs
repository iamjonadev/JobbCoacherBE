using HaidersAPI.Helpers;
using HaidersAPI.Models;
using HaidersAPI.DTOs;

namespace HaidersAPI.Services
{
    /// <summary>
    /// Permission service for Adoteam API following InventoryBE patterns
    /// </summary>
    public class PermissionService
    {
        private readonly AdoteamSQL _usableSQL;
        private readonly AdoteamAuthHelper _authHelper;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            AdoteamSQL usableSQL,
            AdoteamAuthHelper authHelper,
            ILogger<PermissionService> logger)
        {
            _usableSQL = usableSQL;
            _authHelper = authHelper;
            _logger = logger;
        }

        /// <summary>
        /// Get all clients accessible by user
        /// </summary>
        public async Task<IEnumerable<ClientModel>> GetUserClientsAsync(int userId)
        {
            try
            {
                return await Task.FromResult(_usableSQL.GetUserClients(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user clients for userId: {UserId}", userId);
                return Enumerable.Empty<ClientModel>();
            }
        }

        /// <summary>
        /// Validate user permission for specific action and resource
        /// </summary>
        public async Task<bool> ValidatePermissionAsync(int userId, Guid? clientId, string action, string resource)
        {
            try
            {
                if (clientId.HasValue)
                {
                    var access = _usableSQL.GetUserClientAccess(userId, clientId.Value);
                    if (access == null)
                    {
                        return false;
                    }
                    return await Task.FromResult(IsClientPermissionAllowed(access, action, resource));
                }

                return await Task.FromResult(_usableSQL.ValidatePermission(userId, clientId, action, resource));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating permission for userId: {UserId}, clientId: {ClientId}, action: {Action}, resource: {Resource}", 
                    userId, clientId, action, resource);
                return false;
            }
        }

        /// <summary>
        /// Grant client access to user
        /// </summary>
        public async Task<bool> GrantClientAccessAsync(
            int userId, 
            Guid clientId, 
            string accessLevel,
            bool canViewFinancials,
            bool canManageInvoices,
            bool canManageExpenses,
            bool canViewReports,
            int grantedBy)
        {
            try
            {
                if (!_usableSQL.CheckUserExists(userId))
                {
                    _logger.LogWarning("Attempted to grant access to non-existent user: {UserId}", userId);
                    return false;
                }

                if (!_usableSQL.CheckClientExists(clientId))
                {
                    _logger.LogWarning("Attempted to grant access to non-existent client: {ClientId}", clientId);
                    return false;
                }

                var request = new GrantAccessDTO
                {
                    UserId = userId,
                    ClientId = clientId,
                    AccessLevel = accessLevel,
                    CanViewFinancials = canViewFinancials,
                    CanManageInvoices = canManageInvoices,
                    CanManageExpenses = canManageExpenses,
                    CanViewReports = canViewReports
                };

                return await Task.FromResult(_usableSQL.GrantClientAccess(request, grantedBy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting client access for userId: {UserId}, clientId: {ClientId}", userId, clientId);
                return false;
            }
        }

        /// <summary>
        /// Check if user is admin
        /// </summary>
        public bool IsAdmin(HttpContext context)
        {
            var role = _authHelper.GetUserRoleFromContext(context);
            return new[] { "Admin", "Owner", "AdoteamOwner" }.Contains(role);
        }

        /// <summary>
        /// Check if user can view financials
        /// </summary>
        public bool CanViewFinancials(HttpContext context)
        {
            var role = _authHelper.GetUserRoleFromContext(context);
            return new[] { "Admin", "Owner", "AdoteamOwner", "AdoteamAccountant" }.Contains(role);
        }

        /// <summary>
        /// Check if user can manage invoices
        /// </summary>
        public bool CanManageInvoices(HttpContext context)
        {
            var role = _authHelper.GetUserRoleFromContext(context);
            return new[] { "Admin", "Owner", "AdoteamOwner", "AdoteamAccountant" }.Contains(role);
        }

        /// <summary>
        /// Check if user is auditor
        /// </summary>
        public bool IsAuditor(HttpContext context)
        {
            var role = _authHelper.GetUserRoleFromContext(context);
            return role == "AdoteamAuditor";
        }

        private bool IsClientPermissionAllowed(UserClientAccessModel access, string action, string resource)
        {
            return action.ToLower() switch
            {
                "view" => resource.ToLower() switch
                {
                    "financials" => access.CanViewFinancials,
                    "reports" => access.CanViewReports,
                    _ => true
                },
                "manage" => resource.ToLower() switch
                {
                    "invoices" => access.CanManageInvoices,
                    "expenses" => access.CanManageExpenses,
                    _ => false
                },
                _ => false
            };
        }
    }
}