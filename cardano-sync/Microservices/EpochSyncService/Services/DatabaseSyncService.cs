using EpochSyncService.ApiResponses;
using Npgsql;
using System.Net.Sockets;

namespace EpochSyncService.Services;

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

    // Circuit breaker state
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
    /// Get epoch info from backup database
    /// </summary>
    public async Task<EpochApiResponse[]> GetEpochInfoAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.epoch_info()
            ORDER BY epoch_no DESC";

        return await ExecuteQueryWithFailoverAsync<EpochApiResponse>(query, MapEpochInfo);
    }

    /// <summary>
    /// Get epoch protocol parameters from backup database
    /// </summary>
    public async Task<EpochProtocolParametersApiResponse[]> GetEpochProtocolParametersAsync()
    {
        var query = $@"
            SELECT 
                *
            FROM {_schema}.epoch_params()
            ORDER BY epoch_no DESC";

        return await ExecuteQueryWithFailoverAsync<EpochProtocolParametersApiResponse>(query, MapEpochProtocolParameters);
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
    /// Execute query on a specific database with retry, tokens and timeouts
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
            ApplicationName = "EpochSyncService"
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

    private EpochApiResponse MapEpochInfo(NpgsqlDataReader reader)
    {
        return new EpochApiResponse
        {
            epoch_no = reader["epoch_no"] as int?,
            out_sum = reader["out_sum"]?.ToString(),
            fees = reader["fees"]?.ToString(),
            tx_count = reader["tx_count"] as int?,
            blk_count = reader["blk_count"] as int?,
            start_time = reader["start_time"] as int?,
            end_time = reader["end_time"] as int?,
            first_block_time = reader["first_block_time"] as int?,
            last_block_time = reader["last_block_time"] as int?,
            active_stake = reader["active_stake"]?.ToString(),
            total_rewards = reader["total_rewards"]?.ToString(),
            avg_blk_reward = reader["avg_blk_reward"]?.ToString()
        };
    }

    private EpochProtocolParametersApiResponse MapEpochProtocolParameters(NpgsqlDataReader reader)
    {
        return new EpochProtocolParametersApiResponse
        {
            epoch_no = reader["epoch_no"] as int?,
            min_fee_a = reader["min_fee_a"] as int?,
            min_fee_b = reader["min_fee_b"] as int?,
            max_block_size = reader["max_block_size"] as int?,
            max_tx_size = reader["max_tx_size"] as int?,
            max_bh_size = reader["max_bh_size"] as int?,
            key_deposit = reader["key_deposit"]?.ToString(),
            pool_deposit = reader["pool_deposit"]?.ToString(),
            max_epoch = reader["max_epoch"] as int?,
            optimal_pool_count = reader["optimal_pool_count"] as int?,
            influence = reader["influence"] as double?,
            monetary_expand_rate = reader["monetary_expand_rate"] as double?,
            treasury_growth_rate = reader["treasury_growth_rate"] as double?,
            decentralisation = reader["decentralisation"] as double?,
            extra_entropy = reader["extra_entropy"]?.ToString(),
            protocol_major = reader["protocol_major"] as int?,
            protocol_minor = reader["protocol_minor"] as int?,
            min_utxo_value = reader["min_utxo_value"]?.ToString(),
            min_pool_cost = reader["min_pool_cost"]?.ToString(),
            nonce = reader["nonce"]?.ToString(),
            block_hash = reader["block_hash"]?.ToString(),
            cost_models = reader["cost_models"]?.ToString(),
            price_mem = reader["price_mem"] as double?,
            price_step = reader["price_step"] as double?,
            max_tx_ex_mem = ParseDecimal(reader["max_tx_ex_mem"]),
            max_tx_ex_steps = ParseDecimal(reader["max_tx_ex_steps"]),
            max_block_ex_mem = ParseDecimal(reader["max_block_ex_mem"]),
            max_block_ex_steps = ParseDecimal(reader["max_block_ex_steps"]),
            max_val_size = ParseDecimal(reader["max_val_size"]),
            collateral_percent = reader["collateral_percent"] as int?,
            max_collateral_inputs = reader["max_collateral_inputs"] as int?,
            coins_per_utxo_size = reader["coins_per_utxo_size"]?.ToString(),
            pvt_motion_no_confidence = reader["pvt_motion_no_confidence"] as double?,
            pvt_committee_normal = reader["pvt_committee_normal"] as double?,
            pvt_committee_no_confidence = reader["pvt_committee_no_confidence"] as double?,
            pvt_hard_fork_initiation = reader["pvt_hard_fork_initiation"] as double?,
            dvt_motion_no_confidence = reader["dvt_motion_no_confidence"] as double?,
            dvt_committee_normal = reader["dvt_committee_normal"] as double?,
            dvt_committee_no_confidence = reader["dvt_committee_no_confidence"] as double?,
            dvt_update_to_constitution = reader["dvt_update_to_constitution"] as double?,
            dvt_hard_fork_initiation = reader["dvt_hard_fork_initiation"] as double?,
            dvt_p_p_network_group = reader["dvt_p_p_network_group"] as double?,
            dvt_p_p_economic_group = reader["dvt_p_p_economic_group"] as double?,
            dvt_p_p_technical_group = reader["dvt_p_p_technical_group"] as double?,
            dvt_p_p_gov_group = reader["dvt_p_p_gov_group"] as double?,
            dvt_treasury_withdrawal = reader["dvt_treasury_withdrawal"] as double?,
            committee_min_size = ParseDecimal(reader["committee_min_size"]),
            committee_max_term_length = ParseDecimal(reader["committee_max_term_length"]),
            gov_action_lifetime = ParseDecimal(reader["gov_action_lifetime"]),
            gov_action_deposit = reader["gov_action_deposit"]?.ToString(),
            drep_deposit = reader["drep_deposit"]?.ToString(),
            drep_activity = ParseDecimal(reader["drep_activity"]),
            pvtpp_security_group = reader["pvtpp_security_group"] as double?,
            min_fee_ref_script_cost_per_byte = reader["min_fee_ref_script_cost_per_byte"] as double?
        };
    }

    private decimal? ParseDecimal(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            var result = Convert.ToDecimal(value);
            return result;
        }
        catch
        {
            _logger.LogWarning("⚠️ Failed to parse active epoch no JSON: {Value}", value);
            return null;
        }
    }
}
