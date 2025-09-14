using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;

[DisallowConcurrentExecution]
public class UtxoInfoSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<UtxoInfoSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FailedBatchRetryService _failedBatchRetryService;

    public UtxoInfoSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<UtxoInfoSyncJob> logger,
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
        _logger.LogInformation("🚀 Starting UtxoInfoSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;
            // Step 1: Get unique delegation tx hashes from MDPoolDelegators
            var delegationTxHashes = await GetUniqueDelegationTxHashesAsync();

            if (!delegationTxHashes.Any())
            {
                _logger.LogWarning("⚠️ No delegation tx hashes found in MDPoolDelegators. Rescheduling job to run again in 3 hours.");

                // Reschedule the job to run again in 3 hours
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"UtxoInfoSyncJob-Retry-{DateTime.UtcNow:yyyyMMdd-HHmmss}")
                    .StartAt(DateTime.UtcNow.AddHours(3))
                    .Build();

                var jobDetail = JobBuilder.Create<UtxoInfoSyncJob>()
                    .WithIdentity($"UtxoInfoSyncJob-Retry-{DateTime.UtcNow:yyyyMMdd-HHmmss}")
                    .Build();

                await context.Scheduler.ScheduleJob(jobDetail, trigger);

                _logger.LogInformation("📅 Job rescheduled to run at {ScheduledTime}", DateTime.UtcNow.AddHours(7));
                return;
            }

            _logger.LogInformation("Found {Count} unique delegation tx hashes to process", delegationTxHashes.Count);

            // Step 2: Process in batches - choose between sequential or parallel processing
            var useParallelProcessing = true; // Can be moved to configuration
            if (useParallelProcessing)
            {
                await ProcessUtxoInfoInBatchesParallel(delegationTxHashes);
            }
            else
            {
                await ProcessUtxoInfoInBatches(delegationTxHashes);
            }
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("🎯 UtxoInfoSyncJob completed successfully. Processed {Count} records in {Duration}",
                 delegationTxHashes.Count, duration);

            // Wait for failed batches to be processed
            await _failedBatchRetryService.WaitForCompletionAsync();

            _logger.LogInformation("🎯 UtxoInfoSyncJob completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in UtxoInfoSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<List<string>> GetUniqueDelegationTxHashesAsync()
    {
        _logger.LogInformation("🔍 Fetching unique delegation tx hashes from MDPoolDelegators...");

        try
        {
            var txHashes = await _context.MDPoolDelegators
                    .AsNoTracking()
                    .Where(d => !string.IsNullOrEmpty(d.latest_delegation_tx_hash))
                    .Select(d => d.latest_delegation_tx_hash + "#0")
                    .Distinct()
                    .ToListAsync();

            _logger.LogInformation("✅ Found {Count} unique delegation tx hashes", txHashes.Count);
            return txHashes!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching delegation tx hashes: {Message}", ex.Message);
            throw;
        }
    }

    private async Task ProcessUtxoInfoInBatchesParallel(List<string> utxoRefs)
    {
        const int batchSize = 200; // Batch size for API calls
        const int maxConcurrentBatches = 3; // Reduced to avoid exhausting DB connections
        const int delayBetweenBatches = 2000; // 2 seconds between batches

        var totalBatches = (int)Math.Ceiling((double)utxoRefs.Count / batchSize);
        var semaphore = new SemaphoreSlim(maxConcurrentBatches, maxConcurrentBatches);

        _logger.LogInformation("🔄 Processing {Count} UTXO refs in {Batches} batches with max {MaxConcurrent} concurrent batches",
            utxoRefs.Count, totalBatches, maxConcurrentBatches);

        var tasks = new List<Task<UtxoInfoApiResponse[]?>>();
        var allUtxoData = new List<UtxoInfoApiResponse>();

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = utxoRefs.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            var batchNumber = batchIndex + 1;

            var task = ProcessBatchWithSemaphoreAsync(batch, batchNumber, totalBatches, semaphore, delayBetweenBatches);
            tasks.Add(task);
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Collect all successful results
        foreach (var result in results)
        {
            if (result?.Any() == true)
            {
                allUtxoData.AddRange(result);
            }
        }

        // Ensure all reads are completed before starting writes
        if (allUtxoData.Any())
        {
            await BulkRefreshUtxoInfo(allUtxoData.ToArray());
        }

        semaphore.Dispose();
    }

    private async Task<UtxoInfoApiResponse[]?> ProcessBatchWithSemaphoreAsync(
        List<string> batch,
        int batchNumber,
        int totalBatches,
        SemaphoreSlim semaphore,
        int delayBetweenBatches)
    {
        await semaphore.WaitAsync();

        try
        {
            _logger.LogInformation("📡 Processing batch {BatchNumber}/{TotalBatches} with {Count} UTXO refs (Concurrent: {CurrentConcurrent})",
                batchNumber, totalBatches, batch.Count, totalBatches - semaphore.CurrentCount);

            // Add small delay to avoid overwhelming the API
            await Task.Delay(delayBetweenBatches);

            var batchUtxoData = await _databaseSyncService.GetUtxoInfoAsync(batch);

            if (batchUtxoData?.Any() == true)
            {
                _logger.LogInformation("✅ Batch {BatchNumber} returned {Count} UTXO records",
                    batchNumber, batchUtxoData.Length);
                return batchUtxoData;
            }
            else
            {
                _logger.LogWarning("⚠️ Batch {BatchNumber} returned no data", batchNumber);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing batch {BatchNumber}: {Message}", batchNumber, ex.Message);

            // Add failed batch to retry service
            var failedBatch = new FailedBatchRetryService.FailedBatch
            {
                UtxoRefs = batch,
                BatchNumber = batchNumber - 1, // Convert to 0-based index
                TotalBatches = totalBatches,
                RetryCount = 0,
                FirstFailureTime = DateTime.UtcNow,
                LastRetryTime = DateTime.UtcNow,
                FailureReason = ex.Message,
                LastException = ex,
                ServiceName = "PoolSyncService"
            };

            _failedBatchRetryService.AddFailedBatch(failedBatch);

            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Keep the original sequential method as fallback
    private async Task ProcessUtxoInfoInBatches(List<string> utxoRefs)
    {
        const int batchSize = 200; // Reduced batch size for faster inserts
        var totalBatches = (int)Math.Ceiling((double)utxoRefs.Count / batchSize);

        _logger.LogInformation("🔄 Processing {Count} UTXO refs in {Batches} batches", utxoRefs.Count, totalBatches);

        var allUtxoData = new List<UtxoInfoApiResponse>();

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = utxoRefs.Skip(batchIndex * batchSize).Take(batchSize).ToList();

            _logger.LogInformation("📡 Processing batch {BatchNumber}/{TotalBatches} with {Count} UTXO refs",
                batchIndex + 1, totalBatches, batch.Count);

            try
            {
                var batchUtxoData = await _databaseSyncService.GetUtxoInfoAsync(batch);

                if (batchUtxoData?.Any() == true)
                {
                    allUtxoData.AddRange(batchUtxoData);
                    _logger.LogInformation("✅ Batch {BatchNumber} returned {Count} UTXO records",
                        batchIndex + 1, batchUtxoData.Length);
                }
                else
                {
                    _logger.LogWarning("⚠️ Batch {BatchNumber} returned no data", batchIndex + 1);
                }

                // Add delay between batches to avoid overwhelming the API
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(100); // 100ms delay between batches
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing batch {BatchNumber}: {Message}", batchIndex + 1, ex.Message);

                // Add failed batch to retry service
                var failedBatch = new FailedBatchRetryService.FailedBatch
                {
                    UtxoRefs = batch,
                    BatchNumber = batchIndex,
                    TotalBatches = totalBatches,
                    RetryCount = 0,
                    FirstFailureTime = DateTime.UtcNow,
                    LastRetryTime = DateTime.UtcNow,
                    FailureReason = ex.Message,
                    LastException = ex,
                    ServiceName = "PoolSyncService"
                };

                _failedBatchRetryService.AddFailedBatch(failedBatch);

                // Continue with next batch rather than failing the entire job
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(3000); // 3 second delay after error
                }
            }
        }

        if (allUtxoData.Any())
        {
            await BulkRefreshUtxoInfo(allUtxoData.ToArray());
        }
    }

    private async Task BulkRefreshUtxoInfo(UtxoInfoApiResponse[] utxoData)
    {
        _logger.LogInformation("🔄 Starting bulk refresh of {Count} UTXO info records", utxoData.Length);

        try
        {
            // Step 1: Delete all existing UTXO info records
            await DeleteAllUtxoInfoRecords();

            // Step 2: Insert new records in batches
            const int insertBatchSize = 500; // Aligned with Snapshot
            var totalBatches = (int)Math.Ceiling((double)utxoData.Length / insertBatchSize);

            _logger.LogInformation("📦 Inserting {Count} UTXO info records in {Batches} batches", utxoData.Length, totalBatches);

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = utxoData.Skip(i * insertBatchSize).Take(insertBatchSize).ToArray();
                await InsertBatchWithRawSql(batch, i + 1, totalBatches);
            }

            _logger.LogInformation("✅ Successfully refreshed {Count} UTXO info records", utxoData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during bulk refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(UtxoInfoApiResponse[] batch, int batchNumber, int totalBatches)
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

            foreach (var utxo in batch)
            {
                if (string.IsNullOrEmpty(utxo.tx_hash))
                    continue;

                // Create parameters for this record
                var txHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.tx_hash);
                var txIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.tx_index);
                var stakeAddressParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.stake_address);

                var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.epoch_no);
                var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.block_time);

                valueParts.Add($@"(@{txHashParam.ParameterName}, @{txIndexParam.ParameterName},  @{stakeAddressParam.ParameterName},
                    @{epochNoParam.ParameterName},  @{blockTimeParam.ParameterName})");
            }

            if (valueParts.Count == 0)
                return;

            var cmd = $@"INSERT INTO md_utxo_info 
                (tx_hash, tx_index, stake_address,  
                 epoch_no, block_time)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (tx_hash, tx_index) DO UPDATE SET 
                  stake_address = EXCLUDED.stake_address,
                  epoch_no = EXCLUDED.epoch_no,
                  block_time = EXCLUDED.block_time";
            command.CommandText = cmd;

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

    private async Task DeleteAllUtxoInfoRecords()
    {
        try
        {
            _logger.LogInformation("🗑️ Deleting all existing UTXO info records...");
            var deletedCount = await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_utxo_info");
            _logger.LogInformation("✅ Deleted {Count} existing UTXO info records", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting UTXO info records: {Message}", ex.Message);
            throw;
        }
    }
}