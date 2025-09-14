using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;
using VotingSyncService.ApiResponses;
using VotingSyncService.Services;

namespace VotingSyncService.Jobs;


[DisallowConcurrentExecution]
public class VoteListSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly ILogger<VoteListSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;

    public VoteListSyncJob(
        CardanoDbContext context,
        ILogger<VoteListSyncJob> logger,
        DatabaseSyncService databaseSyncService
    )
    {
        _context = context;
        _logger = logger;
        _databaseSyncService = databaseSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting VoteListSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get vote list data from backup database
            _logger.LogInformation(
                "🔄 Fetching vote list data from backup database..."
            );
            var voteData = await _databaseSyncService.GetVoteListAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "✅ Retrieved {Count} vote records from backup database in {Duration}",
                voteData.Length,
                duration
            );

            if (voteData.Any())
            {
                await BulkRefreshVoteList(voteData);

                _logger.LogInformation(
                    "🎯 VoteListSyncJob completed successfully. Processed {TotalCount} vote records in {ElapsedTime}",
                    voteData.Length,
                    DateTime.UtcNow - startTime
                );
            }
            else
            {
                _logger.LogWarning("⚠️ No vote list data retrieved from backup database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in VoteListSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshVoteList(VoteListApiResponse[] voteData)
    {
        _logger.LogInformation(
            "🔄 Starting full refresh for {Count} vote list records using raw SQL",
            voteData.Length
        );

        try
        {
            // // Step 1: Delete all existing records
            // await DeleteAllVoteListRecords();

            // Step 2: Insert all new records in batches
            var processedCount = 0;
            const int batchSize = 500;

            for (int i = 0; i < voteData.Length; i += batchSize)
            {
                var batch = voteData.Skip(i).Take(batchSize).ToArray();

                if (batch.Length > 0)
                {
                    await InsertBatchWithRawSql(batch);
                    processedCount += batch.Length;

                    _logger.LogDebug(
                        "💾 Inserted batch {BatchStart}-{BatchEnd} ({Processed}/{Total})",
                        i + 1,
                        Math.Min(i + batchSize, voteData.Length),
                        processedCount,
                        voteData.Length
                    );
                }
            }

            _logger.LogInformation(
                "✅ Full refresh completed successfully. Processed {ProcessedCount} vote list records using raw SQL",
                processedCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllVoteListRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing vote list records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_vote_list");
            _logger.LogInformation("✅ Successfully deleted all existing vote list records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(VoteListApiResponse[] batch)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            if (string.IsNullOrEmpty(record.vote_tx_hash) || string.IsNullOrEmpty(record.proposal_id))
                continue;

            // Create parameters for this record
            var voteTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.vote_tx_hash);
            var voterRoleParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.voter_role);
            var voterIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.voter_id);
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_id);
            var proposalTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_tx_hash);
            var proposalIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_index);
            var proposalTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_type);
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no);
            var blockHeightParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_height);
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_time);
            var voteParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.vote);
            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
            var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);

            valueParts.Add(
                $"(@{voteTxHashParam.ParameterName}, @{voterRoleParam.ParameterName}, @{voterIdParam.ParameterName}, @{proposalIdParam.ParameterName}, @{proposalTxHashParam.ParameterName}, @{proposalIndexParam.ParameterName}, @{proposalTypeParam.ParameterName}, @{epochNoParam.ParameterName}, @{blockHeightParam.ParameterName}, @{blockTimeParam.ParameterName}, @{voteParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"
                INSERT INTO md_vote_list 
                    (vote_tx_hash, voter_role, voter_id, proposal_id, proposal_tx_hash, proposal_index, proposal_type, epoch_no, block_height, block_time, vote, meta_url, meta_hash, meta_json)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (voter_role, voter_id, proposal_id, block_time, vote_tx_hash) DO UPDATE SET 
                    voter_role = EXCLUDED.voter_role,
                    voter_id = EXCLUDED.voter_id,
                    proposal_id = EXCLUDED.proposal_id,
                    proposal_tx_hash = EXCLUDED.proposal_tx_hash,
                    proposal_index = EXCLUDED.proposal_index,
                    proposal_type = EXCLUDED.proposal_type,
                    epoch_no = EXCLUDED.epoch_no,
                    block_height = EXCLUDED.block_height,
                    block_time = EXCLUDED.block_time,
                    vote = EXCLUDED.vote,
                    meta_url = EXCLUDED.meta_url,
                    meta_hash = EXCLUDED.meta_hash,
                    meta_json = EXCLUDED.meta_json";

        await command.ExecuteNonQueryAsync();
    }


}
