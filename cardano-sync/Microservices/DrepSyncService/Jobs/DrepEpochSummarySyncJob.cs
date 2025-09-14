using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]
public class DrepEpochSummarySyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<DrepEpochSummarySyncJob> _logger;

    public DrepEpochSummarySyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<DrepEpochSummarySyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 DrepEpochSummarySyncJob started at {StartTime}. Trigger: {Trigger}",
            startTime, context.Trigger.Key.Name);

        try
        {
            // Use Database Sync Service to fetch drep epoch summary data
            _logger.LogInformation("🔄 Submitting DRep epoch summary request to Database Sync Service...");
            var allData = await _databaseSyncService.GetDrepEpochSummaryAsync();
            var totalCount = allData?.Length ?? 0;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Gateway returned {Count} records (total: {Total}) in {Duration}",
                allData.Length, totalCount, duration);

            _logger.LogInformation("📝 Processing {Count} valid records", allData.Length);

            if (allData.Length == 0)
            {
                var completeDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ DrepEpochSummarySyncJob completed - no data to process! Duration: {Duration}. Trigger: {Trigger}",
                    completeDuration, context.Trigger.Key.Name);
                return;
            }

            await BulkRefreshDrepEpochSummary(allData.ToList());

            var finalDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("🎯 DrepEpochSummarySyncJob completed successfully at {EndTime}! Duration: {Duration}. " +
                "Processed {FetchedCount} records from API (Total API: {ApiTotal}). Trigger: {Trigger}",
                DateTime.UtcNow, finalDuration, allData.Length, totalCount, context.Trigger.Key.Name);

            // Log service stats
            _logger.LogInformation("📊 Database Sync Service Stats: {Stats}", "Completed successfully");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "❌ DrepEpochSummarySyncJob failed at {EndTime}! Duration: {Duration}. " +
                "Error: {ErrorMessage}. Trigger: {Trigger}",
                DateTime.UtcNow, errorDuration, ex.Message, context.Trigger.Key.Name);
            throw;
        }
    }

    private async Task BulkRefreshDrepEpochSummary(List<DrepEpochSummaryApiResponse> records)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} drep epoch summary records", records.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepEpochSummaryRecords();

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

    private async Task DeleteAllDrepEpochSummaryRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep epoch summary records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_epoch_summary");
            _logger.LogInformation("✅ Successfully deleted all existing drep epoch summary records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<DrepEpochSummaryApiResponse> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            if (record?.EpochNo == null)
                continue;

            // Create parameters for this record
            var epochNoParam = CreateParameter(command, $"p{paramIndex++}", record.EpochNo.Value);
            var amountParam = CreateParameter(command, $"p{paramIndex++}", record.Amount);
            var drepsParam = CreateParameter(command, $"p{paramIndex++}", record.Dreps ?? 0);

            valueParts.Add(
                $"(@{epochNoParam.ParameterName}, @{amountParam.ParameterName}, @{drepsParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps_epoch_summary 
            (epoch_no, amount, dreps)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (epoch_no) DO UPDATE SET 
              amount = EXCLUDED.amount,
              dreps = EXCLUDED.dreps";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private System.Data.Common.DbParameter CreateParameter(
        System.Data.Common.DbCommand command,
        string name,
        object? value
    )
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        command.Parameters.Add(param);
        return param;
    }
}
