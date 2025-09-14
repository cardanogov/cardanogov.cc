using EpochSyncService.ApiResponses;
using EpochSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace EpochSyncService.Jobs;


[DisallowConcurrentExecution]
public class EpochSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<EpochSyncJob> _logger;

    public EpochSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<EpochSyncJob> logger
    )
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        var triggerName = context.Trigger.Key.Name;

        try
        {
            _logger.LogInformation(
                "EpochSyncJob started at {StartTime} by trigger: {TriggerName}",
                startTime,
                triggerName
            );

            await SyncEpochDataAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "EpochSyncJob completed successfully at {Time}. Duration: {Duration}. Trigger: {TriggerName}",
                DateTime.UtcNow,
                duration,
                triggerName
            );
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "EpochSyncJob failed at {Time} after {Duration}. Error: {Error}. Trigger: {TriggerName}",
                DateTime.UtcNow,
                duration,
                ex.Message,
                triggerName
            );

            // Re-throw để Quartz biết job đã fail
            throw;
        }
    }

    private async Task SyncEpochDataAsync()
    {
        _logger.LogInformation("🚀 Starting to sync epoch data from backup database...");

        var startTime = DateTime.UtcNow;

        // Get all epoch data from backup database
        var epochData = await _databaseSyncService.GetEpochInfoAsync();

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "✅ Database returned {Count} epoch records in {Duration}",
            epochData.Length,
            duration
        );

        if (epochData.Any())
        {
            await BulkRefreshEpochData(epochData);
            _logger.LogInformation(
                "🎯 EpochSyncJob completed successfully. Processed {TotalCount} epoch records in {ElapsedTime}",
                epochData.Length,
                DateTime.UtcNow - startTime
            );
        }
        else
        {
            _logger.LogWarning("⚠️ No epoch data retrieved from backup database");
        }
    }

    private async Task BulkRefreshEpochData(EpochApiResponse[] epochData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} epoch records", epochData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllEpochRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)epochData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = epochData.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", epochData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<EpochApiResponse> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            // Create parameters for this record
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no);
            var outSumParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.out_sum);
            var feesParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.fees);
            var txCountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.tx_count);
            var blkCountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.blk_count);
            var startTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.start_time);
            var endTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.end_time);
            var firstBlockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.first_block_time);
            var lastBlockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.last_block_time);
            var activeStakeParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.active_stake);
            var totalRewardsParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.total_rewards);
            var avgBlkRewardParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.avg_blk_reward);

            valueParts.Add(
                $"(@{epochNoParam.ParameterName}, @{outSumParam.ParameterName}, @{feesParam.ParameterName}, @{txCountParam.ParameterName}, @{blkCountParam.ParameterName}, @{startTimeParam.ParameterName}, @{endTimeParam.ParameterName}, @{firstBlockTimeParam.ParameterName}, @{lastBlockTimeParam.ParameterName}, @{activeStakeParam.ParameterName}, @{totalRewardsParam.ParameterName}, @{avgBlkRewardParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        // Insert new records
        command.CommandText =
            $@"INSERT INTO md_epoch 
            (epoch_no, out_sum, fees, tx_count, blk_count, start_time, end_time,
             first_block_time, last_block_time, active_stake, total_rewards, avg_blk_reward)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (epoch_no) DO UPDATE SET 
              out_sum = EXCLUDED.out_sum,
              fees = EXCLUDED.fees,
              tx_count = EXCLUDED.tx_count,
              blk_count = EXCLUDED.blk_count,
              start_time = EXCLUDED.start_time,
              end_time = EXCLUDED.end_time,
              first_block_time = EXCLUDED.first_block_time,
              last_block_time = EXCLUDED.last_block_time,
              active_stake = EXCLUDED.active_stake,
              total_rewards = EXCLUDED.total_rewards,
              avg_blk_reward = EXCLUDED.avg_blk_reward";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} epochs)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllEpochRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing epoch records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_epoch");
            _logger.LogInformation("✅ Successfully deleted all existing epoch records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing epoch records: {Message}", ex.Message);
            throw;
        }
    }
}
