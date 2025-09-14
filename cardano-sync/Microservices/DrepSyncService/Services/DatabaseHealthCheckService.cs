using Npgsql;

namespace DrepSyncService.Services;

/// <summary>
/// Service for checking the health of backup databases
/// </summary>
public class DatabaseHealthCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthCheckService> _logger;
    private readonly string _schema;

    public DatabaseHealthCheckService(IConfiguration configuration, ILogger<DatabaseHealthCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _schema = configuration["DatabaseSync:Schema"] ?? "grest";
    }

    /// <summary>
    /// Check health of all backup databases
    /// </summary>
    public async Task<DatabaseHealthResult> CheckAllDatabasesAsync()
    {
        var result = new DatabaseHealthResult();
        var failoverOrder = _configuration.GetSection("DatabaseSync:FailoverOrder").Get<string[]>() ?? new[] { "BackupDatabase1" };

        foreach (var connectionKey in failoverOrder)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString(connectionKey);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Connection string not found for {ConnectionKey}", connectionKey);
                    continue;
                }

                var healthResult = await CheckDatabaseHealthAsync(connectionString, connectionKey);
                result.DatabaseResults[connectionKey] = healthResult;

                if (healthResult.IsSuccessful)
                {
                    result.OverallStatus = "Healthy";
                    break; // Found a healthy database
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health of {ConnectionKey}", connectionKey);
                result.DatabaseResults[connectionKey] = new DatabaseHealthResult.DatabaseResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        if (result.DatabaseResults.Values.All(r => !r.IsSuccessful))
        {
            result.OverallStatus = "Unhealthy";
        }

        return result;
    }

    /// <summary>
    /// Check health of a specific database
    /// </summary>
    private async Task<DatabaseHealthResult.DatabaseResult> CheckDatabaseHealthAsync(string connectionString, string connectionKey)
    {
        var result = new DatabaseHealthResult.DatabaseResult();

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Test basic connectivity
            result.CanConnect = true;
            result.ConnectionTime = DateTime.UtcNow;

            // Check if schema exists
            var schemaCheckCommand = new NpgsqlCommand(@"
                SELECT COUNT(*) 
                FROM information_schema.schemata 
                WHERE schema_name = @schema", connection);
            schemaCheckCommand.Parameters.AddWithValue("@schema", _schema);

            var schemaCount = await schemaCheckCommand.ExecuteScalarAsync();
            result.SchemaExists = Convert.ToInt32(schemaCount) > 0;

            if (!result.SchemaExists)
            {
                result.ErrorMessage = $"Schema '{_schema}' not found";
                return result;
            }

            // Check if required functions exist
            var functionCheckCommand = new NpgsqlCommand(@"
                SELECT COUNT(*) 
                FROM information_schema.routines 
                WHERE routine_schema = @schema 
                AND routine_name IN ('drep_list', 'drep_info', 'drep_metadata', 'drep_history', 'drep_delegators', 'drep_epoch_summary', 'drep_updates', 'account_updates')", connection);
            functionCheckCommand.Parameters.AddWithValue("@schema", _schema);

            var functionCount = await functionCheckCommand.ExecuteScalarAsync();
            var functionCountInt = Convert.ToInt32(functionCount);
            result.RequiredFunctionsCount = functionCountInt;
            result.HasRequiredFunctions = functionCountInt >= 8; // We need 8 functions

            if (!result.HasRequiredFunctions)
            {
                result.ErrorMessage = $"Missing required functions. Found {functionCountInt}/8";
                return result;
            }

            // Test function calls
            await TestFunctionCallsAsync(connection, result);

            result.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Health check failed for {ConnectionKey}: {Message}", connectionKey, ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Test function calls to ensure they work correctly
    /// </summary>
    private async Task TestFunctionCallsAsync(NpgsqlConnection connection, DatabaseHealthResult.DatabaseResult result)
    {
        try
        {
            // Test drep_list function
            var drepListCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_list()", connection);
            var drepListCount = await drepListCommand.ExecuteScalarAsync();
            result.DrepListCount = Convert.ToInt32(drepListCount);

            // Test drep_info function
            var drepInfoCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_info(ARRAY['test'])", connection);
            var drepInfoCount = await drepInfoCommand.ExecuteScalarAsync();
            result.DrepInfoCount = Convert.ToInt32(drepInfoCount);

            // Test drep_metadata function
            var drepMetadataCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_metadata(ARRAY['test'])", connection);
            var drepMetadataCount = await drepMetadataCommand.ExecuteScalarAsync();
            result.DrepMetadataCount = Convert.ToInt32(drepMetadataCount);

            // Test drep_voting_power_history function
            var drepVotingPowerCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_history()", connection);
            var drepVotingPowerCount = await drepVotingPowerCommand.ExecuteScalarAsync();
            result.DrepVotingPowerHistoryCount = Convert.ToInt32(drepVotingPowerCount);

            // Test drep_delegators function
            var drepDelegatorsCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_delegators('test')", connection);
            var drepDelegatorsCount = await drepDelegatorsCommand.ExecuteScalarAsync();
            result.DrepDelegatorsCount = Convert.ToInt32(drepDelegatorsCount);

            // Test drep_epoch_summary function
            var drepEpochSummaryCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_epoch_summary()", connection);
            var drepEpochSummaryCount = await drepEpochSummaryCommand.ExecuteScalarAsync();
            result.DrepEpochSummaryCount = Convert.ToInt32(drepEpochSummaryCount);

            // Test drep_updates function
            var drepUpdatesCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.drep_updates()", connection);
            var drepUpdatesCount = await drepUpdatesCommand.ExecuteScalarAsync();
            result.DrepUpdatesCount = Convert.ToInt32(drepUpdatesCount);

            // Test account_updates function
            var accountUpdatesCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {_schema}.account_updates()", connection);
            var accountUpdatesCount = await accountUpdatesCommand.ExecuteScalarAsync();
            result.AccountUpdatesCount = Convert.ToInt32(accountUpdatesCount);

            result.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = $"Function test failed: {ex.Message}";
        }
    }
}

/// <summary>
/// Result of database health check
/// </summary>
public class DatabaseHealthResult
{
    public string OverallStatus { get; set; } = "Unknown";
    public Dictionary<string, DatabaseResult> DatabaseResults { get; set; } = new();

    public class DatabaseResult
    {
        public bool IsSuccessful { get; set; }
        public bool CanConnect { get; set; }
        public bool SchemaExists { get; set; }
        public bool HasRequiredFunctions { get; set; }
        public int RequiredFunctionsCount { get; set; }
        public DateTime? ConnectionTime { get; set; }
        public string? ErrorMessage { get; set; }

        // Function test results
        public int DrepListCount { get; set; }
        public int DrepInfoCount { get; set; }
        public int DrepMetadataCount { get; set; }
        public int DrepVotingPowerHistoryCount { get; set; }
        public int DrepDelegatorsCount { get; set; }
        public int DrepEpochSummaryCount { get; set; }
        public int DrepUpdatesCount { get; set; }
        public int AccountUpdatesCount { get; set; }
    }
}
