using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolUpdatesSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolUpdatesSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FailedBatchRetryService _failedBatchRetryService;

    // Maximum pool_ids per batch for gateway processing
    private const int MaxPoolIdsPerRequest = 50; // Reduced from 80 to 50 to work within 60s Koios timeout

    public PoolUpdatesSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolUpdatesSyncJob> logger,
        IServiceProvider serviceProvider,
        FailedBatchRetryService failedBatchRetryService)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _failedBatchRetryService = failedBatchRetryService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("🚀 Starting PoolUpdatesSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get all pool_ids from md_pool_list table
            var poolIds = await _context.MDPoolLists
                .AsNoTracking()
                .Select(p => p.pool_id_bech32)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToListAsync();

            if (!poolIds.Any())
            {
                _logger.LogInformation("No pool_ids found in md_pool_list table");
                return;
            }

            _logger.LogInformation("Found {Count} pool_ids to process", poolIds.Count);

            // Choose between sequential and parallel processing
            var useParallelProcessing = true; // Can be moved to configuration
            List<(string PoolId, PoolUpdatesApiResponse[] Updates)> allPoolUpdates;
            List<string> failedPools;

            if (useParallelProcessing)
            {
                (allPoolUpdates, failedPools) = await ProcessPoolsParallel(poolIds);
            }
            else
            {
                (allPoolUpdates, failedPools) = await ProcessPoolsSequential(poolIds);
            }

            // Retry failed pools with longer timeout
            if (failedPools.Any())
            {
                _logger.LogWarning("⚠️ Retrying {Count} failed pools with extended timeout...", failedPools.Count);

                foreach (var poolId in failedPools)
                {
                    try
                    {
                        // Add extra delay before retry
                        await Task.Delay(2000);

                        var batchResults = await _databaseSyncService.GetPoolUpdatesAsync(poolId);

                        if (batchResults?.Any() == true)
                        {
                            allPoolUpdates.Add((poolId, batchResults));
                            _logger.LogInformation("✅ Retry successful for pool {PoolId}: {Count} updates", poolId, batchResults.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Retry failed for pool {PoolId}: {Message}", poolId, ex.Message);
                    }
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var totalUpdates = allPoolUpdates.Sum(x => x.Updates.Length);

            if (allPoolUpdates.Any())
            {
                await BulkRefreshPoolUpdates(allPoolUpdates);

                _logger.LogInformation("🎯 PoolUpdatesSyncJob completed successfully. Processed {Count} records in {Duration}",
                    totalUpdates, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Database Sync Stats: {Stats}", _databaseSyncService.GetDatabaseStats());
            }
            else
            {
                _logger.LogWarning("⚠️ No pool updates retrieved from API");
            }

            // Wait for failed batches to be processed
            await _failedBatchRetryService.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolUpdatesSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<(List<(string PoolId, PoolUpdatesApiResponse[] Updates)> Results, List<string> FailedPools)>
        ProcessPoolsParallel(List<string> poolIds)
    {
        // Reduce concurrency to work within PgBouncer/Postgres limits
        const int maxConcurrentPools = 4; // Previously 10
        const int delayBetweenPools = 200; // Previously 100

        _logger.LogInformation("🔄 Processing {Count} pools with max {MaxConcurrent} concurrent pools",
            poolIds.Count, maxConcurrentPools);

        var allPoolUpdates = new List<(string PoolId, PoolUpdatesApiResponse[] Updates)>();
        var failedPools = new List<string>();
        var semaphore = new SemaphoreSlim(maxConcurrentPools, maxConcurrentPools);
        var tasks = new List<Task<(string PoolId, PoolUpdatesApiResponse[]? Updates, bool Success)>>();

        // Create tasks for all pools
        foreach (var poolId in poolIds)
        {
            var task = ProcessPoolWithSemaphoreAsync(poolId, semaphore, delayBetweenPools);
            tasks.Add(task);
        }

        // Wait for all tasks to complete
        _logger.LogInformation("⏳ Waiting for all {Count} pool tasks to complete...", tasks.Count);
        var results = await Task.WhenAll(tasks);

        // Collect results
        foreach (var (poolId, updates, success) in results)
        {
            if (success && updates?.Any() == true)
            {
                allPoolUpdates.Add((poolId, updates));
            }
            else if (!success)
            {
                failedPools.Add(poolId);
            }
        }

        _logger.LogInformation("✅ All pools completed. Success: {SuccessCount}, Failed: {FailedCount}",
            allPoolUpdates.Count, failedPools.Count);

        return (allPoolUpdates, failedPools);
    }

    private async Task<(string PoolId, PoolUpdatesApiResponse[]? Updates, bool Success)>
        ProcessPoolWithSemaphoreAsync(string poolId, SemaphoreSlim semaphore, int delayBetweenPools)
    {
        await semaphore.WaitAsync();

        try
        {
            _logger.LogDebug("📡 Processing pool {PoolId} (Concurrent: {CurrentConcurrent})",
                poolId, semaphore.CurrentCount);

            // Add small delay to avoid overwhelming the database
            await Task.Delay(delayBetweenPools);

            var batchResults = await _databaseSyncService.GetPoolUpdatesAsync(poolId);

            if (batchResults?.Any() == true)
            {
                _logger.LogInformation("✅ Pool {PoolId} returned {Count} updates", poolId, batchResults.Length);
                return (poolId, batchResults, true);
            }
            else
            {
                _logger.LogDebug("⚪ Pool {PoolId} has no updates", poolId);
                return (poolId, null, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pool {PoolId}: {Message}", poolId, ex.Message);

            // Add failed pool to retry service
            var failedBatch = new FailedBatchRetryService.FailedBatch
            {
                UtxoRefs = new List<string> { poolId }, // Use poolId as the identifier
                BatchNumber = 0, // Single pool batch
                TotalBatches = 1,
                RetryCount = 0,
                FirstFailureTime = DateTime.UtcNow,
                LastRetryTime = DateTime.UtcNow,
                FailureReason = ex.Message,
                LastException = ex,
                ServiceName = "PoolUpdatesSyncJob"
            };

            _failedBatchRetryService.AddFailedBatch(failedBatch);

            return (poolId, null, false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Keep the original sequential method as fallback
    private async Task<(List<(string PoolId, PoolUpdatesApiResponse[] Updates)> Results, List<string> FailedPools)>
        ProcessPoolsSequential(List<string> poolIds)
    {
        // Process in batches using Gateway Adapter
        const int batchSize = 500; // Reduced from 80 to 50 to work within 60s Koios timeout
        var totalBatches = (int)Math.Ceiling((double)poolIds.Count / batchSize);
        var allPoolUpdates = new List<(string PoolId, PoolUpdatesApiResponse[] Updates)>();
        var failedPools = new List<string>();

        for (int i = 0; i < totalBatches; i++)
        {
            var batch = poolIds.Skip(i * batchSize).Take(batchSize).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToArray();
            _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} pool_ids",
                i + 1, totalBatches, batch.Length);

            foreach (var poolId in batch)
            {
                try
                {
                    // Use Database Sync Service for optimized processing
                    var batchResults = await _databaseSyncService.GetPoolUpdatesAsync(poolId);

                    if (batchResults?.Any() == true)
                    {
                        allPoolUpdates.Add((poolId, batchResults));
                        _logger.LogInformation("✅ Pool {PoolId} returned {Count} updates", poolId, batchResults.Length);
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Pool {PoolId} has no updates", poolId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing pool {PoolId}: {Message}", poolId, ex.Message);

                    // Add failed pool to retry service
                    var failedBatch = new FailedBatchRetryService.FailedBatch
                    {
                        UtxoRefs = new List<string> { poolId }, // Use poolId as the identifier
                        BatchNumber = 0, // Single pool batch
                        TotalBatches = 1,
                        RetryCount = 0,
                        FirstFailureTime = DateTime.UtcNow,
                        LastRetryTime = DateTime.UtcNow,
                        FailureReason = ex.Message,
                        LastException = ex,
                        ServiceName = "PoolUpdatesSyncJob"
                    };

                    _failedBatchRetryService.AddFailedBatch(failedBatch);
                    failedPools.Add(poolId);
                }
            }

            // Reduced delay since Gateway handles rate limiting
            if (i < totalBatches - 1)
            {
                await Task.Delay(100); // Increased from 50ms to 100ms for better stability
            }
        }

        return (allPoolUpdates, failedPools);
    }

    private async Task BulkRefreshPoolUpdates(List<(string PoolId, PoolUpdatesApiResponse[] Updates)> poolUpdates)
    {
        var totalRecords = poolUpdates.Sum(x => x.Updates.Length);
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool updates records", totalRecords);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolUpdatesRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var allUpdates = new List<(string PoolId, PoolUpdatesApiResponse Update)>();

            // Flatten the updates list with their pool IDs
            foreach (var (poolId, updates) in poolUpdates)
            {
                foreach (var update in updates)
                {
                    if (!string.IsNullOrWhiteSpace(update.pool_id_bech32))
                    {
                        allUpdates.Add((poolId, update));
                    }
                }
            }

            var totalBatches = (int)Math.Ceiling((double)allUpdates.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allUpdates.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", allUpdates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllPoolUpdatesRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool updates records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_updates");
            _logger.LogInformation("✅ Successfully deleted all existing pool updates records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string PoolId, PoolUpdatesApiResponse Update)> batch, int batchNumber, int totalBatches)
    {
        // Create a new DbContext scope for each batch to avoid connection pool issues
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

        using var command = context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        try
        {
            if (batch.Count == 0) return;

            var valueParts = new List<string>();
            var paramIndex = 0;

            foreach (var (poolId, record) in batch)
            {
                // Create parameters
                var txHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.tx_hash);
                var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_time);
                var poolIdBech32Param = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_bech32);
                var poolIdHexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_hex);
                var activeEpochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_epoch_no);
                var vrfKeyHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.vrf_key_hash);
                var marginParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.margin);
                var fixedCostParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.fixed_cost);
                var pledgeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pledge);
                var rewardAddrParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.reward_addr);
                var ownersParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.owners);
                var relaysParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.relays);
                var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
                var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
                var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);
                var updateTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.update_type);
                var retiringEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.retiring_epoch);

                valueParts.Add($"(@{txHashParam.ParameterName}, @{blockTimeParam.ParameterName}, @{poolIdBech32Param.ParameterName}, @{poolIdHexParam.ParameterName}, @{activeEpochNoParam.ParameterName}, @{vrfKeyHashParam.ParameterName}, @{marginParam.ParameterName}, @{fixedCostParam.ParameterName}, @{pledgeParam.ParameterName}, @{rewardAddrParam.ParameterName}, @{ownersParam.ParameterName}, @{relaysParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName}, @{updateTypeParam.ParameterName}, @{retiringEpochParam.ParameterName})");
            }

            command.CommandText = $@"INSERT INTO md_pool_updates 
                (tx_hash, block_time, pool_id_bech32, pool_id_hex, active_epoch_no, 
                 vrf_key_hash, margin, fixed_cost, pledge, reward_addr, owners, 
                 relays, meta_url, meta_hash, meta_json, update_type, retiring_epoch)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (tx_hash) DO UPDATE SET 
                  block_time = EXCLUDED.block_time,
                  pool_id_bech32 = EXCLUDED.pool_id_bech32,
                  pool_id_hex = EXCLUDED.pool_id_hex,
                  active_epoch_no = EXCLUDED.active_epoch_no,
                  vrf_key_hash = EXCLUDED.vrf_key_hash,
                  margin = EXCLUDED.margin,
                  fixed_cost = EXCLUDED.fixed_cost,
                  pledge = EXCLUDED.pledge,
                  reward_addr = EXCLUDED.reward_addr,
                  owners = EXCLUDED.owners,
                  relays = EXCLUDED.relays,
                  meta_url = EXCLUDED.meta_url,
                  meta_hash = EXCLUDED.meta_hash,
                  meta_json = EXCLUDED.meta_json,
                  update_type = EXCLUDED.update_type,
                  retiring_epoch = EXCLUDED.retiring_epoch";

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