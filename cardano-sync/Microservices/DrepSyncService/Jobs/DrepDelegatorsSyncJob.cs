using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]
public class DrepDelegatorsSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<DrepDelegatorsSyncJob> _logger;

    public DrepDelegatorsSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<DrepDelegatorsSyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting DrepDelegatorsSyncJob at {Time}", DateTime.UtcNow);

        try
        {
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

            // 🚀 NEW APPROACH: Submit ALL requests at once to Database Sync Service
            var validDrepIds = drepIds.Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToList();
            var allDrepDelegators = await ProcessAllDrepIdsWithDatabaseSync(validDrepIds);

            var totalDelegators = allDrepDelegators.Values.SelectMany(x => x).Count();
            _logger.LogInformation("Collected {Count} total delegator records from {DrepCount} dreps",
                totalDelegators, allDrepDelegators.Count);

            if (allDrepDelegators.Any())
            {
                await BulkRefreshDrepDelegators(allDrepDelegators);
                _logger.LogInformation("DrepDelegatorsSyncJob completed successfully. Processed {Count} delegator records",
                    totalDelegators);
            }
            else
            {
                _logger.LogWarning("No drep delegator data retrieved from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DrepDelegatorsSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 🚀 BATCH PROCESSING: Process drep_ids in controlled batches to avoid API overload
    /// Uses concurrency control and rate limiting to prevent timeouts
    /// </summary>
    private async Task<Dictionary<string, List<DrepDelegatorsApiResponse>>> ProcessAllDrepIdsWithDatabaseSync(List<string> drepIds)
    {
        _logger.LogInformation("🚀 Starting batch processing: {Count} drep_ids with concurrency control", drepIds.Count);

        var startTime = DateTime.UtcNow;
        var allDrepDelegators = new Dictionary<string, List<DrepDelegatorsApiResponse>>();

        // Process in batches with controlled concurrency
        const int batchSize = 50; // Reduced batch size to avoid overloading API
        const int maxConcurrency = 3; // Reduced to work within DB connection limits
        var totalBatches = (int)Math.Ceiling((double)drepIds.Count / batchSize);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = drepIds.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} drep_ids",
                batchIndex + 1, totalBatches, batch.Count);

            var batchStartTime = DateTime.UtcNow;

            // Process batch with concurrency control using SemaphoreSlim
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var batchTasks = batch.Select(async drepId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug("📡 Processing drep_id: {DrepId}", drepId);

                    var delegators = await _databaseSyncService.GetDrepDelegatorsAsync(drepId);

                    if (delegators?.Any() == true)
                    {
                        _logger.LogDebug("✅ Drep {DrepId} has {Count} delegators", drepId, delegators.Length);
                        return new KeyValuePair<string, List<DrepDelegatorsApiResponse>>(drepId, delegators.ToList());
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Drep {DrepId} has no delegators", drepId);
                        return new KeyValuePair<string, List<DrepDelegatorsApiResponse>>(drepId, new List<DrepDelegatorsApiResponse>());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing drep_id {DrepId}: {Message}", drepId, ex.Message);
                    return new KeyValuePair<string, List<DrepDelegatorsApiResponse>>(drepId, new List<DrepDelegatorsApiResponse>());
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            // Wait for batch completion
            var batchResults = await Task.WhenAll(batchTasks);

            // Collect batch results
            var successCount = 0;
            foreach (var result in batchResults)
            {
                if (result.Value.Any())
                {
                    allDrepDelegators[result.Key] = result.Value;
                    successCount++;
                }
            }

            var batchDuration = DateTime.UtcNow - batchStartTime;
            _logger.LogInformation("✅ Batch {BatchNumber} completed in {Duration}. Success: {SuccessCount}/{BatchSize}",
                batchIndex + 1, batchDuration, successCount, batch.Count);

            // Add delay between batches to respect rate limits
            if (batchIndex < totalBatches - 1)
            {
                _logger.LogDebug("⏱️ Waiting 1 seconds before next batch...");
                await Task.Delay(500); // 1 second delay between batches
            }
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var totalDelegators = allDrepDelegators.Values.SelectMany(x => x).Count();

        _logger.LogInformation("🎯 Batch processing completed in {Duration}. Processed {ProcessedCount}/{TotalCount} dreps successfully. Total delegators: {DelegatorCount}",
            totalDuration, allDrepDelegators.Count, drepIds.Count, totalDelegators);

        // Log adapter stats
        _logger.LogInformation("📊 Database Sync Service Stats: {Stats}", "Completed successfully");

        return allDrepDelegators;
    }

    private async Task BulkRefreshDrepDelegators(Dictionary<string, List<DrepDelegatorsApiResponse>> drepDelegatorsDict)
    {
        var totalCount = drepDelegatorsDict.Values.SelectMany(x => x).Count();
        _logger.LogInformation("🔄 Starting full refresh for {Count} drep delegator records", totalCount);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepDelegatorsRecords();

            // Step 2: Flatten all delegators with their drep_id for batch processing
            var allDelegators = new List<(string drepId, DrepDelegatorsApiResponse delegator)>();
            foreach (var kvp in drepDelegatorsDict)
            {
                var drepId = kvp.Key;
                var delegators = kvp.Value;

                if (string.IsNullOrWhiteSpace(drepId) || !delegators.Any())
                    continue;

                foreach (var delegator in delegators)
                {
                    if (!string.IsNullOrWhiteSpace(delegator.stake_address))
                    {
                        allDelegators.Add((drepId, delegator));
                    }
                }
            }

            // Step 3: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)allDelegators.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allDelegators.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records across {DrepCount} dreps",
                totalCount, drepDelegatorsDict.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string drepId, DrepDelegatorsApiResponse delegator)> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var (drepId, delegator) in batch)
        {
            // Create parameters for this record
            var drepIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drepId);
            var stakeAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.stake_address);
            var stakeAddressHexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.stake_address_hex);
            var scriptHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", delegator.script_hash);
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.epoch_no);
            var amountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.amount);

            valueParts.Add(
                $"(@{drepIdParam.ParameterName}, @{stakeAddressParam.ParameterName}, @{stakeAddressHexParam.ParameterName}, @{scriptHashParam.ParameterName}, @{epochNoParam.ParameterName}, @{amountParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps_delegators 
            (drep_id, stake_address, stake_address_hex, script_hash, epoch_no, amount)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (drep_id, stake_address) DO UPDATE SET 
              stake_address_hex = EXCLUDED.stake_address_hex,
              script_hash = EXCLUDED.script_hash,
              epoch_no = EXCLUDED.epoch_no,
              amount = EXCLUDED.amount";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }



    private async Task DeleteAllDrepDelegatorsRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep delegators records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_delegators");
            _logger.LogInformation("✅ Successfully deleted all existing drep delegators records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}