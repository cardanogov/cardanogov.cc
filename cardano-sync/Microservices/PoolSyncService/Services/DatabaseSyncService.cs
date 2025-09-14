using Npgsql;
using PoolSyncService.ApiResponses;
using System.Data;
using System.Net.Sockets;
using System.Text.Json;

namespace PoolSyncService.Services;

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
    public int CommandTimeoutSeconds => _commandTimeoutSeconds;
    private readonly bool _enableFailover;
    private readonly string[] _failoverOrder;

    // Throttle concurrent DB operations across the service
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

        // Initialize global DB semaphore (default 8) and allow override via config
        _maxConcurrentDbOps = int.TryParse(configuration["DatabaseSync:MaxConcurrentDbOperations"], out var m) ? Math.Max(1, m) : 8;
        _globalDbSemaphore ??= new SemaphoreSlim(_maxConcurrentDbOps, _maxConcurrentDbOps);
        _logger.LogInformation("Initialized global DB semaphore with MaxConcurrentDbOperations={MaxOps}", _maxConcurrentDbOps);
    }

    // Public helper to throttle arbitrary DB work (e.g., EF writes) with the same global semaphore
    public async Task WithDbThrottleAsync(Func<Task> operation)
    {
        await _globalDbSemaphore!.WaitAsync();
        var active = Interlocked.Increment(ref _activeDbOps);
        _logger.LogDebug("DB throttle acquired. ActiveOps={Active}/{Max}", active, _maxConcurrentDbOps);
        try
        {
            await operation();
        }
        finally
        {
            var remaining = Interlocked.Decrement(ref _activeDbOps);
            _globalDbSemaphore!.Release();
            _logger.LogDebug("DB throttle released. ActiveOps={Active}/{Max}", remaining, _maxConcurrentDbOps);
        }
    }

    public async Task<T> WithDbThrottleAsync<T>(Func<Task<T>> operation)
    {
        await _globalDbSemaphore!.WaitAsync();
        var active = Interlocked.Increment(ref _activeDbOps);
        _logger.LogDebug("DB throttle acquired. ActiveOps={Active}/{Max}", active, _maxConcurrentDbOps);
        try
        {
            return await operation();
        }
        finally
        {
            var remaining = Interlocked.Decrement(ref _activeDbOps);
            _globalDbSemaphore!.Release();
            _logger.LogDebug("DB throttle released. ActiveOps={Active}/{Max}", remaining, _maxConcurrentDbOps);
        }
    }

    /// <summary>
    /// Get pool list from backup database
    /// </summary>
    public async Task<PoolListApiResponse[]> GetPoolListAsync()
    {
        var query = $@"
            SELECT *
            FROM {_schema}.pool_list()
            ORDER BY pool_id_bech32";

        return await ExecuteQueryWithFailoverAsync<PoolListApiResponse>(query, MapPoolList);
    }

    /// <summary>
    /// Get pool voting power history from backup database
    /// </summary>
    public async Task<PoolVotingPowerHistoryApiResponse[]> GetPoolVotingPowerHistoryAsync()
    {
        var query = $@"
            SELECT *
            FROM {_schema}.pool_voting_power_history()
            ORDER BY epoch_no DESC, amount DESC";

        return await ExecuteQueryWithFailoverAsync<PoolVotingPowerHistoryApiResponse>(query, MapPoolVotingPowerHistory);
    }

    /// <summary>
    /// Get pool metadata for specified pool bech32 IDs from backup database
    /// </summary>
    public async Task<PoolMetadataApiResponse[]> GetPoolMetadataAsync(string[] poolBech32Ids)
    {
        if (poolBech32Ids == null || poolBech32Ids.Length == 0)
            return Array.Empty<PoolMetadataApiResponse>();

        var query = $@"
            SELECT *
            FROM {_schema}.pool_metadata(@poolBech32Ids)
            ORDER BY pool_id_bech32";

        var parameters = new Dictionary<string, object>
        {
            ["@poolBech32Ids"] = poolBech32Ids
        };

        return await ExecuteQueryWithFailoverAsync<PoolMetadataApiResponse>(query, MapPoolMetadata, parameters);
    }

    /// <summary>
    /// Get pool stake snapshot for a specific pool bech32 ID from backup database
    /// </summary>
    public async Task<PoolStakeSnapshotApiResponse[]> GetPoolStakeSnapshotAsync(string poolBech32Id)
    {
        var query = $@"
            SELECT *
            FROM {_schema}.pool_stake_snapshot(@poolBech32Id)
            ORDER BY epoch_no DESC";

        var parameters = new Dictionary<string, object>
        {
            ["@poolBech32Id"] = poolBech32Id
        };

        return await ExecuteQueryWithFailoverAsync<PoolStakeSnapshotApiResponse>(query, MapPoolStakeSnapshot, parameters);
    }

    /// <summary>
    /// Get pool delegators for a specific pool bech32 ID from backup database
    /// </summary>
    public async Task<PoolDelegatorsApiResponse[]> GetPoolDelegatorsAsync(string poolBech32Id)
    {
        var query = $@"
            SELECT *
            FROM {_schema}.pool_delegators(@poolBech32Id)
            ORDER BY amount DESC";

        var parameters = new Dictionary<string, object>
        {
            ["@poolBech32Id"] = poolBech32Id
        };

        return await ExecuteQueryWithFailoverAsync<PoolDelegatorsApiResponse>(query, MapPoolDelegators, parameters);
    }

    /// <summary>
    /// Get UTXO info for specified UTXO references from backup database
    /// </summary>
    public async Task<UtxoInfoApiResponse[]> GetUtxoInfoAsync(List<string> utxoRefs)
    {
        if (utxoRefs == null || utxoRefs.Count == 0)
            return Array.Empty<UtxoInfoApiResponse>();

        var query = $@"
            SELECT tx_hash, tx_index, stake_address, epoch_no, block_time
            FROM {_schema}.utxo_info(@utxoRefs)";

        var parameters = new Dictionary<string, object>
        {
            ["@utxoRefs"] = utxoRefs
        };

        return await ExecuteQueryWithFailoverAsync<UtxoInfoApiResponse>(query, MapUtxoInfo, parameters);
    }

    /// <summary>
    /// Get pool updates from backup database
    /// </summary>
    public async Task<PoolUpdatesApiResponse[]> GetPoolUpdatesAsync(string poolBech32Id)
    {
        var query = $@"
            SELECT *
            FROM {_schema}.pool_updates(@poolBech32Id)";

        var parameters = new Dictionary<string, object>
        {
            ["@poolBech32Id"] = poolBech32Id
        };

        return await ExecuteQueryWithFailoverAsync<PoolUpdatesApiResponse>(query, MapPoolUpdates, parameters);
    }

    /// <summary>
    /// Get pool information for specified pool bech32 IDs from backup database
    /// </summary>
    public async Task<PoolInformationApiResponse[]> GetPoolInformationAsync(string[] poolBech32Ids)
    {
        if (poolBech32Ids == null || poolBech32Ids.Length == 0)
            return Array.Empty<PoolInformationApiResponse>();

        var query = $@"
            SELECT *, CAST(sigma AS NUMERIC(19, 4)) 
            FROM {_schema}.pool_info(@poolBech32Ids)
            ORDER BY pool_id_bech32";

        var parameters = new Dictionary<string, object>
        {
            ["@poolBech32Ids"] = poolBech32Ids
        };

        return await ExecuteQueryWithFailoverAsync<PoolInformationApiResponse>(query, MapPoolInformation, parameters);
    }

    /// <summary>
    /// Get comprehensive database statistics for logging
    /// </summary>
    public string GetDatabaseStats()
    {
        var circuitBreakerStats = string.Join(", ", _databaseFailureCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        return $"Schema: {_schema}, MaxRetries: {_maxRetries}, RetryDelay: {_retryDelayMs}ms, ConnectionTimeout: {_connectionTimeoutSeconds}s, CommandTimeout: {_commandTimeoutSeconds}s, Failover: {_enableFailover}, CircuitBreaker: [{circuitBreakerStats}], ActiveDbOps: {_activeDbOps}, MaxConcurrentDbOps: {_maxConcurrentDbOps}";
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
        var availableDatabases = _failoverOrder.Where(db => !IsCircuitBreakerOpen(db)).ToArray();

        if (!availableDatabases.Any())
        {
            _logger.LogWarning("All databases are in circuit breaker open state. Attempting with primary database anyway.");
            availableDatabases = new[] { _failoverOrder.First() };
        }

        foreach (var connectionKey in availableDatabases)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString(connectionKey);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Connection string not found for {ConnectionKey}", connectionKey);
                    continue;
                }

                _logger.LogInformation("Attempting to connect to {ConnectionKey} (Failures: {FailureCount})",
                    connectionKey, _databaseFailureCounts.GetValueOrDefault(connectionKey, 0));

                var result = await ExecuteQueryAsync<T>(connectionString, query, mapper, parameters);

                // Reset failure count on success
                ResetDatabaseFailureState(connectionKey);

                _logger.LogInformation("Successfully retrieved data from {ConnectionKey}", connectionKey);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                RecordDatabaseFailure(connectionKey, ex);

                _logger.LogWarning(ex, "Failed to connect to {ConnectionKey}: {Message} (Failure count: {FailureCount})",
                    connectionKey, ex.Message, _databaseFailureCounts.GetValueOrDefault(connectionKey, 0));

                if (!_enableFailover)
                    break;

                // Add delay between failover attempts
                if (connectionKey != availableDatabases.Last())
                {
                    await Task.Delay(_retryDelayMs);
                }
            }
        }

        _logger.LogError(lastException, "All backup databases failed");
        throw new InvalidOperationException("All backup databases are unavailable", lastException);
    }

    /// <summary>
    /// Execute query on a specific database with retry logic and proper timeout handling
    /// </summary>
    private async Task<T[]> ExecuteQueryAsync<T>(
        string connectionString,
        string query,
        Func<NpgsqlDataReader, T> mapper,
        Dictionary<string, object>? parameters = null)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = _connectionTimeoutSeconds,
            CommandTimeout = _commandTimeoutSeconds,
            // Add connection resilience settings
            KeepAlive = 30,
            TcpKeepAliveTime = 30,
            TcpKeepAliveInterval = 5,
            // Retry settings for connection
            MaxAutoPrepare = 0, // Disable auto-prepare to reduce connection overhead
            ApplicationName = "PoolSyncService"
        };

        Exception? lastException = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            // Throttle global concurrent DB operations
            await _globalDbSemaphore!.WaitAsync();
            Interlocked.Increment(ref _activeDbOps);
            try
            {
                using var connection = new NpgsqlConnection(connectionStringBuilder.ToString());

                // Set connection timeout via cancellation token
                using var connectionCts = new CancellationTokenSource(TimeSpan.FromSeconds(_connectionTimeoutSeconds));
                await connection.OpenAsync(connectionCts.Token);

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

                // Set read timeout via cancellation token
                using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(_commandTimeoutSeconds));
                using var reader = await command.ExecuteReaderAsync(readCts.Token);

                while (await reader.ReadAsync(readCts.Token))
                {
                    results.Add(mapper(reader));
                }

                _logger.LogDebug("Successfully executed query on attempt {Attempt}/{MaxRetries}", attempt, _maxRetries);
                return results.ToArray();
            }
            catch (Exception ex) when (IsRetriableException(ex))
            {
                lastException = ex;
                _logger.LogWarning("Query attempt {Attempt}/{MaxRetries} failed with retriable error: {Message}",
                    attempt, _maxRetries, ex.Message);

                if (attempt < _maxRetries)
                {
                    // Exponential backoff to reduce pressure on PgBouncer/DB
                    var delay = _retryDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogDebug("Retrying in {Delay}ms...", delay);
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query attempt {Attempt}/{MaxRetries} failed with non-retriable error: {Message}",
                    attempt, _maxRetries, ex.Message);
                throw;
            }
            finally
            {
                if (_activeDbOps > 0) Interlocked.Decrement(ref _activeDbOps);
                _globalDbSemaphore!.Release();
            }
        }

        throw lastException ?? new Exception("All retry attempts failed");
    }

    /// <summary>
    /// Determines if an exception is retriable (timeout, connection issues)
    /// </summary>
    private static bool IsRetriableException(Exception ex)
    {
        if (ex is TimeoutException || ex is SocketException)
            return true;

        if (ex is NpgsqlException npgsqlEx)
        {
            // Treat too-many-clients (often 53300 or cached server_login retry) and protocol 08P01 as retriable
            var msg = npgsqlEx.Message?.ToLowerInvariant() ?? string.Empty;
            if (npgsqlEx.IsTransient ||
                msg.Contains("too many clients") ||
                msg.Contains("server_login_retry") ||
                msg.Contains("timeout") ||
                msg.Contains("connection") ||
                msg.Contains("network") ||
                msg.Contains("08p01") ||
                msg.Contains("canceling statement due to user request") ||
                msg.Contains("query was cancelled") ||
                msg.Contains("57014"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if circuit breaker is open for a database
    /// </summary>
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

        // Check if enough time has passed to try again
        if (DateTime.UtcNow - lastFailure > _circuitBreakerTimeout)
        {
            _logger.LogInformation("Circuit breaker timeout expired for {ConnectionKey}, allowing retry", connectionKey);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Record a database failure for circuit breaker logic
    /// </summary>
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

    /// <summary>
    /// Reset database failure state on successful connection
    /// </summary>
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
    private PoolListApiResponse MapPoolList(NpgsqlDataReader reader)
    {
        return new PoolListApiResponse
        {
            pool_id_bech32 = reader["pool_id_bech32"]?.ToString(),
            pool_id_hex = reader["pool_id_hex"]?.ToString(),
            active_epoch_no = int.TryParse(ParseActiveEpochNo(reader["active_epoch_no"]), out int activeEpochNo) ? activeEpochNo : (int?)null,
            margin = reader["margin"] as double?,
            fixed_cost = reader["fixed_cost"]?.ToString(),
            pledge = reader["pledge"]?.ToString(),
            deposit = reader["deposit"]?.ToString(),
            reward_addr = reader["reward_addr"]?.ToString(),
            owners = ParseStringList(reader["owners"]),
            relays = ParsePoolRelays(reader["relays"]),
            ticker = reader["ticker"]?.ToString(),
            pool_group = reader["pool_group"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            pool_status = reader["pool_status"]?.ToString(),
            active_stake = reader["active_stake"]?.ToString(),
            retiring_epoch = reader["retiring_epoch"] as int?
        };
    }

    private PoolVotingPowerHistoryApiResponse MapPoolVotingPowerHistory(NpgsqlDataReader reader)
    {
        return new PoolVotingPowerHistoryApiResponse
        {
            pool_id_bech32 = reader["pool_id_bech32"]?.ToString(),
            epoch_no = reader["epoch_no"] as int?,
            amount = reader["amount"]?.ToString()
        };
    }

    private PoolMetadataApiResponse MapPoolMetadata(NpgsqlDataReader reader)
    {
        return new PoolMetadataApiResponse
        {
            pool_id_bech32 = reader["pool_id_bech32"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = reader["meta_json"]?.ToString()
        };
    }

    private PoolStakeSnapshotApiResponse MapPoolStakeSnapshot(NpgsqlDataReader reader)
    {
        return new PoolStakeSnapshotApiResponse
        {
            snapshot = reader["snapshot"]?.ToString(),
            epoch_no = reader["epoch_no"] as long?,
            nonce = reader["nonce"]?.ToString(),
            pool_stake = reader["pool_stake"]?.ToString(),
            active_stake = reader["active_stake"]?.ToString()
        };
    }

    private PoolDelegatorsApiResponse MapPoolDelegators(NpgsqlDataReader reader)
    {
        return new PoolDelegatorsApiResponse
        {
            stake_address = reader["stake_address"]?.ToString(),
            amount = reader["amount"]?.ToString(),
            active_epoch_no = reader["active_epoch_no"] as long?,
            latest_delegation_tx_hash = reader["latest_delegation_tx_hash"]?.ToString(),
        };
    }

    private UtxoInfoApiResponse MapUtxoInfo(NpgsqlDataReader reader)
    {
        return new UtxoInfoApiResponse
        {
            tx_hash = reader["tx_hash"]?.ToString(),
            tx_index = reader["tx_index"] as short?,
            stake_address = reader["stake_address"]?.ToString(),
            epoch_no = reader["epoch_no"] as int?,
            block_time = reader["block_time"] as int?
        };
    }

    private PoolUpdatesApiResponse MapPoolUpdates(NpgsqlDataReader reader)
    {
        return new PoolUpdatesApiResponse
        {
            tx_hash = reader["tx_hash"]?.ToString(),
            block_time = reader["block_time"] as int?,
            pool_id_bech32 = reader["pool_id_bech32"]?.ToString(),
            pool_id_hex = reader["pool_id_hex"]?.ToString(),
            active_epoch_no = ParseActiveEpochNo(reader["active_epoch_no"]),
            vrf_key_hash = reader["vrf_key_hash"]?.ToString(),
            margin = reader["margin"] as double?,
            fixed_cost = reader["fixed_cost"]?.ToString(),
            pledge = reader["pledge"]?.ToString(),
            reward_addr = reader["reward_addr"]?.ToString(),
            owners = ParseStringList(reader["owners"]),
            relays = ParsePoolRelays(reader["relays"]),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = ParsePoolMetaJson(reader["meta_json"]),
            update_type = reader["update_type"]?.ToString(),
            retiring_epoch = reader["retiring_epoch"] as int?
        };
    }

    private PoolInformationApiResponse MapPoolInformation(NpgsqlDataReader reader)
    {
        return new PoolInformationApiResponse
        {
            pool_id_bech32 = reader["pool_id_bech32"]?.ToString(),
            pool_id_hex = reader["pool_id_hex"]?.ToString(),
            active_epoch_no = reader["active_epoch_no"] as long?,
            vrf_key_hash = reader["vrf_key_hash"]?.ToString(),
            margin = reader["margin"] as double?,
            fixed_cost = reader["fixed_cost"]?.ToString(),
            pledge = reader["pledge"]?.ToString(),
            deposit = reader["deposit"]?.ToString(),
            reward_addr = reader["reward_addr"]?.ToString(),
            reward_addr_delegated_drep = reader["reward_addr_delegated_drep"]?.ToString(),
            owners = ParseStringList(reader["owners"]),
            relays = ParsePoolRelays(reader["relays"]),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = ParsePoolMetaJson(reader["meta_json"]),
            pool_status = reader["pool_status"]?.ToString(),
            retiring_epoch = reader["retiring_epoch"] as int?,
            op_cert = reader["op_cert"]?.ToString(),
            op_cert_counter = reader["op_cert_counter"] as long?,
            active_stake = reader["active_stake"]?.ToString(),
            //sigma = reader["sigma"] as double?,
            block_count = reader["block_count"] as int?,
            live_pledge = reader["live_pledge"]?.ToString(),
            live_stake = reader["live_stake"]?.ToString(),
            live_delegators = reader["live_delegators"] as long?,
            live_saturation = reader["live_saturation"] as double?,
            voting_power = reader["voting_power"]?.ToString()
        };
    }

    /// <summary>
    /// Parse string list from database value
    /// </summary>
    private List<string?>? ParseStringList(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var owners = value as string[];
            if (owners == null) return null;
            return new List<string?>(owners);
        }
        catch { return null; }
    }

    /// <summary>
    /// Parse pool relays from database value
    /// </summary>
    private List<PoolRelayResponse?>? ParsePoolRelays(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var relays = value as string[];
            var result = relays?.Select(r => JsonSerializer.Deserialize<PoolRelayResponse>(r)).ToList();
            return result;
        }
        catch { return null; }
    }

    private string? ParseActiveEpochNo(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var result = Convert.ToInt32(value);
            return result.ToString();
        }
        catch
        {
            _logger.LogWarning("⚠️ Failed to parse active epoch no JSON: {Value}", value);
            return null;
        }
    }


    /// <summary>
    /// Parse pool metadata JSON from database value
    /// </summary>
    private PoolMetaJsonResponse? ParsePoolMetaJson(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (value is string str)
        {
            try
            {
                return JsonSerializer.Deserialize<PoolMetaJsonResponse>(str);
            }
            catch
            {
                _logger.LogWarning("⚠️ Failed to parse pool metadata JSON: {Value}", str);
                return null;
            }
        }

        return null;
    }
}
