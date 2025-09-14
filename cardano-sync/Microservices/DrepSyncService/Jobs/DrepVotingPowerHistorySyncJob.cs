using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]
public class DrepVotingPowerHistorySyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<DrepVotingPowerHistorySyncJob> _logger;

    public DrepVotingPowerHistorySyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<DrepVotingPowerHistorySyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 DrepVotingPowerHistorySyncJob started at {StartTime}. Trigger: {Trigger}",
            startTime, context.Trigger.Key.Name);

        try
        {
            // Use Database Sync Service to fetch drep voting power history data
            _logger.LogInformation("🔄 Submitting DRep voting power history request to Database Sync Service...");
            var allData = await _databaseSyncService.GetDrepVotingPowerHistoryAsync();
            var totalCount = allData?.Length ?? 0;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Gateway returned {Count} records (total: {Total}) in {Duration}",
                allData.Length, totalCount, duration);

            _logger.LogInformation("📝 Processing {Count} valid records", allData.Length);

            if (allData.Length == 0)
            {
                var completeDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ DrepVotingPowerHistorySyncJob completed - no data to process! Duration: {Duration}. Trigger: {Trigger}",
                    completeDuration, context.Trigger.Key.Name);
                return;
            }

            await BulkRefreshDrepVotingPowerHistory(allData.ToList());

            var finalDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("🎯 DrepVotingPowerHistorySyncJob completed successfully at {EndTime}! Duration: {Duration}. " +
                "Processed {FetchedCount} records from API (Total API: {ApiTotal}). Trigger: {Trigger}",
                DateTime.UtcNow, finalDuration, allData.Length, totalCount, context.Trigger.Key.Name);

            // Log service stats
            _logger.LogInformation("📊 Database Sync Service Stats: {Stats}", "Completed successfully");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "❌ DrepVotingPowerHistorySyncJob failed at {EndTime}! Duration: {Duration}. " +
                "Error: {ErrorMessage}. Trigger: {Trigger}",
                DateTime.UtcNow, errorDuration, ex.Message, context.Trigger.Key.Name);
            throw;
        }
    }

    private async Task BulkRefreshDrepVotingPowerHistory(List<DrepVotingPowerHistoryApiResponse> records)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} drep voting power history records", records.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepVotingPowerHistoryRecords();

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

    private async Task DeleteAllDrepVotingPowerHistoryRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep voting power history records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_voting_power_history");
            _logger.LogInformation("✅ Successfully deleted all existing drep voting power history records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<DrepVotingPowerHistoryApiResponse> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            if (string.IsNullOrEmpty(record.drep_id))
                continue;

            // Create parameters for this record
            var drepIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_id);
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no ?? 0);
            var amountParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.amount);

            valueParts.Add(
                $"(@{drepIdParam.ParameterName}, @{epochNoParam.ParameterName}, @{amountParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps_voting_power_history 
            (drep_id, epoch_no, amount)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (drep_id, epoch_no) DO UPDATE SET 
              amount = EXCLUDED.amount";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }


}