using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;
using VotingSyncService.ApiResponses;
using VotingSyncService.Services;

namespace VotingSyncService.Jobs;


[DisallowConcurrentExecution]
public class VoterProposalListSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly ILogger<VoterProposalListSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;

    public VoterProposalListSyncJob(
        CardanoDbContext context,
        ILogger<VoterProposalListSyncJob> logger,
        DatabaseSyncService databaseSyncService
    )
    {
        _context = context;
        _logger = logger;
        _databaseSyncService = databaseSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("🚀 Starting VoterProposalListSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation(
                "🔄 Fetching voter proposal list data from backup database..."
            );

            var voterProposalData = await _databaseSyncService.GetVoterProposalListAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "✅ Retrieved {Count} voter proposal records from backup database in {Duration}",
                voterProposalData.Length,
                duration
            );

            if (voterProposalData.Any())
            {
                await BulkRefreshVoterProposalList(voterProposalData);

                _logger.LogInformation(
                    "🎯 VoterProposalListSyncJob completed successfully. Processed {TotalCount} voter proposal records in {ElapsedTime}",
                    voterProposalData.Length,
                    DateTime.UtcNow - startTime
                );
            }
            else
            {
                _logger.LogWarning("⚠️ No voter proposal data received from backup database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in VoterProposalListSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshVoterProposalList(VoterProposalListApiResponse[] voterProposalData)
    {
        _logger.LogInformation(
            "🔄 Starting full refresh for {Count} voter proposal records using raw SQL",
            voterProposalData.Length
        );

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllVoterProposalListRecords();

            // Step 2: Insert all new records in batches
            var processedCount = 0;
            const int batchSize = 500;

            for (int i = 0; i < voterProposalData.Length; i += batchSize)
            {
                var batch = voterProposalData.Skip(i).Take(batchSize).ToArray();

                if (batch.Length > 0)
                {
                    await InsertBatchWithRawSql(batch);
                    processedCount += batch.Length;

                    _logger.LogDebug(
                        "💾 Inserted batch {BatchStart}-{BatchEnd} ({Processed}/{Total})",
                        i + 1,
                        Math.Min(i + batchSize, voterProposalData.Length),
                        processedCount,
                        voterProposalData.Length
                    );
                }
            }

            _logger.LogInformation(
                "✅ Full refresh completed successfully. Processed {ProcessedCount} voter proposal list records using raw SQL",
                processedCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllVoterProposalListRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing voter proposal list records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_voters_proposal_list");
            _logger.LogInformation("✅ Successfully deleted all existing voter proposal list records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(VoterProposalListApiResponse[] batch)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var uniqueRecords = batch
            .Where(r => !string.IsNullOrEmpty(r.proposal_id) && !string.IsNullOrEmpty(r.proposal_tx_hash))
            .GroupBy(r => new { r.proposal_id, r.proposal_tx_hash, r.proposal_index })
            .Select(g => g.OrderByDescending(r => r.block_time).First())
            .ToArray();

        if (uniqueRecords.Length == 0)
            return;

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in uniqueRecords)
        {
            // Create parameters for this record
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_time);
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_id);
            var proposalTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_tx_hash);
            var proposalIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_index);
            var proposalTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposal_type);
            var proposalDescriptionParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.proposal_description);
            var depositParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.deposit);
            var returnAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.return_address);
            var proposedEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.proposed_epoch);
            var ratifiedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.ratified_epoch);
            var enactedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.enacted_epoch);
            var droppedEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.dropped_epoch);
            var expiredEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.expired_epoch);
            var expirationParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.expiration);
            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
            var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);
            var metaCommentParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_comment);
            var metaLanguageParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_language);
            var metaIsValidParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_is_valid);
            var withdrawalParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.withdrawal);
            var paramProposalParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.param_proposal);

            valueParts.Add(
                $"(@{blockTimeParam.ParameterName}, @{proposalIdParam.ParameterName}, @{proposalTxHashParam.ParameterName}, @{proposalIndexParam.ParameterName}, @{proposalTypeParam.ParameterName}, @{proposalDescriptionParam.ParameterName}, @{depositParam.ParameterName}, @{returnAddressParam.ParameterName}, @{proposedEpochParam.ParameterName}, @{ratifiedEpochParam.ParameterName}, @{enactedEpochParam.ParameterName}, @{droppedEpochParam.ParameterName}, @{expiredEpochParam.ParameterName}, @{expirationParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName}, @{metaCommentParam.ParameterName}, @{metaLanguageParam.ParameterName}, @{metaIsValidParam.ParameterName}, @{withdrawalParam.ParameterName}, @{paramProposalParam.ParameterName})"
            );
        }

        command.CommandText = $@"INSERT INTO md_voters_proposal_list 
            (block_time, proposal_id, proposal_tx_hash, proposal_index, proposal_type,
             proposal_description, deposit, return_address, proposed_epoch, ratified_epoch,
             enacted_epoch, dropped_epoch, expired_epoch, expiration, meta_url,
             meta_hash, meta_json, meta_comment, meta_language, meta_is_valid,
             withdrawal, param_proposal)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (proposal_id, proposal_tx_hash, proposal_index) DO UPDATE SET 
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
              param_proposal = EXCLUDED.param_proposal";

        await command.ExecuteNonQueryAsync();
    }


}