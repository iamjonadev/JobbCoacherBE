using System.Data;
using Dapper;
using HaidersAPI.DTOs;
using HaidersAPI.Models;
using HaidersAPI.Data;

namespace HaidersAPI.Helpers
{
    /// <summary>
    /// Data access helper using stored procedures for HaidersAPI
    /// Following InventoryBE patterns with enterprise-grade SQL operations
    /// </summary>
    public class AdoteamSQL
    {
        private readonly AdoteamDataContext _dapper;

        public AdoteamSQL(IConfiguration config)
        {
            _dapper = new AdoteamDataContext(config);
        }

        /// <summary>
        /// Get user's accessible clients using stored procedure
        /// </summary>
        public IEnumerable<ClientModel> GetUserClients(int userId)
        {
            DynamicParameters parameters = new();
            parameters.Add("@UserId", userId, DbType.Int32);
            
            return _dapper.LoadDataStoredProcAsync<ClientModel>(
                "[fakturering].[sp_GetUserClients]", 
                parameters
            ).Result;
        }

        /// <summary>
        /// Validate user permission using stored procedure
        /// </summary>
        public bool ValidatePermission(int userId, Guid? clientId, string action, string resource)
        {
            DynamicParameters parameters = new();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@ClientId", clientId, DbType.Guid);
            parameters.Add("@Action", action, DbType.String);
            parameters.Add("@Resource", resource, DbType.String);
            parameters.Add("@HasPermission", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            
            _dapper.ExecuteStoredProcedureAsync("[fakturering].[sp_ValidateUserPermission]", parameters).Wait();
            
            return parameters.Get<bool>("@HasPermission");
        }

        /// <summary>
        /// Get user's specific client access details
        /// </summary>
        public UserClientAccessModel? GetUserClientAccess(int userId, Guid clientId)
        {
            string sql = @"
                SELECT uca.UserId, uca.ClientId, uca.AccessLevel, uca.CanViewFinancials, 
                       uca.CanManageInvoices, uca.CanManageExpenses, uca.CanViewReports,
                       uca.CreatedDate, uca.CreatedBy, 1 as IsActive
                FROM [fakturering].[UserClientAccess] uca 
                WHERE uca.UserId = @UserId AND uca.ClientId = @ClientId";
            
            DynamicParameters parameters = new();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@ClientId", clientId, DbType.Guid);
            
            return _dapper.LoadDataSingleWithParameters<UserClientAccessModel>(sql, parameters);
        }

        /// <summary>
        /// Grant client access to user
        /// </summary>
        public bool GrantClientAccess(GrantAccessDTO request, int grantedBy)
        {
            string sql = @"
                INSERT INTO [fakturering].[UserClientAccess] 
                (UserId, ClientId, AccessLevel, CanViewFinancials, CanManageInvoices, 
                 CanManageExpenses, CanViewReports, CreatedBy, CreatedDate)
                VALUES 
                (@UserId, @ClientId, @AccessLevel, @CanViewFinancials, @CanManageInvoices,
                 @CanManageExpenses, @CanViewReports, @GrantedBy, GETDATE())";
            
            DynamicParameters parameters = new();
            parameters.Add("@UserId", request.UserId, DbType.Int32);
            parameters.Add("@ClientId", request.ClientId, DbType.Guid);
            parameters.Add("@AccessLevel", request.AccessLevel, DbType.String);
            parameters.Add("@CanViewFinancials", request.CanViewFinancials, DbType.Boolean);
            parameters.Add("@CanManageInvoices", request.CanManageInvoices, DbType.Boolean);
            parameters.Add("@CanManageExpenses", request.CanManageExpenses, DbType.Boolean);
            parameters.Add("@CanViewReports", request.CanViewReports, DbType.Boolean);
            parameters.Add("@GrantedBy", grantedBy, DbType.Int32);
            
            return _dapper.ExecuteSqlWithParameters(sql, parameters);
        }

        /// <summary>
        /// Check if user exists in InventoryBE Users table
        /// </summary>
        public bool CheckUserExists(int userId)
        {
            string sql = "SELECT COUNT(*) FROM [dbo].[Users] WHERE [user_id] = @UserId AND [active] = 1";
            
            DynamicParameters parameters = new();
            parameters.Add("@UserId", userId, DbType.Int32);
            
            var count = _dapper.LoadDataSingleIntWithParameters(sql, parameters);
            return count > 0;
        }

        /// <summary>
        /// Check if client exists
        /// </summary>
        public bool CheckClientExists(Guid clientId)
        {
            string sql = "SELECT COUNT(*) FROM [fakturering].[Clients] WHERE [Id] = @ClientId AND [IsActive] = 1";
            
            DynamicParameters parameters = new();
            parameters.Add("@ClientId", clientId, DbType.Guid);
            
            var count = _dapper.LoadDataSingleIntWithParameters(sql, parameters);
            return count > 0;
        }

        /// <summary>
        /// Generate next invoice number using stored procedure
        /// </summary>
        public string GenerateInvoiceNumber(int? year = null)
        {
            DynamicParameters parameters = new();
            parameters.Add("@Year", year, DbType.Int32);
            parameters.Add("@InvoiceNumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
            
            _dapper.ExecuteStoredProcedureAsync("[fakturering].[sp_GenerateInvoiceNumber]", parameters).Wait();
            
            return parameters.Get<string>("@InvoiceNumber");
        }

        /// <summary>
        /// Generate OCR number using stored procedure
        /// </summary>
        public string GenerateOCRNumber(string invoiceNumber)
        {
            DynamicParameters parameters = new();
            parameters.Add("@InvoiceNumber", invoiceNumber, DbType.String);
            parameters.Add("@OCRNumber", dbType: DbType.String, size: 25, direction: ParameterDirection.Output);
            
            _dapper.ExecuteStoredProcedureAsync("[fakturering].[sp_GenerateOCRNumber]", parameters).Wait();
            
            return parameters.Get<string>("@OCRNumber");
        }
    }
}