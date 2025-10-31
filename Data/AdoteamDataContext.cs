using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Extensions.Http;
using System.Diagnostics;
using HaidersAPI.Models;

namespace HaidersAPI.Data;

public class AdoteamDataContext
{
    private readonly string _connectionString;
    private readonly ILogger<AdoteamDataContext>? _logger;
    private readonly IAsyncPolicy<object?> _retryPolicy;
    private static readonly ActivitySource ActivitySource = new("HaidersAPI.DataAccess");

    public AdoteamDataContext(IConfiguration config, ILogger<AdoteamDataContext>? logger = null)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string");
        _logger = logger;
        
        // Configure connection string for optimal connection pooling
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            MaxPoolSize = 100,
            MinPoolSize = 5,
            ConnectTimeout = 30,
            CommandTimeout = 60,
            Pooling = true,
            ConnectRetryCount = 3,
            ConnectRetryInterval = 10
        };
        _connectionString = builder.ConnectionString;

        // Configure retry policy for transient failures
        _retryPolicy = Policy
            .Handle<SqlException>(ex => IsTransientError(ex))
            .Or<TimeoutException>()
            .OrResult<object?>(null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Exponential backoff
                );
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    private static bool IsTransientError(SqlException ex)
    {
        // Common transient error numbers that should be retried
        var transientErrors = new[]
        {
            -2,     // Timeout
            2,      // Connection timeout
            53,     // Network path not found
            121,    // Semaphore timeout
            233,    // No process on the other end of pipe
            10053,  // Connection broken
            10054,  // Connection reset by peer
            10060,  // Connection timeout
            40197,  // Service has encountered an error processing your request
            40501,  // Service is currently busy
            40613,  // Database is currently unavailable
            49918,  // Cannot process request. Not enough resources to process request
            49919,  // Cannot process create or update request
            49920,  // Cannot process request. Too many operations in progress for subscription
        };

        return transientErrors.Contains(ex.Number);
    }

    /// <summary>
    /// Performs a database health check
    /// </summary>
    public async Task<DatabaseHealthStatus> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();
            
            var result = await connection.QuerySingleAsync<int>("SELECT 1");
            stopwatch.Stop();
            
            var status = new DatabaseHealthStatus
            {
                IsHealthy = result == 1,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                CheckTime = DateTime.UtcNow,
                Message = "Database connection successful"
            };

            _logger?.LogInformation("Adoteam database health check passed in {ResponseTime}ms", status.ResponseTime);
            return status;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var status = new DatabaseHealthStatus
            {
                IsHealthy = false,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                CheckTime = DateTime.UtcNow,
                Message = $"Adoteam database health check failed: {ex.Message}",
                Error = ex
            };

            _logger?.LogError(ex, "Adoteam database health check failed after {ResponseTime}ms", status.ResponseTime);
            return status;
        }
    }

    /// <summary>
    /// Gets database connection pool statistics
    /// </summary>
    public async Task<ConnectionPoolStats> GetConnectionPoolStatsAsync()
    {
        try
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();
            
            var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT 
                    @@CONNECTIONS as TotalConnections,
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) as ActiveSessions,
                    (SELECT COUNT(*) FROM sys.dm_exec_requests) as ActiveRequests
            ");

            return new ConnectionPoolStats
            {
                TotalConnections = stats?.TotalConnections ?? 0,
                ActiveSessions = stats?.ActiveSessions ?? 0,
                ActiveRequests = stats?.ActiveRequests ?? 0,
                CheckTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get connection pool statistics");
            return new ConnectionPoolStats
            {
                CheckTime = DateTime.UtcNow,
                Message = $"Failed to retrieve stats: {ex.Message}"
            };
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        using var activity = ActivitySource.StartActivity($"AdoteamsDatabase.{operationName}");
        activity?.SetTag("database.operation", operationName);
        activity?.SetTag("service", "HaidersAPI");
        
        try
        {
            var result = await operation();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger?.LogError(ex, "Adoteam database operation failed: {Operation}", operationName);
            throw;
        }
    }

    // Synchronous methods
    public IEnumerable<T> LoadData<T>(string sql)
    {
        using var connection = CreateConnection();
        return connection.Query<T>(sql);
    }

    public T LoadDataSingle<T>(string sql)
    {
        using var connection = CreateConnection();
        return connection.QuerySingle<T>(sql);
    }

    public bool ExecuteSql(string sql)
    {
        using var connection = CreateConnection();
        return connection.Execute(sql) > 0;
    }

    public int ExecuteSqlWithRowCount(string sql)
    {
        using var connection = CreateConnection();
        return connection.Execute(sql);
    }

    public bool ExecuteSqlWithParameters(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.Execute(sql, parameters) > 0;
    }

    public IEnumerable<T> LoadDataWithParameters<T>(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.Query<T>(sql, parameters);
    }

    public T? LoadDataSingleOrDefaultWithParameters<T>(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.QuerySingleOrDefault<T>(sql, parameters);
    }

    public T? LoadDataSingleWithParameters<T>(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.QuerySingle<T>(sql, parameters);
    }

    public int LoadDataSingleIntWithParameters(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.QuerySingle<int>(sql, parameters);
    }

    public T? LoadDataFirstOrDefaultWithParameters<T>(string sql, DynamicParameters parameters)
    {
        using var connection = CreateConnection();
        return connection.QueryFirstOrDefault<T>(sql, parameters);
    }

    // Enhanced async methods with retry policies
    public async Task<IEnumerable<T>> LoadDataAsync<T>(string sql)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>(sql);
        }, "LoadData");
    }

    public async Task<T> LoadDataSingleAsync<T>(string sql)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleAsync<T>(sql);
        }, "LoadDataSingle");
    }

    public async Task<bool> ExecuteSqlAsync(string sql)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync(sql) > 0;
        }, "ExecuteSql");
    }

    public async Task<int> ExecuteSqlWithRowCountAsync(string sql)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync(sql);
        }, "ExecuteSqlWithRowCount");
    }

    public async Task<bool> ExecuteSqlWithParametersAsync(string sql, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync(sql, parameters) > 0;
        }, "ExecuteSqlWithParameters");
    }

    public async Task<IEnumerable<T>> LoadDataWithParametersAsync<T>(string sql, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>(sql, parameters);
        }, "LoadDataWithParameters");
    }

    public async Task<T?> LoadDataSingleWithParametersAsync<T>(string sql, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }, "LoadDataSingleWithParameters");
    }

    public async Task<T?> LoadDataSingleOrDefaultWithParametersAsync<T>(string sql, DynamicParameters parameters) where T : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }, "LoadDataSingleOrDefaultWithParameters");
    }

    public async Task<int> LoadDataSingleIntWithParametersAsync(string sql, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, parameters);
        }, "LoadDataSingleIntWithParameters");
    }

    public async Task<T?> LoadDataFirstOrDefaultWithParametersAsync<T>(string sql, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }, "LoadDataFirstOrDefaultWithParameters");
    }

    // Enhanced Stored Procedure Methods with retry policies
    public async Task<IEnumerable<T>> LoadDataStoredProcAsync<T>(string storedProcedure, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }, $"StoredProc.{storedProcedure}");
    }

    public async Task<T?> LoadDataSingleStoredProcAsync<T>(string storedProcedure, DynamicParameters parameters) where T : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }, $"StoredProcSingle.{storedProcedure}");
    }

    public async Task<bool> ExecuteStoredProcedureAsync(string storedProcedure, DynamicParameters parameters)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure) > 0;
        }, $"ExecuteStoredProc.{storedProcedure}");
    }

    // Adoteam-specific methods
    /// <summary>
    /// Executes a transaction with multiple operations
    /// </summary>
    public async Task<bool> ExecuteTransactionAsync(IEnumerable<(string sql, DynamicParameters parameters)> operations)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                foreach (var (sql, parameters) in operations)
                {
                    await connection.ExecuteAsync(sql, parameters, transaction);
                }
                
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }, "ExecuteTransaction");
    }

    /// <summary>
    /// Validates OCR number uniqueness
    /// </summary>
    public async Task<bool> IsOCRUniqueAsync(string ocrNumber)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OCRNumber", ocrNumber);
        
        var count = await LoadDataSingleIntWithParametersAsync(
            "SELECT COUNT(*) FROM Invoices WHERE OCRNumber = @OCRNumber", 
            parameters);
        
        return count == 0;
    }

    /// <summary>
    /// Get the next invoice number for the current year
    /// </summary>
    public async Task<int> GetNextInvoiceNumberAsync()
    {
        var currentYear = DateTime.Now.Year;
        var parameters = new DynamicParameters();
        parameters.Add("@Year", currentYear);
        
        var lastNumber = await LoadDataFirstOrDefaultWithParametersAsync<int?>(
            "SELECT MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT)) FROM Invoices WHERE InvoiceNumber LIKE @Year + '%'", 
            parameters);
        
        return (lastNumber ?? 0) + 1;
    }

    /// <summary>
    /// Validate user permission using stored procedure
    /// </summary>
    public async Task<bool> ValidateUserPermissionAsync(int userId, Guid? clientId, string action, string resource)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@ClientId", clientId);
        parameters.Add("@Action", action);
        parameters.Add("@Resource", resource);
        parameters.Add("@HasPermission", dbType: DbType.Boolean, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("[fakturering].[sp_ValidateUserPermission]", parameters, commandType: CommandType.StoredProcedure);

        return parameters.Get<bool>("@HasPermission");
    }

    /// <summary>
    /// Get user's accessible clients using stored procedure
    /// </summary>
    public async Task<IEnumerable<dynamic>> GetUserClientsAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);

        return await connection.QueryAsync("[fakturering].[sp_GetUserClients]", parameters, commandType: CommandType.StoredProcedure);
    }

    /// <summary>
    /// Generate invoice number using stored procedure
    /// </summary>
    public async Task<string> GenerateInvoiceNumberAsync(int? year = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@Year", year);
        parameters.Add("@InvoiceNumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("[fakturering].[sp_GenerateInvoiceNumber]", parameters, commandType: CommandType.StoredProcedure);

        return parameters.Get<string>("@InvoiceNumber");
    }

    /// <summary>
    /// Generate OCR number using stored procedure
    /// </summary>
    public async Task<string> GenerateOCRNumberAsync(string invoiceNumber)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@InvoiceNumber", invoiceNumber);
        parameters.Add("@OCRNumber", dbType: DbType.String, size: 25, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("[fakturering].[sp_GenerateOCRNumber]", parameters, commandType: CommandType.StoredProcedure);

        return parameters.Get<string>("@OCRNumber");
    }

    /// <summary>
    /// Execute SQL command with parameters
    /// </summary>
    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Query single result with parameters
    /// </summary>
    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    /// <summary>
    /// Query single dynamic result with parameters
    /// </summary>
    public async Task<dynamic?> QueryFirstOrDefaultAsync(string sql, object? parameters = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryFirstOrDefaultAsync(sql, parameters);
    }

    /// <summary>
    /// Query multiple results with parameters
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<T>(sql, parameters);
    }

    /// <summary>
    /// Query multiple dynamic results with parameters
    /// </summary>
    public async Task<IEnumerable<dynamic>> QueryAsync(string sql, object? parameters = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync(sql, parameters);
    }
}