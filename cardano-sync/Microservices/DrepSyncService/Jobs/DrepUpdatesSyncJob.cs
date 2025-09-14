using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]
public class DrepUpdatesSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<DrepUpdatesSyncJob> _logger;

    public DrepUpdatesSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<DrepUpdatesSyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting DrepUpdatesSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Use Database Sync Service to get ALL DRep updates
            _logger.LogInformation("🔄 Submitting DRep updates request to Database Sync Service...");
            var updatesData = await _databaseSyncService.GetDrepUpdatesAsync();
            var totalCount = updatesData?.Length ?? 0;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Database Sync Service returned {Count} update records (total: {Total}) in {Duration}",
                updatesData.Length, totalCount, duration);

            if (updatesData.Any())
            {
                await BulkRefreshDrepUpdates(updatesData);

                _logger.LogInformation("🎯 DrepUpdatesSyncJob completed successfully. Processed {TotalCount} update records in {ElapsedTime}",
                    totalCount, DateTime.UtcNow - startTime);

                // Log simple stats since we're using Database Sync Service
                _logger.LogInformation("📊 Database Sync Service Stats - Using direct database access");
            }
            else
            {
                _logger.LogWarning("⚠️ No DRep updates data retrieved from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in DrepUpdatesSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshDrepUpdates(DrepUpdatesApiResponse[] updatesData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} DRep update records", updatesData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepUpdatesRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)updatesData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = updatesData.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} update records", updatesData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllDrepUpdatesRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep updates records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_updates");
            _logger.LogInformation("✅ Successfully deleted all existing drep updates records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(DrepUpdatesApiResponse[] batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            if (string.IsNullOrEmpty(record.drep_id) || string.IsNullOrEmpty(record.update_tx_hash))
                continue;

            // Create parameters for this record
            var drepIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_id);
            var hexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.hex);
            var hasScriptParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.has_script);
            var updateTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.update_tx_hash);
            var certIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.cert_index);
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_time);
            var actionParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.action);
            var depositParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.deposit);
            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
            var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);

            valueParts.Add(
                $"(@{drepIdParam.ParameterName}, @{hexParam.ParameterName}, @{hasScriptParam.ParameterName}, @{updateTxHashParam.ParameterName}, @{certIndexParam.ParameterName}, @{blockTimeParam.ParameterName}, @{actionParam.ParameterName}, @{depositParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps_updates 
            (drep_id, hex, has_script, update_tx_hash, cert_index, block_time, action, deposit, meta_url, meta_hash, meta_json)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (update_tx_hash, cert_index) DO UPDATE SET
                drep_id = EXCLUDED.drep_id,
                hex = EXCLUDED.hex,
                has_script = EXCLUDED.has_script,
                block_time = EXCLUDED.block_time,
                action = EXCLUDED.action,
                deposit = EXCLUDED.deposit,
                meta_url = EXCLUDED.meta_url,
                meta_hash = EXCLUDED.meta_hash,
                meta_json = EXCLUDED.meta_json";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records, deduplicated from {OriginalCount})",
            batchNumber, totalBatches, valueParts.Count, batch.Length);
    }


}
