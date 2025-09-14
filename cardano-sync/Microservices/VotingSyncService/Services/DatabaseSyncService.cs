using Npgsql;
using System.Net.Sockets;
using VotingSyncService.ApiResponses;

namespace VotingSyncService.Services;

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
    /// Get vote list from backup database
    /// </summary>
    public async Task<VoteListApiResponse[]> GetVoteListAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.vote_list()
            ORDER BY block_time DESC";

        return await ExecuteQueryWithFailoverAsync<VoteListApiResponse>(query, MapVoteList);
    }

    /// <summary>
    /// Get voter proposal list from backup database
    /// </summary>
    public async Task<VoterProposalListApiResponse[]> GetVoterProposalListAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.voter_proposal_list()
            ORDER BY block_time DESC";

        return await ExecuteQueryWithFailoverAsync<VoterProposalListApiResponse>(query, MapVoterProposalList);
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
                    _logger.LogWarning("Connection string for {ConnectionKey} is null or empty, skipping...", connectionKey);
                    continue;
                }

                _logger.LogInformation("Attempting to connect to {ConnectionKey} (Failures: {Count})...", connectionKey, _databaseFailureCounts.GetValueOrDefault(connectionKey, 0));
                var result = await ExecuteQueryAsync(connectionString, query, mapper, parameters);
                ResetDatabaseFailureState(connectionKey);
                _logger.LogInformation("Successfully retrieved data from {ConnectionKey}", connectionKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve data from {ConnectionKey}: {Message}", connectionKey, ex.Message);
                lastException = ex;
                RecordDatabaseFailure(connectionKey, ex);
            }
        }

        _logger.LogError(lastException, "All backup databases failed. Last error: {Message}", lastException.Message);
        throw lastException;
    }

    /// <summary>
    /// Execute query on a specific database connection
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
            ApplicationName = "VotingSyncService"
        };

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
                            _logger.LogWarning(ex, "Error mapping row {RowNumber}: {Message}", results.Count + 1, ex.Message);
                        }
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
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Final attempt {Attempt} failed: {Message}", attempt, ex.Message);
                    throw;
                }

                var delay = _retryDelayMs * (int)Math.Pow(2, attempt - 1);
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}ms: {Message}",
                    attempt, delay, ex.Message);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-retriable error on attempt {Attempt}: {Message}", attempt, ex.Message);
                throw;
            }
        }

        throw new Exception($"Failed after {_maxRetries} attempts");
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
    /// Map database row to VoteListApiResponse
    /// </summary>
    private VoteListApiResponse MapVoteList(NpgsqlDataReader reader)
    {
        return new VoteListApiResponse
        {
            vote_tx_hash = reader["vote_tx_hash"]?.ToString(),
            voter_role = reader["voter_role"]?.ToString(),
            voter_id = reader["voter_id"]?.ToString(),
            proposal_id = reader["proposal_id"]?.ToString(),
            proposal_tx_hash = reader["proposal_tx_hash"]?.ToString(),
            proposal_index = reader["proposal_index"] != DBNull.Value ? Convert.ToInt32(reader["proposal_index"]) : null,
            proposal_type = reader["proposal_type"]?.ToString(),
            epoch_no = reader["epoch_no"] != DBNull.Value ? Convert.ToInt32(reader["epoch_no"]) : null,
            block_height = reader["block_height"] != DBNull.Value ? Convert.ToInt32(reader["block_height"]) : null,
            block_time = reader["block_time"] != DBNull.Value ? Convert.ToInt64(reader["block_time"]) : null,
            vote = reader["vote"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = reader["meta_json"]?.ToString()
        };
    }

    /// <summary>
    /// Map database row to VoterProposalListApiResponse
    /// </summary>
    private VoterProposalListApiResponse MapVoterProposalList(NpgsqlDataReader reader)
    {
        return new VoterProposalListApiResponse
        {
            block_time = reader["block_time"] != DBNull.Value ? Convert.ToInt64(reader["block_time"]) : null,
            proposal_id = reader["proposal_id"]?.ToString(),
            proposal_tx_hash = reader["proposal_tx_hash"]?.ToString(),
            proposal_index = reader["proposal_index"] != DBNull.Value ? Convert.ToInt32(reader["proposal_index"]) : null,
            proposal_type = reader["proposal_type"]?.ToString(),
            proposal_description = reader["proposal_description"]?.ToString(),
            deposit = reader["deposit"]?.ToString(),
            return_address = reader["return_address"]?.ToString(),
            proposed_epoch = reader["proposed_epoch"] != DBNull.Value ? Convert.ToInt32(reader["proposed_epoch"]) : null,
            ratified_epoch = reader["ratified_epoch"]?.ToString(),
            enacted_epoch = reader["enacted_epoch"]?.ToString(),
            dropped_epoch = reader["dropped_epoch"]?.ToString(),
            expired_epoch = reader["expired_epoch"]?.ToString(),
            expiration = reader["expiration"]?.ToString(),
            meta_url = reader["meta_url"]?.ToString(),
            meta_hash = reader["meta_hash"]?.ToString(),
            meta_json = reader["meta_json"]?.ToString(),
            meta_comment = reader["meta_comment"]?.ToString(),
            meta_language = reader["meta_language"]?.ToString(),
            meta_is_valid = reader["meta_is_valid"]?.ToString(),
            withdrawal = reader["withdrawal"]?.ToString(),
            param_proposal = reader["param_proposal"]?.ToString()
        };
    }
}
