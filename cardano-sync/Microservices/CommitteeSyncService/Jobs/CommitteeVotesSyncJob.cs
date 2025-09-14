using CommitteeSyncService.ApiResponses;
using CommitteeSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;
using System.Text.Json;

namespace CommitteeSyncService.Jobs;


[DisallowConcurrentExecution]
public class CommitteeVotesSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<CommitteeVotesSyncJob> _logger;

    public CommitteeVotesSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<CommitteeVotesSyncJob> logger
    )
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting CommitteeVotesSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Get all cc_hot_ids from committee members
            var members = await _context.MDCommitteeInformations
                .AsNoTracking()
                .Select(m => m.members)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(members))
            {
                _logger.LogInformation("No committee members data found in database");
                return;
            }

            var listOfMembers = JsonSerializer.Deserialize<List<CommitteeMember>>(members);
            var ccHotIds = listOfMembers?.Where(m => !string.IsNullOrEmpty(m.cc_hot_id))
                                        .Select(m => m.cc_hot_id!)
                                        .ToList();

            if (ccHotIds?.Any() != true)
            {
                _logger.LogInformation("No valid cc_hot_ids found in committee members");
                return;
            }

            _logger.LogInformation("Found {Count} cc_hot_ids to process", ccHotIds.Count);

            // Test connections to backup databases first
            var connectionStatus = await _databaseSyncService.TestConnectionsAsync();
            _logger.LogInformation("🔍 Database connection status: {@ConnectionStatus}", connectionStatus);

            // 🚀 NEW APPROACH: Submit ALL requests at once to backup databases with batch processing
            var allCommitteeVotes = await ProcessAllCcHotIdsWithBackupDatabases(ccHotIds);

            var totalVotes = allCommitteeVotes.Values.SelectMany(x => x).Count();
            _logger.LogInformation("Collected {Count} total vote records from {MemberCount} committee members",
                totalVotes, allCommitteeVotes.Count);

            if (allCommitteeVotes.Any())
            {
                await BulkRefreshCommitteeVotes(allCommitteeVotes);
                _logger.LogInformation("CommitteeVotesSyncJob completed successfully. Processed {Count} vote records",
                    totalVotes);
            }
            else
            {
                _logger.LogWarning("No committee votes data retrieved from backup databases");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CommitteeVotesSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 🚀 BATCH PROCESSING: Process cc_hot_ids in controlled batches from backup databases
    /// Uses concurrency control and rate limiting to prevent timeouts
    /// </summary>
    private async Task<Dictionary<string, List<CommitteeVotesApiResponse>>> ProcessAllCcHotIdsWithBackupDatabases(List<string> ccHotIds)
    {
        _logger.LogInformation("🚀 Starting batch processing: {Count} cc_hot_ids with concurrency control from backup databases", ccHotIds.Count);

        var startTime = DateTime.UtcNow;
        var allCommitteeVotes = new Dictionary<string, List<CommitteeVotesApiResponse>>();

        // Process in batches with controlled concurrency
        const int batchSize = 20; // Reduced batch size to avoid overloading backup databases
        const int maxConcurrency = 5; // Maximum concurrent requests per batch
        var totalBatches = (int)Math.Ceiling((double)ccHotIds.Count / batchSize);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = ccHotIds.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} cc_hot_ids",
                batchIndex + 1, totalBatches, batch.Count);

            var batchStartTime = DateTime.UtcNow;

            // Process batch with concurrency control using SemaphoreSlim
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var batchTasks = batch.Select(async ccHotId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug("📡 Processing cc_hot_id: {CcHotId}", ccHotId);

                    var votes = await _databaseSyncService.GetCommitteeVotesAsync(ccHotId);

                    if (votes?.Any() == true)
                    {
                        _logger.LogDebug("✅ Committee member {CcHotId} has {Count} votes", ccHotId, votes.Length);
                        return new KeyValuePair<string, List<CommitteeVotesApiResponse>>(ccHotId, votes.ToList());
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Committee member {CcHotId} has no votes", ccHotId);
                        return new KeyValuePair<string, List<CommitteeVotesApiResponse>>(ccHotId, new List<CommitteeVotesApiResponse>());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing cc_hot_id {CcHotId}: {Message}", ccHotId, ex.Message);
                    return new KeyValuePair<string, List<CommitteeVotesApiResponse>>(ccHotId, new List<CommitteeVotesApiResponse>());
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            // Wait for batch completion
            var batchResults = await Task.WhenAll(batchTasks);

            // Collect batch results
            var successCount = 0;
            foreach (var result in batchResults)
            {
                if (result.Value.Any())
                {
                    allCommitteeVotes[result.Key] = result.Value;
                    successCount++;
                }
            }

            var batchDuration = DateTime.UtcNow - batchStartTime;
            _logger.LogInformation("✅ Batch {BatchNumber} completed in {Duration}. Success: {SuccessCount}/{BatchSize}",
                batchIndex + 1, batchDuration, successCount, batch.Count);

            // Add delay between batches to respect rate limits
            if (batchIndex < totalBatches - 1)
            {
                _logger.LogDebug("⏱️ Waiting 1 seconds before next batch...");
                await Task.Delay(1000); // 1 second delay between batches
            }
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var totalVotes = allCommitteeVotes.Values.SelectMany(x => x).Count();

        _logger.LogInformation("🎯 Batch processing completed in {Duration}. Processed {ProcessedCount}/{TotalCount} committee members successfully. Total votes: {VoteCount}",
            totalDuration, allCommitteeVotes.Count, ccHotIds.Count, totalVotes);

        // Log service stats
        _logger.LogInformation("📊 Service Stats: {Stats}", _databaseSyncService.GetServiceStats());

        return allCommitteeVotes;
    }

    private async Task BulkRefreshCommitteeVotes(Dictionary<string, List<CommitteeVotesApiResponse>> committeeVotesDict)
    {
        var totalCount = committeeVotesDict.Values.SelectMany(x => x).Count();
        _logger.LogInformation("🔄 Starting full refresh for {Count} committee vote records", totalCount);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllCommitteeVotesRecords();

            // Step 2: Flatten all votes with their cc_hot_id for batch processing
            var allVotes = new List<(string ccHotId, CommitteeVotesApiResponse vote)>();
            foreach (var kvp in committeeVotesDict)
            {
                var ccHotId = kvp.Key;
                var votes = kvp.Value;

                if (string.IsNullOrWhiteSpace(ccHotId) || !votes.Any())
                    continue;

                foreach (var vote in votes)
                {
                    if (!string.IsNullOrWhiteSpace(vote.proposal_id))
                    {
                        allVotes.Add((ccHotId, vote));
                    }
                }
            }

            // Step 3: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)allVotes.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allVotes.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records across {MemberCount} committee members",
                totalCount, committeeVotesDict.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string ccHotId, CommitteeVotesApiResponse vote)> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var (ccHotId, vote) in batch)
        {
            // Create parameters for this record
            var ccHotIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", ccHotId);
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.proposal_id);
            var proposalTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.proposal_tx_hash);
            var proposalIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.proposal_index);
            var voteTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.vote_tx_hash);
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.block_time);
            var voteParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.vote);

            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", vote.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", vote.meta_hash);

            valueParts.Add(
                $"(@{ccHotIdParam.ParameterName}, @{proposalIdParam.ParameterName}, @{proposalTxHashParam.ParameterName}, @{proposalIndexParam.ParameterName}, @{voteTxHashParam.ParameterName}, @{blockTimeParam.ParameterName}, @{voteParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_committee_votes 
            (cc_hot_id, proposal_id, proposal_tx_hash, proposal_index, vote_tx_hash, block_time, vote, meta_url, meta_hash)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (cc_hot_id, proposal_id, vote_tx_hash) DO UPDATE SET
                proposal_tx_hash = EXCLUDED.proposal_tx_hash,
                proposal_index = EXCLUDED.proposal_index,
                block_time = EXCLUDED.block_time,
                vote = EXCLUDED.vote,
                meta_url = EXCLUDED.meta_url,
                meta_hash = EXCLUDED.meta_hash";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllCommitteeVotesRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing committee votes records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_committee_votes");
            _logger.LogInformation("✅ Successfully deleted all existing committee votes records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}