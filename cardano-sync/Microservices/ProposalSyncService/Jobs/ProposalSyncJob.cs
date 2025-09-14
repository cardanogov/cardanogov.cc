using Microsoft.EntityFrameworkCore;
using ProposalSyncService.ApiResponses;
using ProposalSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ProposalSyncService.Jobs;


[DisallowConcurrentExecution]
public class ProposalSyncJob : IJob
{
    private readonly ILogger<ProposalSyncJob> _logger;
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;

    public ProposalSyncJob(
        ILogger<ProposalSyncJob> logger,
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService
    )
    {
        _logger = logger;
        _context = context;
        _databaseSyncService = databaseSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting ProposalSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Get proposal list data from backup database
            var proposalData = await _databaseSyncService.GetProposalListAsync();

            if (proposalData?.Any() != true)
            {
                _logger.LogWarning("⚠️ No proposal list data retrieved from backup database");
                return;
            }

            _logger.LogInformation("✅ Retrieved {Count} proposal records from backup database", proposalData.Length);

            // Full refresh approach: Delete all then insert new data
            await BulkRefreshProposals(proposalData);

            _logger.LogInformation("🎯 ProposalSyncJob completed successfully. Processed {Count} proposal records",
                proposalData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in ProposalSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshProposals(ProposalListApiResponse[] proposalData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} proposal records", proposalData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllProposalRecords();

            // Step 2: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)proposalData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = proposalData.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", proposalData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(ProposalListApiResponse[] batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        // Deduplicate within the batch by composite key (block_time, proposal_id, proposal_tx_hash, withdrawal_hash)
        var seenKeys = new HashSet<string>();

        foreach (var proposal in batch)
        {
            // Create parameters for this record
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.block_time);
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.proposal_id);
            var proposalTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.proposal_tx_hash);
            var proposalIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.proposal_index);
            var proposalTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.proposal_type);

            // JSON fields
            var proposalDescParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.proposal_description);
            var depositParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.deposit);

            var returnAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.return_address);
            var proposedEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposal.proposed_epoch);

            var ratifiedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.ratified_epoch);
            var enactedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.enacted_epoch);
            var droppedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.dropped_epoch);
            var expiredEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.expired_epoch);
            var expirationParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.expiration);

            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_hash);

            var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_json);
            var metaCommentParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_comment);

            var metaLanguageParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_language);

            var metaIsValidParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.meta_is_valid);
            var withdrawalParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.withdrawal);
            var paramProposalParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", proposal.param_proposal);

            // Compute withdrawal_hash (md5 hex of withdrawal JSON text) to support ON CONFLICT with column list
            string? withdrawalText = proposal.withdrawal?.ToString();
            string? withdrawalHash = withdrawalText == null
                ? null
                : Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(withdrawalText))).ToLowerInvariant();
            var withdrawalHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", (object?)withdrawalHash ?? DBNull.Value);

            // Deduplicate check
            var dedupeKey = $"{proposal.block_time}|{proposal.proposal_id}|{proposal.proposal_tx_hash}|{withdrawalHash}";
            if (!seenKeys.Add(dedupeKey))
            {
                // rollback paramIndex for skipped row (not strictly necessary but keeps parity)
                continue;
            }

            valueParts.Add($@"(@{blockTimeParam.ParameterName}, @{proposalIdParam.ParameterName}, @{proposalTxHashParam.ParameterName}, 
                @{proposalIndexParam.ParameterName}, @{proposalTypeParam.ParameterName}, @{proposalDescParam.ParameterName}, 
                @{depositParam.ParameterName}, @{returnAddressParam.ParameterName}, @{proposedEpochParam.ParameterName}, 
                @{ratifiedEpochParam.ParameterName}, @{enactedEpochParam.ParameterName}, @{droppedEpochParam.ParameterName}, 
                @{expiredEpochParam.ParameterName}, @{expirationParam.ParameterName}, @{metaUrlParam.ParameterName}, 
                @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName}, @{metaCommentParam.ParameterName}, 
                @{metaLanguageParam.ParameterName}, @{metaIsValidParam.ParameterName}, @{withdrawalParam.ParameterName}, 
                @{paramProposalParam.ParameterName}, @{withdrawalHashParam.ParameterName})");
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_proposals_list 
            (block_time, proposal_id, proposal_tx_hash, proposal_index, proposal_type, proposal_description, 
             deposit, return_address, proposed_epoch, ratified_epoch, enacted_epoch, dropped_epoch, 
             expired_epoch, expiration, meta_url, meta_hash, meta_json, meta_comment, 
             meta_language, meta_is_valid, withdrawal, param_proposal, withdrawal_hash)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (block_time, proposal_id, proposal_tx_hash, withdrawal_hash) DO UPDATE SET 
              block_time = EXCLUDED.block_time,
              proposal_type = EXCLUDED.proposal_type,
              proposal_description = EXCLUDED.proposal_description,
              deposit = EXCLUDED.deposit,
              return_address = EXCLUDED.return_address,
              proposed_epoch = EXCLUDED.proposed_epoch,
              ratified_epoch = EXCLUDED.ratified_epoch,
              enacted_epoch = EXCLUDED.enacted_epoch,
              dropped_epoch = EXCLUDED.dropped_epoch,
              expired_epoch = EXCLUDED.expired_epoch,
              expiration = EXCLUDED.expiration,
              meta_url = EXCLUDED.meta_url,
              meta_hash = EXCLUDED.meta_hash,
              meta_json = EXCLUDED.meta_json,
              meta_comment = EXCLUDED.meta_comment,
              meta_language = EXCLUDED.meta_language,
              meta_is_valid = EXCLUDED.meta_is_valid,
              withdrawal = EXCLUDED.withdrawal,
              param_proposal = EXCLUDED.param_proposal,
              withdrawal_hash = EXCLUDED.withdrawal_hash";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllProposalRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing proposal records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_proposals_list");
            _logger.LogInformation("✅ Successfully deleted all existing proposal records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}