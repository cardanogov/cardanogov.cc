using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolStakeSnapshotSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolStakeSnapshotSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FailedBatchRetryService _failedBatchRetryService;

    // Maximum pool_ids per batch for gateway processing
    private const int MaxPoolIdsPerRequest = 50; // Reduced from 80 to 50 to work within 60s Koios timeout

    public PoolStakeSnapshotSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolStakeSnapshotSyncJob> logger,
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
        _logger.LogInformation("🚀 Starting PoolStakeSnapshotSyncJob at {Time}", DateTime.UtcNow);

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
            List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)> allPoolStakeSnapshots;
            List<string> failedPools;

            if (useParallelProcessing)
            {
                (allPoolStakeSnapshots, failedPools) = await ProcessPoolsParallel(poolIds);
            }
            else
            {
                (allPoolStakeSnapshots, failedPools) = await ProcessPoolsSequential(poolIds);
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

                        var batchResults = await _databaseSyncService.GetPoolStakeSnapshotAsync(poolId);

                        if (batchResults?.Any() == true)
                        {
                            allPoolStakeSnapshots.Add((poolId, batchResults));
                            _logger.LogInformation("✅ Retry successful for pool {PoolId}: {Count} snapshots", poolId, batchResults.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Retry failed for pool {PoolId}: {Message}", poolId, ex.Message);
                    }
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var totalSnapshots = allPoolStakeSnapshots.Sum(x => x.Snapshots.Length);

            if (allPoolStakeSnapshots.Any())
            {
                await BulkRefreshPoolStakeSnapshots(allPoolStakeSnapshots);

                _logger.LogInformation("🎯 PoolStakeSnapshotSyncJob completed successfully. Processed {Count} records in {Duration}",
                    totalSnapshots, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Job completed successfully with {Count} pools processed", allPoolStakeSnapshots.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ No pool stake snapshots retrieved from API");
            }

            // Wait for failed batches to be processed
            await _failedBatchRetryService.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolStakeSnapshotSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<(List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)> Results, List<string> FailedPools)>
        ProcessPoolsParallel(List<string> poolIds)
    {
        // Reduce concurrency to work within PgBouncer/Postgres limits
        const int maxConcurrentPools = 4; // Previously 10
        const int delayBetweenPools = 200; // Previously 100

        _logger.LogInformation("🔄 Processing {Count} pools with max {MaxConcurrent} concurrent pools",
            poolIds.Count, maxConcurrentPools);

        var allPoolStakeSnapshots = new List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)>();
        var failedPools = new List<string>();
        var semaphore = new SemaphoreSlim(maxConcurrentPools, maxConcurrentPools);
        var tasks = new List<Task<(string PoolId, PoolStakeSnapshotApiResponse[]? Snapshots, bool Success)>>();

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
        foreach (var (poolId, snapshots, success) in results)
        {
            if (success && snapshots?.Any() == true)
            {
                allPoolStakeSnapshots.Add((poolId, snapshots));
            }
            else if (!success)
            {
                failedPools.Add(poolId);
            }
        }

        _logger.LogInformation("✅ All pools completed. Success: {SuccessCount}, Failed: {FailedCount}",
            allPoolStakeSnapshots.Count, failedPools.Count);

        return (allPoolStakeSnapshots, failedPools);
    }

    private async Task<(string PoolId, PoolStakeSnapshotApiResponse[]? Snapshots, bool Success)>
        ProcessPoolWithSemaphoreAsync(string poolId, SemaphoreSlim semaphore, int delayBetweenPools)
    {
        await semaphore.WaitAsync();

        try
        {
            _logger.LogDebug("📡 Processing pool {PoolId} (Concurrent: {CurrentConcurrent})",
                poolId, semaphore.CurrentCount);

            // Add small delay to avoid overwhelming the API
            await Task.Delay(delayBetweenPools);

            var batchResults = await _databaseSyncService.GetPoolStakeSnapshotAsync(poolId);

            if (batchResults?.Any() == true)
            {
                _logger.LogInformation("✅ Pool {PoolId} returned {Count} snapshots", poolId, batchResults.Length);
                return (poolId, batchResults, true);
            }
            else
            {
                _logger.LogDebug("⚪ Pool {PoolId} has no snapshots", poolId);
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
                ServiceName = "PoolStakeSnapshotSyncJob"
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
    private async Task<(List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)> Results, List<string> FailedPools)>
        ProcessPoolsSequential(List<string> poolIds)
    {
        // Process in batches using Gateway Adapter
        const int batchSize = 500; // Reduced from 80 to 50 to work within 60s Koios timeout
        var totalBatches = (int)Math.Ceiling((double)poolIds.Count / batchSize);
        var allPoolStakeSnapshots = new List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)>();
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
                    // Use Gateway Adapter for optimized processing
                    var batchResults = await _databaseSyncService.GetPoolStakeSnapshotAsync(poolId);

                    if (batchResults?.Any() == true)
                    {
                        allPoolStakeSnapshots.Add((poolId, batchResults));
                        _logger.LogInformation("✅ Pool {PoolId} returned {Count} snapshots", poolId, batchResults.Length);
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Pool {PoolId} has no snapshots", poolId);
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
                        ServiceName = "PoolStakeSnapshotSyncJob"
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

        return (allPoolStakeSnapshots, failedPools);
    }

    private async Task BulkRefreshPoolStakeSnapshots(List<(string PoolId, PoolStakeSnapshotApiResponse[] Snapshots)> poolStakeSnapshots)
    {
        var totalRecords = poolStakeSnapshots.Sum(x => x.Snapshots.Length);
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool stake snapshot records", totalRecords);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolStakeSnapshotRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var allSnapshots = new List<(string PoolId, PoolStakeSnapshotApiResponse Snapshot)>();

            // Flatten the snapshots list with their pool IDs
            foreach (var (poolId, snapshots) in poolStakeSnapshots)
            {
                foreach (var snapshot in snapshots)
                {
                    if (!string.IsNullOrWhiteSpace(snapshot.snapshot))
                    {
                        allSnapshots.Add((poolId, snapshot));
                    }
                }
            }

            var totalBatches = (int)Math.Ceiling((double)allSnapshots.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allSnapshots.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", allSnapshots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string PoolId, PoolStakeSnapshotApiResponse Snapshot)> batch, int batchNumber, int totalBatches)
    {
        // Create a new DbContext scope for each batch to avoid connection pool issues
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

        using var command = context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        try
        {
            var valueParts = new List<string>();
            var paramIndex = 0;

            foreach (var (poolId, record) in batch)
            {
                if (string.IsNullOrWhiteSpace(record.snapshot))
                    continue;

                // Create parameters for this record
                var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", poolId);
                var snapshotParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.snapshot);
                var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no);
                var nonceParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.nonce);
                var poolStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_stake);
                var activeStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_stake);

                valueParts.Add(
                    $"(@{poolIdParam.ParameterName}, @{snapshotParam.ParameterName}, @{epochNoParam.ParameterName}, @{nonceParam.ParameterName}, @{poolStakeParam.ParameterName}, @{activeStakeParam.ParameterName})"
                );
            }

            if (valueParts.Count == 0)
                return;

            command.CommandText = $@"INSERT INTO md_pool_stake_snapshot
                (pool_id_bech32, snapshot, epoch_no, nonce, pool_stake, active_stake)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32, epoch_no) DO UPDATE SET 
                  snapshot = EXCLUDED.snapshot,
                  nonce = EXCLUDED.nonce,
                  pool_stake = EXCLUDED.pool_stake,
                  active_stake = EXCLUDED.active_stake";

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

    private async Task DeleteAllPoolStakeSnapshotRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool stake snapshot records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_stake_snapshot");
            _logger.LogInformation("✅ Successfully deleted all existing pool stake snapshot records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}