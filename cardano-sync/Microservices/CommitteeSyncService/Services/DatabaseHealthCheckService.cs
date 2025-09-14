using Npgsql;

namespace CommitteeSyncService.Services;

/// <summary>
/// Service for monitoring the health of backup databases
/// Provides real-time status and performance metrics
/// </summary>
public class DatabaseHealthCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthCheckService> _logger;
    private readonly string[] _connectionNames;

    public DatabaseHealthCheckService(
        IConfiguration configuration,
        ILogger<DatabaseHealthCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionNames = configuration.GetSection("DatabaseSync:FailoverOrder").Get<string[]>() ?? new[] { "BackupDatabase1" };
    }

    /// <summary>
    /// Get comprehensive health status of all backup databases
    /// </summary>
    public async Task<Dictionary<string, DatabaseHealthStatus>> GetHealthStatusAsync()
    {
        var healthStatuses = new Dictionary<string, DatabaseHealthStatus>();

        foreach (var connectionName in _connectionNames)
        {
            try
            {
                var status = await CheckDatabaseHealthAsync(connectionName);
                healthStatuses[connectionName] = status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking health for {ConnectionName}: {Message}", connectionName, ex.Message);
                healthStatuses[connectionName] = new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    LastChecked = DateTime.UtcNow,
                    ErrorMessage = ex.Message,
                    ResponseTimeMs = -1
                };
            }
        }

        return healthStatuses;
    }

    /// <summary>
    /// Check health of a specific database connection
    /// </summary>
    private async Task<DatabaseHealthStatus> CheckDatabaseHealthAsync(string connectionName)
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrEmpty(connectionString))
        {
            return new DatabaseHealthStatus
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = "Connection string not found",
                ResponseTimeMs = -1
            };
        }

        var startTime = DateTime.UtcNow;
        var status = new DatabaseHealthStatus
        {
            ConnectionName = connectionName,
            LastChecked = startTime
        };

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Test basic connectivity
            using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync();

            // Test schema access
            var schemaCommand = new NpgsqlCommand("SELECT current_schema()", connection);
            var schema = await schemaCommand.ExecuteScalarAsync() as string;

            // Test function access (check if grest schema exists and has expected functions)
            var functionCheckCommand = new NpgsqlCommand(@"
                SELECT COUNT(*) 
                FROM information_schema.routines 
                WHERE routine_schema = 'grest' 
                AND routine_name IN ('committee_info', 'committee_votes', 'treasury_withdrawals', 'totals')", connection);

            var functionCount = await functionCheckCommand.ExecuteScalarAsync();
            var functionCountInt = Convert.ToInt32(functionCount);

            var endTime = DateTime.UtcNow;
            var responseTime = (endTime - startTime).TotalMilliseconds;

            status.IsHealthy = true;
            status.ResponseTimeMs = (long)responseTime;
            status.Schema = schema;
            status.AvailableFunctions = functionCountInt;
            status.ConnectionString = MaskConnectionString(connectionString);

            _logger.LogInformation("✅ Health check passed for {ConnectionName}: Response time {ResponseTime}ms, Schema: {Schema}, Functions: {FunctionCount}",
                connectionName, responseTime, schema, functionCountInt);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var responseTime = (endTime - startTime).TotalMilliseconds;

            status.IsHealthy = false;
            status.ResponseTimeMs = (long)responseTime;
            status.ErrorMessage = ex.Message;
            status.ConnectionString = MaskConnectionString(connectionString);

            _logger.LogWarning("⚠️ Health check failed for {ConnectionName}: {Message} (Response time: {ResponseTime}ms)",
                connectionName, ex.Message, responseTime);
        }

        return status;
    }

    /// <summary>
    /// Test specific queries on backup databases
    /// </summary>
    public async Task<Dictionary<string, QueryTestResult>> TestQueriesAsync()
    {
        var results = new Dictionary<string, QueryTestResult>();

        foreach (var connectionName in _connectionNames)
        {
            try
            {
                var result = await TestQueryOnDatabaseAsync(connectionName);
                results[connectionName] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error testing queries on {ConnectionName}: {Message}", connectionName, ex.Message);
                results[connectionName] = new QueryTestResult
                {
                    ConnectionName = connectionName,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    LastTested = DateTime.UtcNow
                };
            }
        }

        return results;
    }

    /// <summary>
    /// Test specific queries on a database
    /// </summary>
    private async Task<QueryTestResult> TestQueryOnDatabaseAsync(string connectionName)
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrEmpty(connectionString))
        {
            return new QueryTestResult
            {
                ConnectionName = connectionName,
                IsSuccessful = false,
                ErrorMessage = "Connection string not found",
                LastTested = DateTime.UtcNow
            };
        }

        var result = new QueryTestResult
        {
            ConnectionName = connectionName,
            LastTested = DateTime.UtcNow
        };

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Test committee_info function
            var committeeInfoCommand = new NpgsqlCommand("SELECT COUNT(*) FROM grest.committee_info()", connection);
            var committeeInfoCount = await committeeInfoCommand.ExecuteScalarAsync();
            result.CommitteeInfoCount = Convert.ToInt32(committeeInfoCount);

            // Test committee_votes function
            var committeeVotesCommand = new NpgsqlCommand("SELECT COUNT(*) FROM grest.committee_votes('test')", connection);
            var committeeVotesCount = await committeeVotesCommand.ExecuteScalarAsync();
            result.CommitteeVotesCount = Convert.ToInt32(committeeVotesCount);

            // Test treasury_withdrawals function
            var treasuryCommand = new NpgsqlCommand("SELECT COUNT(*) FROM grest.treasury_withdrawals()", connection);
            var treasuryCount = await treasuryCommand.ExecuteScalarAsync();
            result.TreasuryWithdrawalsCount = Convert.ToInt32(treasuryCount);

            // Test totals function
            var totalsCommand = new NpgsqlCommand("SELECT COUNT(*) FROM grest.totals(1)", connection);
            var totalsCount = await totalsCommand.ExecuteScalarAsync();
            result.TotalsCount = Convert.ToInt32(totalsCount);

            result.IsSuccessful = true;
            _logger.LogInformation("✅ Query test successful for {ConnectionName}: Committee Info: {CommitteeInfo}, Votes: {Votes}, Treasury: {Treasury}, Totals: {Totals}",
                connectionName, result.CommitteeInfoCount, result.CommitteeVotesCount, result.TreasuryWithdrawalsCount, result.TotalsCount);
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogWarning("⚠️ Query test failed for {ConnectionName}: {Message}", connectionName, ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Mask sensitive information in connection string
    /// </summary>
    private string MaskConnectionString(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            var maskedParts = parts.Select(part =>
            {
                if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                    return "Password=***";
                if (part.StartsWith("Username=", StringComparison.OrdinalIgnoreCase))
                    return "Username=***";
                return part;
            });
            return string.Join(";", maskedParts);
        }
        catch
        {
            return "***";
        }
    }
}

/// <summary>
/// Database health status information
/// </summary>
public class DatabaseHealthStatus
{
    public string? ConnectionName { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Schema { get; set; }
    public int AvailableFunctions { get; set; }
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Query test result information
/// </summary>
public class QueryTestResult
{
    public string? ConnectionName { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime LastTested { get; set; }
    public string? ErrorMessage { get; set; }
    public int CommitteeInfoCount { get; set; }
    public int CommitteeVotesCount { get; set; }
    public int TreasuryWithdrawalsCount { get; set; }
    public int TotalsCount { get; set; }
}
