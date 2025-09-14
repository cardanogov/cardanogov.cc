using DrepSyncService.ApiResponses;
using Npgsql;
using System.Data;
using System.Net.Sockets;
using System.Text.Json;

namespace DrepSyncService.Services;

/// <summary>
/// Service for syncing data from backup databases
/// </summary>
public class DatabaseSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly string _schema;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;
    private readonly int _connectionTimeoutSeconds;
    private readonly int _commandTimeoutSeconds;
    private readonly bool _enableFailover;
    private readonly string[] _failoverOrder;

    // Global DB operation throttling
    private static SemaphoreSlim? _globalDbSemaphore;
    private static int _activeDbOps;
    private readonly int _maxConcurrentDbOps;

    // Circuit breaker state for each database
    private readonly Dictionary<string, DateTime> _databaseFailureTimestamps = new();
    private readonly Dictionary<string, int> _databaseFailureCounts = new();
    private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(5);
    private readonly int _circuitBreakerThreshold = 3;

    public DatabaseSyncService(IConfiguration configuration, ILogger<DatabaseSyncService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _schema = configuration["DatabaseSync:Schema"] ?? "grest";
        _maxRetries = int.Parse(configuration["DatabaseSync:MaxRetries"] ?? "3");
        _retryDelayMs = int.Parse(configuration["DatabaseSync:RetryDelayMs"] ?? "1000");
        _connectionTimeoutSeconds = int.Parse(configuration["DatabaseSync:ConnectionTimeoutSeconds"] ?? "30");
        _commandTimeoutSeconds = int.Parse(configuration["DatabaseSync:CommandTimeoutSeconds"] ?? "60");
        _enableFailover = bool.Parse(configuration["DatabaseSync:EnableFailover"] ?? "true");
        _failoverOrder = configuration.GetSection("DatabaseSync:FailoverOrder").Get<string[]>() ?? new[] { "BackupDatabase1" };

        // Initialize global DB semaphore (default 8)
        _maxConcurrentDbOps = int.TryParse(configuration["DatabaseSync:MaxConcurrentDbOperations"], out var m) ? Math.Max(1, m) : 8;
        _globalDbSemaphore ??= new SemaphoreSlim(_maxConcurrentDbOps, _maxConcurrentDbOps);
    }

    /// <summary>
    /// Get DRep list from backup database
    /// </summary>
    public async Task<DrepListApiResponse[]> GetDrepListAsync()
    {
        var query = $@"
            SELECT *
            FROM {_schema}.drep_list()
            ORDER BY drep_id";

        return await ExecuteQueryWithFailoverAsync<DrepListApiResponse>(query, MapDrepList);
    }

    /// <summary>
    /// Get DRep info from backup database
    /// </summary>
    public async Task<DrepInfoApiResponse[]> GetDrepInfoAsync(string[] drepIds)
    {
        if (drepIds == null || drepIds.Length == 0)
            return Array.Empty<DrepInfoApiResponse>();

        var drepIdsParam = string.Join(",", drepIds.Select((_, i) => $"@drepId{i}"));
        var query = $@"
            SELECT 
                *
            FROM {_schema}.drep_info(ARRAY[{drepIdsParam}])
            ORDER BY drep_id";

        var parameters = new Dictionary<string, object>();
        for (int i = 0; i < drepIds.Length; i++)
        {
            parameters[$"@drepId{i}"] = drepIds[i];
        }

        return await ExecuteQueryWithFailoverAsync<DrepInfoApiResponse>(query, MapDrepInfo, parameters);
    }

    /// <summary>
    /// Get DRep metadata from backup database
    /// </summary>
    public async Task<DrepMetadataApiResponse[]> GetDrepMetadataAsync(string[] drepIds)
    {
        if (drepIds == null || drepIds.Length == 0)
            return Array.Empty<DrepMetadataApiResponse>();

        var drepIdsParam = string.Join(",", drepIds.Select((_, i) => $"@drepId{i}"));
        var query = $@"
            SELECT 
               *
            FROM {_schema}.drep_metadata(ARRAY[{drepIdsParam}])
            ORDER BY drep_id";

        var parameters = new Dictionary<string, object>();
        for (int i = 0; i < drepIds.Length; i++)
        {
            parameters[$"@drepId{i}"] = drepIds[i];
        }

        return await ExecuteQueryWithFailoverAsync<DrepMetadataApiResponse>(query, MapDrepMetadata, parameters);
    }

    /// <summary>
    /// Get DRep voting power history from backup database
    /// </summary>
    public async Task<DrepVotingPowerHistoryApiResponse[]> GetDrepVotingPowerHistoryAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.drep_history()
            ORDER BY epoch_no DESC, drep_id";

        return await ExecuteQueryWithFailoverAsync<DrepVotingPowerHistoryApiResponse>(query, MapDrepVotingPowerHistory);
    }

    /// <summary>
    /// Get DRep delegators from backup database
    /// </summary>
    public async Task<DrepDelegatorsApiResponse[]> GetDrepDelegatorsAsync(string drepId)
    {
        var query = $@"
            SELECT 
              *
            FROM {_schema}.drep_delegators(@drepId)
            ORDER BY amount DESC";

        var parameters = new Dictionary<string, object> { { "@drepId", drepId } };
        return await ExecuteQueryWithFailoverAsync<DrepDelegatorsApiResponse>(query, MapDrepDelegators, parameters);
    }

    /// <summary>
    /// Get DRep epoch summary from backup database
    /// </summary>
    public async Task<DrepEpochSummaryApiResponse[]> GetDrepEpochSummaryAsync()
    {
        var query = $@"
            SELECT 
              *
            FROM {_schema}.drep_epoch_summary()
            ORDER BY epoch_no DESC";

        return await ExecuteQueryWithFailoverAsync<DrepEpochSummaryApiResponse>(query, MapDrepEpochSummary);
    }

    /// <summary>
    /// Get DRep updates from backup database
    /// </summary>
    public async Task<DrepUpdatesApiResponse[]> GetDrepUpdatesAsync()
    {
        var query = $@"
            SELECT 
                *
            FROM {_schema}.drep_updates()
            ORDER BY block_time DESC";

        return await ExecuteQueryWithFailoverAsync<DrepUpdatesApiResponse>(query, MapDrepUpdates);
    }

    /// <summary>
    /// Get account updates from backup database for specific stake addresses
    /// </summary>
    public async Task<AccountUpdatesApiResponse[]> GetAccountUpdatesAsync(string[] stakeAddresses)
    {
        var query = $@"
            SELECT 
             *
            FROM {_schema}.account_updates(@stakeAddresses)";

        var parameters = new Dictionary<string, object>
        {
            ["@stakeAddresses"] = stakeAddresses
        };

        return await ExecuteQueryWithFailoverAsync<AccountUpdatesApiResponse>(query, MapAccountUpdates, parameters);
    }

    /// <summary>
    /// Execute query with failover support
    /// </summary>
    private async Task<T[]> ExecuteQueryWithFailoverAsync<T>(
        string query,
        Func<NpgsqlDataReader, T> mapper,
        Dictionary<string, object>? parameters = null)
    {
        var lastException = new Exception("No databases available");

        var available = _failoverOrder.Where(k => !IsCircuitBreakerOpen(k)).ToArray();
        if (!available.Any())
        {
            _logger.LogWarning("All databases in circuit breaker open. Trying primary");
            available = new[] { _failoverOrder.First() };
        }

        foreach (var connectionKey in available)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString(connectionKey);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Connection string not found for {ConnectionKey}", connectionKey);
                    continue;
                }

                _logger.LogInformation("Attempting to connect to {ConnectionKey} (Failures: {Count})", connectionKey, _databaseFailureCounts.GetValueOrDefault(connectionKey, 0));
                var result = await ExecuteQueryAsync<T>(connectionString, query, mapper, parameters);
                ResetDatabaseFailureState(connectionKey);
                _logger.LogInformation("Successfully retrieved data from {ConnectionKey}", connectionKey);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                RecordDatabaseFailure(connectionKey, ex);
                _logger.LogWarning(ex, "Failed to connect to {ConnectionKey}: {Message}", connectionKey, ex.Message);

                if (!_enableFailover)
                    break;
            }
        }

        _logger.LogError(lastException, "All backup databases failed");
        throw new InvalidOperationException("All backup databases are unavailable", lastException);
    }

    /// <summary>
    /// Execute query on a specific database with retry, cancellation, and timeouts
    /// </summary>
    private async Task<T[]> ExecuteQueryAsync<T>(
        string connectionString,
        string query,
        Func<NpgsqlDataReader, T> mapper,
        Dictionary<string, object>? parameters = null)
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = _connectionTimeoutSeconds,
            CommandTimeout = _commandTimeoutSeconds,
            KeepAlive = 30,
            TcpKeepAliveTime = 30,
            TcpKeepAliveInterval = 5,
            MaxAutoPrepare = 0,
            ApplicationName = "DrepSyncService"
        };

        Exception? last = null;
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                // Throttle global DB operations
                await _globalDbSemaphore!.WaitAsync();
                Interlocked.Increment(ref _activeDbOps);
                try
                {
                    using var connection = new NpgsqlConnection(csb.ToString());
                    using var command = new NpgsqlCommand(query, connection);
                    command.CommandTimeout = _commandTimeoutSeconds;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var results = new List<T>();
                    using var openCts = new CancellationTokenSource(TimeSpan.FromSeconds(_connectionTimeoutSeconds));
                    await connection.OpenAsync(openCts.Token);

                    using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(_commandTimeoutSeconds));
                    using var reader = await command.ExecuteReaderAsync(readCts.Token);

                    while (await reader.ReadAsync(readCts.Token))
                    {
                        results.Add(mapper(reader));
                    }

                    return results.ToArray();
                }
                finally
                {
                    if (_activeDbOps > 0) Interlocked.Decrement(ref _activeDbOps);
                    _globalDbSemaphore!.Release();
                }
            }
            catch (Exception ex) when (IsRetriableException(ex))
            {
                last = ex;
                if (attempt < _maxRetries)
                {
                    var delay = _retryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
        }

        throw last ?? new Exception("All attempts failed");
    }

    private static bool IsRetriableException(Exception ex)
    {
        if (ex is TimeoutException || ex is SocketException)
            return true;

        if (ex is NpgsqlException nx)
        {
            var msg = nx.Message?.ToLowerInvariant() ?? string.Empty;
            if (nx.IsTransient || msg.Contains("too many clients") || msg.Contains("server_login_retry") || msg.Contains("timeout") || msg.Contains("connection") || msg.Contains("network") || msg.Contains("08p01"))
                return true;
        }

        return false;
    }

    private bool IsCircuitBreakerOpen(string connectionKey)
    {
        if (!_databaseFailureTimestamps.TryGetValue(connectionKey, out var lastFailure) ||
            !_databaseFailureCounts.TryGetValue(connectionKey, out var failureCount))
        {
            return false;
        }

        if (failureCount < _circuitBreakerThreshold)
        {
            return false;
        }

        if (DateTime.UtcNow - lastFailure > _circuitBreakerTimeout)
        {
            _logger.LogInformation("Circuit breaker timeout expired for {ConnectionKey}, allowing retry", connectionKey);
            return false;
        }

        return true;
    }

    private void RecordDatabaseFailure(string connectionKey, Exception ex)
    {
        _databaseFailureTimestamps[connectionKey] = DateTime.UtcNow;
        _databaseFailureCounts[connectionKey] = _databaseFailureCounts.GetValueOrDefault(connectionKey, 0) + 1;

        if (_databaseFailureCounts[connectionKey] >= _circuitBreakerThreshold)
        {
            _logger.LogWarning("Circuit breaker opened for {ConnectionKey} after {FailureCount} failures. Will retry after {Timeout}",
                connectionKey, _databaseFailureCounts[connectionKey], _circuitBreakerTimeout);
        }
    }

    private void ResetDatabaseFailureState(string connectionKey)
    {
        if (_databaseFailureCounts.ContainsKey(connectionKey))
        {
            _logger.LogInformation("Resetting failure state for {ConnectionKey}", connectionKey);
            _databaseFailureCounts.Remove(connectionKey);
            _databaseFailureTimestamps.Remove(connectionKey);
        }
    }

    // Mapping functions
    private DrepListApiResponse MapDrepList(NpgsqlDataReader reader)
    {
        return new DrepListApiResponse
        {
            drep_id = reader["drep_id"]?.ToString(),
            hex = reader["hex"]?.ToString(),
            has_script = reader["has_script"] as bool?,
            registered = reader["registered"] as bool?
        };
    }

    private DrepInfoApiResponse MapDrepInfo(NpgsqlDataReader reader)
    {
        return new DrepInfoApiResponse
        {
            drep_id = reader["drep_id"]?.ToString(),
            hex = reader["hex"]?.ToString(),
            has_script = reader["has_script"] as bool?,
            registered = reader["registered"] as bool?,
            deposit = reader["deposit"]?.ToString(),
            active = reader["active"] as bool?,
            expires_epoch_no = ParseActiveEpochNo(reader["expires_epoch_no"]),
            amount = reader["amount"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString()
        };
    }

    private DrepMetadataApiResponse MapDrepMetadata(NpgsqlDataReader reader)
    {
        return new DrepMetadataApiResponse
        {
            drep_id = reader["drep_id"]?.ToString(),
            hex = reader["hex"]?.ToString(),
            has_script = reader["has_script"] as bool?,
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = reader["meta_json"]?.ToString(),
            bytes = reader["bytes"]?.ToString(),
            warning = reader["warning"]?.ToString(),
            language = reader["language"]?.ToString(),
            comment = reader["comment"]?.ToString(),
            is_valid = reader["is_valid"] as bool?
        };
    }

    private DrepVotingPowerHistoryApiResponse MapDrepVotingPowerHistory(NpgsqlDataReader reader)
    {
        return new DrepVotingPowerHistoryApiResponse
        {
            drep_id = reader["drep_id"]?.ToString(),
            epoch_no = reader["epoch_no"] as int?,
            amount = reader["amount"]?.ToString()
        };
    }

    private DrepDelegatorsApiResponse MapDrepDelegators(NpgsqlDataReader reader)
    {
        return new DrepDelegatorsApiResponse
        {
            stake_address = reader["stake_address"]?.ToString(),
            stake_address_hex = reader["stake_address_hex"]?.ToString(),
            script_hash = reader["script_hash"]?.ToString(),
            epoch_no = reader["epoch_no"] as int?,
            amount = reader["amount"]?.ToString()
        };
    }

    private DrepEpochSummaryApiResponse MapDrepEpochSummary(NpgsqlDataReader reader)
    {
        return new DrepEpochSummaryApiResponse
        {
            EpochNo = reader["epoch_no"] as int?,
            Amount = reader["amount"]?.ToString(),
            Dreps = reader["dreps"] as int?
        };
    }

    private DrepUpdatesApiResponse MapDrepUpdates(NpgsqlDataReader reader)
    {
        return new DrepUpdatesApiResponse
        {
            drep_id = reader["drep_id"]?.ToString() ?? "",
            hex = reader["hex"]?.ToString() ?? "",
            has_script = reader["has_script"] as bool? ?? false,
            update_tx_hash = reader["update_tx_hash"]?.ToString() ?? "",
            cert_index = reader["cert_index"] as int? ?? 0,
            block_time = reader["block_time"] as int? ?? 0,
            action = reader["action"]?.ToString() ?? "",
            deposit = reader["deposit"]?.ToString() ?? "",
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = reader["meta_json"]?.ToString()
        };
    }

    private AccountUpdatesApiResponse MapAccountUpdates(NpgsqlDataReader reader)
    {
        return new AccountUpdatesApiResponse
        {
            stake_address = reader["stake_address"]?.ToString(),
            updates = JsonSerializer.Deserialize<List<AccountUpdateRecord>>(reader["updates"]?.ToString() ?? "[]")
        };
    }

    private int? ParseActiveEpochNo(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var result = Convert.ToInt32(value);
            return result;
        }
        catch
        {
            _logger.LogWarning("⚠️ Failed to parse active epoch no JSON: {Value}", value);
            return null;
        }
    }
}
