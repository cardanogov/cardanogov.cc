using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]
public class DrepMetadataSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<DrepMetadataSyncJob> _logger;

    // Maximum drep_ids per batch for database sync processing
    private const int MaxDrepIdsPerRequest = 80;

    public DrepMetadataSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<DrepMetadataSyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting DrepMetadataSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get all drep_ids from md_dreps_list table
            var drepIds = await _context.MDDrepsLists
                .AsNoTracking()
                .Select(d => d.drep_id)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToListAsync();

            if (!drepIds.Any())
            {
                _logger.LogInformation("No drep_ids found in md_dreps_list table");
                return;
            }

            _logger.LogInformation("Found {Count} drep_ids to process", drepIds.Count);

            // Process in batches using Database Sync Service
            const int batchSize = 80;
            var totalBatches = (int)Math.Ceiling((double)drepIds.Count / batchSize);
            var allDrepMetadata = new List<DrepMetadataApiResponse>();

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = drepIds.Skip(i * batchSize).Take(batchSize).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToArray();
                _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} drep_ids",
                    i + 1, totalBatches, batch.Length);

                try
                {
                    // Use Database Sync Service for optimized processing
                    var batchResults = await _databaseSyncService.GetDrepMetadataAsync(batch);

                    if (batchResults?.Any() == true)
                    {
                        allDrepMetadata.AddRange(batchResults);
                        _logger.LogInformation("✅ Batch {BatchNumber} returned {Count} records", i + 1, batchResults.Length);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Batch {BatchNumber} returned no data", i + 1);
                    }

                    // Reduced delay since Database Sync Service handles rate limiting
                    if (i < totalBatches - 1)
                    {
                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing batch {BatchNumber}: {Message}", i + 1, ex.Message);
                    // Continue with next batch instead of failing the entire job
                }
            }

            var duration = DateTime.UtcNow - startTime;

            if (allDrepMetadata.Any())
            {
                await BulkRefreshDrepMetadata(allDrepMetadata);

                _logger.LogInformation("🎯 DrepMetadataSyncJob completed successfully. Processed {Count} records in {Duration}",
                    allDrepMetadata.Count, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Database Sync Service Stats: {Stats}", "Completed successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ No drep metadata retrieved from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in DrepMetadataSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshDrepMetadata(List<DrepMetadataApiResponse> drepMetadata)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} drep metadata records", drepMetadata.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepMetadataRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)drepMetadata.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = drepMetadata.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", drepMetadata.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<DrepMetadataApiResponse> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            if (string.IsNullOrWhiteSpace(record.drep_id))
                continue;

            // Create parameters for this record
            var drepIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_id);
            var hexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.hex);
            var hasScriptParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.has_script);
            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
            var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);
            var bytesParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.bytes);
            var warningParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.warning);
            var languageParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.language);
            var commentParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.comment);
            var isValidParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.is_valid);

            valueParts.Add(
                $"(@{drepIdParam.ParameterName}, @{hexParam.ParameterName}, @{hasScriptParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName}, @{bytesParam.ParameterName}, @{warningParam.ParameterName}, @{languageParam.ParameterName}, @{commentParam.ParameterName}, @{isValidParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps_metadata 
            (drep_id, hex, has_script, meta_url, meta_hash, meta_json, bytes, warning, language, comment, is_valid)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (drep_id) DO UPDATE SET 
              hex = EXCLUDED.hex,
              has_script = EXCLUDED.has_script,
              meta_url = EXCLUDED.meta_url,
              meta_hash = EXCLUDED.meta_hash,
              meta_json = EXCLUDED.meta_json,
              bytes = EXCLUDED.bytes,
              warning = EXCLUDED.warning,
              language = EXCLUDED.language,
              comment = EXCLUDED.comment,
              is_valid = EXCLUDED.is_valid";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }



    private async Task DeleteAllDrepMetadataRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep metadata records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_metadata");
            _logger.LogInformation("✅ Successfully deleted all existing drep metadata records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}