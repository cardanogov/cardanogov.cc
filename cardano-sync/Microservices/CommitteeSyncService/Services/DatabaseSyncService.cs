using CommitteeSyncService.ApiResponses;
using Npgsql;
using System.Net.Sockets;
using System.Text.Json;

namespace CommitteeSyncService.Services;

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

    // Global DB operation throttling to work with PgBouncer/Postgres limits
    private static SemaphoreSlim? _globalDbSemaphore;
    private static int _activeDbOps;
    private readonly int _maxConcurrentDbOps;

    // Circuit breaker state for each database
    private readonly Dictionary<string, DateTime> _databaseFailureTimestamps = new();
    private readonly Dictionary<string, int> _databaseFailureCounts = new();
    private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(5);
    private readonly int _circuitBreakerThreshold = 3;

    public DatabaseSyncService(
        IConfiguration configuration,
        ILogger<DatabaseSyncService> logger)
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

        // Initialize global DB semaphore (default 8) with config override
        _maxConcurrentDbOps = int.TryParse(configuration["DatabaseSync:MaxConcurrentDbOperations"], out var m) ? Math.Max(1, m) : 8;
        _globalDbSemaphore ??= new SemaphoreSlim(_maxConcurrentDbOps, _maxConcurrentDbOps);
    }

    /// <summary>
    /// Get committee info from backup databases
    /// </summary>
    public async Task<CommitteeInfoApiResponse[]?> GetCommitteeInfoAsync()
    {
        var query = $@"
            SELECT 
                *
            FROM {_schema}.committee_info()
            ORDER BY proposal_id";

        return await ExecuteQueryWithFailoverAsync<CommitteeInfoApiResponse>(query, MapCommitteeInfo);
    }

    /// <summary>
    /// Get committee votes from backup databases
    /// </summary>
    public async Task<CommitteeVotesApiResponse[]?> GetCommitteeVotesAsync(string ccHotId)
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.committee_votes(@ccHotId)
            ORDER BY block_time DESC";

        var parameters = new Dictionary<string, object>
        {
            { "ccHotId", ccHotId }
        };

        return await ExecuteQueryWithFailoverAsync<CommitteeVotesApiResponse>(query, MapCommitteeVotes, parameters);
    }

    /// <summary>
    /// Get treasury withdrawals from backup databases
    /// </summary>
    public async Task<TreasuryWithdrawalsApiResponse[]?> GetTreasuryWithdrawalsAsync()
    {
        var query = $@"
            SELECT 
                epoch_no,
                SUM(amount::bigint) AS sum
            FROM {_schema}.treasury_withdrawals()
            GROUP BY epoch_no
            ORDER BY epoch_no DESC";

        return await ExecuteQueryWithFailoverAsync<TreasuryWithdrawalsApiResponse>(query, MapTreasuryWithdrawals);
    }

    /// <summary>
    /// Get totals from backup databases
    /// </summary>
    public async Task<TotalsApiResponse[]?> GetTotalsAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.totals()
            ORDER BY epoch_no DESC";

        return await ExecuteQueryWithFailoverAsync<TotalsApiResponse>(query, MapTotals);
    }

    /// <summary>
    /// Execute query with failover mechanism
    /// </summary>
    private async Task<T[]?> ExecuteQueryWithFailoverAsync<T>(string query, Func<NpgsqlDataReader, T> mapper, Dictionary<string, object>? parameters = null)
    {
        var lastException = new Exception("No databases available");

        var availableDatabases = _failoverOrder.Where(db => !IsCircuitBreakerOpen(db)).ToArray();
        if (!availableDatabases.Any())
        {
            _logger.LogWarning("All databases are in circuit breaker open state. Attempting with primary database anyway.");
            availableDatabases = new[] { _failoverOrder.First() };
        }

        foreach (var connectionName in availableDatabases)
        {
            try
            {
                _logger.LogInformation("🔄 Attempting to execute query on {ConnectionName} (Failures: {Count})", connectionName, _databaseFailureCounts.GetValueOrDefault(connectionName, 0));

                var result = await ExecuteQueryAsync<T>(connectionName, query, mapper, parameters);

                if (result != null)
                {
                    ResetDatabaseFailureState(connectionName);
                    _logger.LogInformation("✅ Successfully executed query on {ConnectionName}", connectionName);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to execute query on {ConnectionName}: {Message}", connectionName, ex.Message);
                lastException = ex;
                RecordDatabaseFailure(connectionName, ex);

                if (!_enableFailover)
                {
                    throw;
                }
            }

            // Wait before trying next database
            if (_enableFailover && connectionName != availableDatabases.Last())
            {
                await Task.Delay(_retryDelayMs);
            }
        }

        _logger.LogError(lastException, "❌ All backup databases failed. Last error: {Message}", lastException.Message);
        throw new Exception("All backup databases are unavailable", lastException);
    }

    /// <summary>
    /// Execute query on specific database connection with retry and timeout handling
    /// </summary>
    private async Task<T[]?> ExecuteQueryAsync<T>(string connectionName, string query, Func<NpgsqlDataReader, T> mapper, Dictionary<string, object>? parameters = null)
    {
        var rawConnectionString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrEmpty(rawConnectionString))
        {
            throw new ArgumentException($"Connection string '{connectionName}' not found in configuration");
        }

        var csb = new NpgsqlConnectionStringBuilder(rawConnectionString)
        {
            Timeout = _connectionTimeoutSeconds,
            CommandTimeout = _commandTimeoutSeconds,
            KeepAlive = 30,
            TcpKeepAliveTime = 30,
            TcpKeepAliveInterval = 5,
            MaxAutoPrepare = 0,
            ApplicationName = "CommitteeSyncService"
        };

        Exception? lastException = null;
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
                        try
                        {
                            var item = mapper(reader);
                            results.Add(item);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ Failed to map row data: {Message}", ex.Message);
                        }
                    }

                    _logger.LogDebug("Query succeeded on attempt {Attempt}/{Max}", attempt, _maxRetries);
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
                lastException = ex;
                _logger.LogWarning(ex, "Attempt {Attempt}/{Max} failed with retriable error: {Message}", attempt, _maxRetries, ex.Message);
                if (attempt < _maxRetries)
                {
                    var delay = _retryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-retriable error on attempt {Attempt}/{Max}: {Message}", attempt, _maxRetries, ex.Message);
                throw;
            }
        }

        throw lastException ?? new Exception("All attempts failed");
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

    /// <summary>
    /// Mapper functions for different response types
    /// </summary>
    private CommitteeInfoApiResponse MapCommitteeInfo(NpgsqlDataReader reader)
    {
        return new CommitteeInfoApiResponse
        {
            proposal_id = reader["proposal_id"]?.ToString(),
            proposal_tx_hash = reader["proposal_tx_hash"]?.ToString(),
            proposal_index = int.TryParse(reader["proposal_index"]?.ToString(), out int proposal_index) ? proposal_index : 0,
            quorum_numerator = int.TryParse(reader["quorum_numerator"]?.ToString(), out int quorum_numerator) ? quorum_numerator : 0,
            quorum_denominator = int.TryParse(reader["quorum_denominator"]?.ToString(), out int quorum_denominator) ? quorum_denominator : 0,
            members = JsonSerializer.Deserialize<CommitteeMember[]>(reader["members"]?.ToString())
        };
    }

    private CommitteeVotesApiResponse MapCommitteeVotes(NpgsqlDataReader reader)
    {
        return new CommitteeVotesApiResponse
        {
            proposal_id = reader["proposal_id"]?.ToString(),
            proposal_tx_hash = reader["proposal_tx_hash"]?.ToString(),
            proposal_index = int.TryParse(reader["proposal_index"]?.ToString(), out int proposal_index) ? proposal_index : 0,
            vote_tx_hash = reader["vote_tx_hash"]?.ToString(),
            block_time = int.TryParse(reader["block_time"]?.ToString(), out int block_time) ? block_time : 0,
            vote = reader["vote"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString()
        };
    }

    private TreasuryWithdrawalsApiResponse MapTreasuryWithdrawals(NpgsqlDataReader reader)
    {
        return new TreasuryWithdrawalsApiResponse
        {
            epoch_no = int.TryParse(reader["epoch_no"]?.ToString(), out int epoch_no) ? epoch_no : 0,
            sum = long.TryParse(reader["sum"]?.ToString(), out long sum) ? sum : 0,
        };
    }

    private TotalsApiResponse MapTotals(NpgsqlDataReader reader)
    {
        return new TotalsApiResponse
        {
            epoch_no = int.TryParse(reader["epoch_no"]?.ToString(), out int epoch_no) ? epoch_no : 0,
            circulation = reader["circulation"]?.ToString(),
            treasury = reader["treasury"]?.ToString(),
            reserves = reader["reserves"]?.ToString(),
            reward = reader["reward"]?.ToString(),
            supply = reader["supply"]?.ToString(),
            deposits_drep = reader["deposits_drep"]?.ToString(),
            deposits_proposal = reader["deposits_proposal"]?.ToString(),
            deposits_stake = reader["deposits_stake"]?.ToString(),
            fees = reader["fees"]?.ToString(),
        };
    }

    /// <summary>
    /// Test connection to all backup databases
    /// </summary>
    public async Task<Dictionary<string, bool>> TestConnectionsAsync()
    {
        var results = new Dictionary<string, bool>();

        foreach (var connectionName in _failoverOrder)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString(connectionName);
                if (string.IsNullOrEmpty(connectionString))
                {
                    results[connectionName] = false;
                    continue;
                }

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Test with a simple query
                using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();

                results[connectionName] = true;
                _logger.LogInformation("✅ Connection test successful for {ConnectionName}", connectionName);
            }
            catch (Exception ex)
            {
                results[connectionName] = false;
                _logger.LogWarning(ex, "❌ Connection test failed for {ConnectionName}: {Message}", connectionName, ex.Message);
            }
        }

        return results;
    }

    /// <summary>
    /// Get service statistics
    /// </summary>
    public object GetServiceStats()
    {
        return new
        {
            Schema = _schema,
            MaxRetries = _maxRetries,
            RetryDelayMs = _retryDelayMs,
            ConnectionTimeoutSeconds = _connectionTimeoutSeconds,
            CommandTimeoutSeconds = _commandTimeoutSeconds,
            EnableFailover = _enableFailover,
            FailoverOrder = _failoverOrder,
            AvailableConnections = _failoverOrder.Length
        };
    }
}
