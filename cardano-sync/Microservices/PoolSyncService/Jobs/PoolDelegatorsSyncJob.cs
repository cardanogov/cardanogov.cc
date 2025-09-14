using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolDelegatorsSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolDelegatorsSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FailedBatchRetryService _failedBatchRetryService;

    // Maximum pool_ids per batch for gateway processing
    private const int MaxPoolIdsPerRequest = 50; // Reduced from 80 to 50 to work within 60s Koios timeout

    public PoolDelegatorsSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolDelegatorsSyncJob> logger,
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
        _logger.LogInformation("🚀 Starting PoolDelegatorsSyncJob at {Time}", DateTime.UtcNow);

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
            List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)> allPoolDelegators;
            List<string> failedPools;

            if (useParallelProcessing)
            {
                (allPoolDelegators, failedPools) = await ProcessPoolsParallel(poolIds);
            }
            else
            {
                (allPoolDelegators, failedPools) = await ProcessPoolsSequential(poolIds);
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

                        var batchResults = await _databaseSyncService.GetPoolDelegatorsAsync(poolId);

                        if (batchResults?.Any() == true)
                        {
                            allPoolDelegators.Add((poolId, batchResults));
                            _logger.LogInformation("✅ Retry successful for pool {PoolId}: {Count} delegators", poolId, batchResults.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Retry failed for pool {PoolId}: {Message}", poolId, ex.Message);
                    }
                }
            }

            var duration = DateTime.UtcNow - startTime;
            var totalDelegators = allPoolDelegators.Sum(x => x.Delegators.Length);

            if (allPoolDelegators.Any())
            {
                await BulkRefreshPoolDelegators(allPoolDelegators);

                _logger.LogInformation("🎯 PoolDelegatorsSyncJob completed successfully. Processed {Count} records in {Duration}",
                    totalDelegators, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Database Sync Stats: {Stats}", _databaseSyncService.GetDatabaseStats());
            }
            else
            {
                _logger.LogWarning("⚠️ No pool delegators retrieved from API");
            }

            // Wait for failed batches to be processed
            await _failedBatchRetryService.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolDelegatorsSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<(List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)> Results, List<string> FailedPools)>
        ProcessPoolsParallel(List<string> poolIds)
    {
        // Reduce concurrency to avoid exhausting PgBouncer / Postgres connections
        const int maxConcurrentPools = 4; // Previously 10
        const int delayBetweenPools = 200; // Previously 100

        _logger.LogInformation("🔄 Processing {Count} pools with max {MaxConcurrent} concurrent pools",
            poolIds.Count, maxConcurrentPools);

        var allPoolDelegators = new List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)>();
        var failedPools = new List<string>();
        var semaphore = new SemaphoreSlim(maxConcurrentPools, maxConcurrentPools);
        var tasks = new List<Task<(string PoolId, PoolDelegatorsApiResponse[]? Delegators, bool Success)>>();

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
        foreach (var (poolId, delegators, success) in results)
        {
            if (success && delegators?.Any() == true)
            {
                allPoolDelegators.Add((poolId, delegators));
            }
            else if (!success)
            {
                failedPools.Add(poolId);
            }
        }

        _logger.LogInformation("✅ All pools completed. Success: {SuccessCount}, Failed: {FailedCount}",
            allPoolDelegators.Count, failedPools.Count);

        return (allPoolDelegators, failedPools);
    }

    private async Task<(string PoolId, PoolDelegatorsApiResponse[]? Delegators, bool Success)>
        ProcessPoolWithSemaphoreAsync(string poolId, SemaphoreSlim semaphore, int delayBetweenPools)
    {
        await semaphore.WaitAsync();

        try
        {
            _logger.LogDebug("📡 Processing pool {PoolId} (Concurrent: {CurrentConcurrent})",
                poolId, semaphore.CurrentCount);

            // Add small delay to avoid overwhelming the database
            await Task.Delay(delayBetweenPools);

            var batchResults = await _databaseSyncService.GetPoolDelegatorsAsync(poolId);

            if (batchResults?.Any() == true)
            {
                _logger.LogInformation("✅ Pool {PoolId} returned {Count} delegators", poolId, batchResults.Length);
                return (poolId, batchResults, true);
            }
            else
            {
                _logger.LogDebug("⚪ Pool {PoolId} has no delegators", poolId);
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
                ServiceName = "PoolDelegatorsSyncJob"
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
    private async Task<(List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)> Results, List<string> FailedPools)>
        ProcessPoolsSequential(List<string> poolIds)
    {
        // Process in batches using Gateway Adapter
        const int batchSize = 500; // Reduced from 80 to 50 to work within 60s Koios timeout
        var totalBatches = (int)Math.Ceiling((double)poolIds.Count / batchSize);
        var allPoolDelegators = new List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)>();
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
                    var batchResults = await _databaseSyncService.GetPoolDelegatorsAsync(poolId);

                    if (batchResults?.Any() == true)
                    {
                        allPoolDelegators.Add((poolId, batchResults));
                        _logger.LogInformation("✅ Pool {PoolId} returned {Count} delegators", poolId, batchResults.Length);
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Pool {PoolId} has no delegators", poolId);
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
                        ServiceName = "PoolDelegatorsSyncJob"
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

        return (allPoolDelegators, failedPools);
    }

    private async Task BulkRefreshPoolDelegators(List<(string PoolId, PoolDelegatorsApiResponse[] Delegators)> poolDelegators)
    {
        var totalRecords = poolDelegators.Sum(x => x.Delegators.Length);
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool delegator records", totalRecords);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolDelegatorRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var allDelegators = new List<(string PoolId, PoolDelegatorsApiResponse Delegator)>();

            // Flatten the delegators list with their pool IDs
            foreach (var (poolId, delegators) in poolDelegators)
            {
                foreach (var delegator in delegators)
                {
                    if (!string.IsNullOrWhiteSpace(delegator.stake_address))
                    {
                        allDelegators.Add((poolId, delegator));
                    }
                }
            }

            var totalBatches = (int)Math.Ceiling((double)allDelegators.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allDelegators.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", allDelegators.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string PoolId, PoolDelegatorsApiResponse Delegator)> batch, int batchNumber, int totalBatches)
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
                if (string.IsNullOrWhiteSpace(record.stake_address))
                    continue;

                // Create parameters for this record
                var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", poolId);
                var stakeAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.stake_address);
                var amountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.amount);
                var activeEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_epoch_no);
                var latestTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.latest_delegation_tx_hash);

                valueParts.Add(
                    $"(@{poolIdParam.ParameterName}, @{stakeAddressParam.ParameterName}, @{amountParam.ParameterName}, @{activeEpochParam.ParameterName}, @{latestTxHashParam.ParameterName})"
                );
            }

            if (valueParts.Count == 0)
                return;

            command.CommandText = $@"INSERT INTO md_pool_delegators 
                (pool_id_bech32, stake_address, amount, active_epoch_no, latest_delegation_tx_hash)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32, stake_address) DO UPDATE SET 
                  amount = EXCLUDED.amount,
                  active_epoch_no = EXCLUDED.active_epoch_no,
                  latest_delegation_tx_hash = EXCLUDED.latest_delegation_tx_hash";

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

    private async Task DeleteAllPoolDelegatorRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool delegator records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_delegators");
            _logger.LogInformation("✅ Successfully deleted all existing pool delegator records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}