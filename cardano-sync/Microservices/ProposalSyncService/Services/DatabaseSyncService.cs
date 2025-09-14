using Npgsql;
using ProposalSyncService.ApiResponses;

namespace ProposalSyncService.Services;

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
    }

    /// <summary>
    /// Get proposal list from backup database
    /// </summary>
    public async Task<ProposalListApiResponse[]> GetProposalListAsync()
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.proposal_list()
            ORDER BY block_time DESC";

        return await ExecuteQueryWithFailoverAsync<ProposalListApiResponse>(query, MapProposalList);
    }

    /// <summary>
    /// Get proposal voting summary from backup database
    /// </summary>
    public async Task<ProposalVotingSummaryApiResponse[]> GetProposalVotingSummaryAsync(string proposalId)
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.proposal_voting_summary(@proposalId)
            ORDER BY epoch_no DESC";

        var parameters = new Dictionary<string, object>
        {
            ["@proposalId"] = proposalId
        };

        return await ExecuteQueryWithFailoverAsync<ProposalVotingSummaryApiResponse>(query, MapProposalVotingSummary, parameters);
    }

    /// <summary>
    /// Get proposal votes from backup database
    /// </summary>
    public async Task<ProposalVotesApiResponse[]> GetProposalVotesAsync(string proposalId)
    {
        var query = $@"
            SELECT 
               *
            FROM {_schema}.proposal_votes(@proposalId)
            ORDER BY block_time DESC";

        var parameters = new Dictionary<string, object>
        {
            ["@proposalId"] = proposalId
        };

        return await ExecuteQueryWithFailoverAsync<ProposalVotesApiResponse>(query, MapProposalVotes, parameters);
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

        foreach (var connectionKey in _failoverOrder)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString(connectionKey);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Connection string not found for {ConnectionKey}", connectionKey);
                    continue;
                }

                _logger.LogInformation("Attempting to connect to {ConnectionKey}", connectionKey);
                var result = await ExecuteQueryAsync<T>(connectionString, query, mapper, parameters);
                _logger.LogInformation("Successfully retrieved data from {ConnectionKey}", connectionKey);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to connect to {ConnectionKey}: {Message}", connectionKey, ex.Message);

                if (!_enableFailover)
                    break;
            }
        }

        _logger.LogError(lastException, "All backup databases failed. Last error: {Message}", lastException.Message);
        throw lastException;
    }

    /// <summary>
    /// Execute query on specific database
    /// </summary>
    private async Task<T[]> ExecuteQueryAsync<T>(
        string connectionString,
        string query,
        Func<NpgsqlDataReader, T> mapper,
        Dictionary<string, object>? parameters = null)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

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
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            try
            {
                var result = mapper(reader);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to map row: {Message}", ex.Message);
            }
        }

        return results.ToArray();
    }

    /// <summary>
    /// Map proposal list from database reader
    /// </summary>
    private ProposalListApiResponse MapProposalList(NpgsqlDataReader reader)
    {
        var ratified_epoch = reader["ratified_epoch"];
        var enacted_epoch = reader["enacted_epoch"];
        var dropped_epoch = reader["dropped_epoch"];
        var expired_epoch = reader["expired_epoch"];
        var expiration = reader["expiration"];

        if (ratified_epoch == DBNull.Value) ratified_epoch = null;
        if (enacted_epoch == DBNull.Value) enacted_epoch = null;
        if (dropped_epoch == DBNull.Value) dropped_epoch = null;
        if (expired_epoch == DBNull.Value) expired_epoch = null;
        if (expiration == DBNull.Value) expiration = null;

        return new ProposalListApiResponse
        {
            block_time = reader["block_time"] as int?,
            proposal_id = reader["proposal_id"] as string,
            proposal_tx_hash = reader["proposal_tx_hash"] as string,
            proposal_index = reader["proposal_index"] as int?,
            proposal_type = reader["proposal_type"] as string,
            proposal_description = reader["proposal_description"] as string,
            deposit = reader["deposit"] as string,
            return_address = reader["return_address"] as string,
            proposed_epoch = reader["proposed_epoch"] as int?,
            ratified_epoch = ratified_epoch,
            enacted_epoch = enacted_epoch,
            dropped_epoch = dropped_epoch,
            expired_epoch = expired_epoch,
            expiration = expiration,
            meta_url = reader["meta_url"] as string,
            meta_hash = reader["meta_hash"] as string,
            meta_json = reader["meta_json"] as string,
            meta_comment = reader["meta_comment"] as string,
            meta_language = reader["meta_language"] as string,
            meta_is_valid = reader["meta_is_valid"] as string,
            withdrawal = reader["withdrawal"] as string,
            param_proposal = reader["param_proposal"] as string
        };
    }


    private double ToDoubleSafe(object value)
    {
        if (value == DBNull.Value) return 0.0d;
        return Convert.ToDouble(value);
    }


    /// <summary>
    /// Map proposal voting summary from database reader
    /// </summary>
    private ProposalVotingSummaryApiResponse MapProposalVotingSummary(NpgsqlDataReader reader)
    {
        double drep_yes_pct = ToDoubleSafe(reader["drep_yes_pct"]);
        double drep_no_pct = ToDoubleSafe(reader["drep_no_pct"]);
        double pool_yes_pct = ToDoubleSafe(reader["pool_yes_pct"]);
        double pool_no_pct = ToDoubleSafe(reader["pool_no_pct"]);
        double committee_yes_pct = ToDoubleSafe(reader["committee_yes_pct"]);
        double committee_no_pct = ToDoubleSafe(reader["committee_no_pct"]);

        return new ProposalVotingSummaryApiResponse
        {
            proposal_type = reader["proposal_type"] as string,
            epoch_no = reader["epoch_no"] as int?,
            drep_yes_votes_cast = reader["drep_yes_votes_cast"] as int?,
            drep_active_yes_vote_power = reader["drep_active_yes_vote_power"] as string,
            drep_yes_vote_power = reader["drep_yes_vote_power"] as string,
            drep_yes_pct = drep_yes_pct as double?,
            drep_no_votes_cast = reader["drep_no_votes_cast"] as int?,
            drep_active_no_vote_power = reader["drep_active_no_vote_power"] as string,
            drep_no_vote_power = reader["drep_no_vote_power"] as string,
            drep_no_pct = drep_no_pct as double?,
            drep_abstain_votes_cast = reader["drep_abstain_votes_cast"] as int?,
            drep_active_abstain_vote_power = reader["drep_active_abstain_vote_power"] as string,
            drep_always_no_confidence_vote_power = reader["drep_always_no_confidence_vote_power"] as string,
            drep_always_abstain_vote_power = reader["drep_always_abstain_vote_power"] as string,
            pool_yes_votes_cast = reader["pool_yes_votes_cast"] as int?,
            pool_active_yes_vote_power = reader["pool_active_yes_vote_power"] as string,
            pool_yes_vote_power = reader["pool_yes_vote_power"] as string,
            pool_yes_pct = pool_yes_pct as double?,
            pool_no_votes_cast = reader["pool_no_votes_cast"] as int?,
            pool_active_no_vote_power = reader["pool_active_no_vote_power"] as string,
            pool_no_vote_power = reader["pool_no_vote_power"] as string,
            pool_no_pct = pool_no_pct as double?,
            pool_abstain_votes_cast = reader["pool_abstain_votes_cast"] as int?,
            pool_active_abstain_vote_power = reader["pool_active_abstain_vote_power"] as string,
            pool_passive_always_abstain_votes_assigned = reader["pool_passive_always_abstain_votes_assigned"] as int?,
            pool_passive_always_abstain_vote_power = reader["pool_passive_always_abstain_vote_power"] as string,
            pool_passive_always_no_confidence_votes_assigned = reader["pool_passive_always_no_confidence_votes_assigned"] as int?,
            pool_passive_always_no_confidence_vote_power = reader["pool_passive_always_no_confidence_vote_power"] as string,
            committee_yes_votes_cast = reader["committee_yes_votes_cast"] as int?,
            committee_yes_pct = committee_yes_pct as double?,
            committee_no_votes_cast = reader["committee_no_votes_cast"] as int?,
            committee_no_pct = committee_no_pct as double?,
            committee_abstain_votes_cast = reader["committee_abstain_votes_cast"] as int?,
        };
    }

    /// <summary>
    /// Map proposal votes from database reader
    /// </summary>
    private ProposalVotesApiResponse MapProposalVotes(NpgsqlDataReader reader)
    {
        return new ProposalVotesApiResponse
        {
            block_time = reader["block_time"] as int?,
            voter_role = reader["voter_role"] as string,
            voter_id = reader["voter_id"] as string,
            voter_hex = reader["voter_hex"] as string,
            voter_has_script = reader["voter_has_script"] as bool?,
            vote = reader["vote"] as string,
            meta_url = reader["meta_url"] as string,
            meta_hash = reader["meta_hash"] as string
        };
    }
}
