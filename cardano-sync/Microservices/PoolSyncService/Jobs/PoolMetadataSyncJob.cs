using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolMetadataSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolMetadataSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Maximum pool_ids per batch for gateway processing
    private const int MaxPoolIdsPerRequest = 50; // Reduced from 80 to 50 to work within 60s Koios timeout

    public PoolMetadataSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolMetadataSyncJob> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(" Starting PoolMetadataSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get all pool_id_bech32 from md_pool_list table
            var poolIds = await _context.MDPoolLists
                .AsNoTracking()
                .Select(p => p.pool_id_bech32)
                .Distinct()
                .Where(id => !string.IsNullOrEmpty(id))
                .ToListAsync();

            if (!poolIds.Any())
            {
                _logger.LogInformation("No pool_id_bech32 found in md_pool_list table");
                return;
            }

            _logger.LogInformation("Found {Count} pool_id_bech32 to process", poolIds.Count);

            // Process in batches using Database Sync Service
            const int batchSize = 50; // Batch size for processing
            var totalBatches = (int)Math.Ceiling((double)poolIds.Count / batchSize);
            var allPoolMetadata = new List<PoolMetadataApiResponse>();

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = poolIds.Skip(i * batchSize).Take(batchSize).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToArray();
                _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} pool_ids",
                    i + 1, totalBatches, batch.Length);

                try
                {
                    // Use Database Sync Service for processing
                    var batchResults = await _databaseSyncService.GetPoolMetadataAsync(batch);

                    if (batchResults?.Any() == true)
                    {
                        allPoolMetadata.AddRange(batchResults);
                        _logger.LogInformation("✅ Batch {BatchNumber} returned {Count} records", i + 1, batchResults.Length);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Batch {BatchNumber} returned no data", i + 1);
                    }

                    // Small delay between batches
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

            if (allPoolMetadata.Any())
            {
                await BulkRefreshPoolMetadata(allPoolMetadata);

                _logger.LogInformation(" PoolMetadataSyncJob completed successfully. Processed {Count} records in {Duration}",
                    allPoolMetadata.Count, duration);

                // Log adapter stats
                _logger.LogInformation(" Adapter Stats: {Stats}", _databaseSyncService.GetDatabaseStats());
            }
            else
            {
                _logger.LogWarning("⚠️ No pool metadata retrieved from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolMetadataSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshPoolMetadata(List<PoolMetadataApiResponse> poolMetadata)
    {
        _logger.LogInformation(" Starting full refresh for {Count} pool metadata records", poolMetadata.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolMetadataRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)poolMetadata.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = poolMetadata.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", poolMetadata.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<PoolMetadataApiResponse> batch, int batchNumber, int totalBatches)
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
                if (string.IsNullOrWhiteSpace(record.pool_id_bech32))
                    continue;

                // Create parameters for this record
                var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_bech32);
                var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
                var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
                var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);

                valueParts.Add(
                    $"(@{poolIdParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName})"
                );
            }

            if (valueParts.Count == 0)
                return;

            command.CommandText = $@"INSERT INTO md_pool_metadata
                (pool_id_bech32, meta_url, meta_hash, meta_json)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32) DO UPDATE SET
                    meta_url = EXCLUDED.meta_url,
                    meta_hash = EXCLUDED.meta_hash,
                    meta_json = EXCLUDED.meta_json";

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



    private async Task DeleteAllPoolMetadataRecords()
    {
        _logger.LogInformation("���️ Deleting all existing pool metadata records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_metadata");
            _logger.LogInformation("✅ Successfully deleted all existing pool metadata records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}
