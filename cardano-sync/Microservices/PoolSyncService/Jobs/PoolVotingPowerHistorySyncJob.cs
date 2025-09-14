using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolVotingPowerHistorySyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolVotingPowerHistorySyncJob> _logger;

    public PoolVotingPowerHistorySyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolVotingPowerHistorySyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 PoolVotingPowerHistorySyncJob started at {StartTime}. Trigger: {Trigger}",
            startTime, context.Trigger.Key.Name);

        try
        {
            // Use Database Sync Service to fetch pool voting power history data
            _logger.LogInformation("🔄 Submitting Pool voting power history request to Database Sync Service...");
            var allData = await _databaseSyncService.GetPoolVotingPowerHistoryAsync();
            var totalCount = allData.Length;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Gateway returned {Count} records (total: {Total}) in {Duration}",
                allData.Length, totalCount, duration);

            _logger.LogInformation("📝 Processing {Count} valid records", allData.Length);

            if (allData.Length == 0)
            {
                var completeDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ PoolVotingPowerHistorySyncJob completed - no data to process! Duration: {Duration}. Trigger: {Trigger}",
                    completeDuration, context.Trigger.Key.Name);
                return;
            }

            await BulkRefreshPoolVotingPowerHistory(allData.ToList());

            var finalDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("🎯 PoolVotingPowerHistorySyncJob completed successfully at {EndTime}! Duration: {Duration}. " +
                "Processed {FetchedCount} records from API (Total API: {ApiTotal}). Trigger: {Trigger}",
                DateTime.UtcNow, finalDuration, allData.Length, totalCount, context.Trigger.Key.Name);

            // Log completion stats
            _logger.LogInformation("📊 Job completed successfully with {Count} records processed", allData.Length);
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "❌ PoolVotingPowerHistorySyncJob failed at {EndTime}! Duration: {Duration}. " +
                "Error: {ErrorMessage}. Trigger: {Trigger}",
                DateTime.UtcNow, errorDuration, ex.Message, context.Trigger.Key.Name);
            throw;
        }
    }

    private async Task BulkRefreshPoolVotingPowerHistory(List<PoolVotingPowerHistoryApiResponse> records)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool voting power history records", records.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolVotingPowerHistoryRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)records.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = records.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllPoolVotingPowerHistoryRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool voting power history records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pools_voting_power_history");
            _logger.LogInformation("✅ Successfully deleted all existing pool voting power history records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<PoolVotingPowerHistoryApiResponse> batch, int batchNumber, int totalBatches)
    {
        // Use the existing context for batch operations
        var context = _context;

        using var command = context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        try
        {
            var valueParts = new List<string>();
            var paramIndex = 0;

            foreach (var record in batch)
            {
                if (string.IsNullOrEmpty(record.pool_id_bech32))
                    continue;

                // Create parameters for this record
                var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_bech32);
                var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no ?? 0);
                var amountParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.amount);

                valueParts.Add(
                    $"(@{poolIdParam.ParameterName}, @{epochNoParam.ParameterName}, @{amountParam.ParameterName})"
                );
            }

            if (valueParts.Count == 0)
                return;

            command.CommandText = $@"INSERT INTO md_pools_voting_power_history 
                (pool_id_bech32, epoch_no, amount)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32, epoch_no) DO UPDATE SET 
                  amount = EXCLUDED.amount";

            await command.ExecuteNonQueryAsync();

            // Explicitly close the connection to return it to the pool
            command.Connection.Close();

            _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
                batchNumber, totalBatches, valueParts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inserting batch {BatchNumber}/{TotalBatches}: {Message}",
                batchNumber, totalBatches, ex.Message);

            // Ensure connection is closed even on error
            if (command.Connection?.State == ConnectionState.Open)
                command.Connection.Close();

            throw;
        }
    }


}