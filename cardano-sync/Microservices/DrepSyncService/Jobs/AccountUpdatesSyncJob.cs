using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using SharedLibrary.Models;
using System.Data;

namespace DrepSyncService.Jobs;


[DisallowConcurrentExecution]

public class AccountUpdatesSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<AccountUpdatesSyncJob> _logger;
    private readonly ISchedulerFactory _schedulerFactory;

    // Maximum stake addresses per batch for database sync processing
    private const int MaxStakeAddressesPerRequest = 50; // Reduced from 80 to 50 for optimal performance

    public AccountUpdatesSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<AccountUpdatesSyncJob> logger,
        ISchedulerFactory schedulerFactory)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting AccountUpdatesSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get all unique stake addresses with their drep_ids from md_drep_delegators table
            var stakeAddressesWithDrepsRaw = await _context.MDDrepsDelegators
                .AsNoTracking()
                .Where(d => !string.IsNullOrEmpty(d.stake_address)).ToListAsync();


            var stakeAddressesWithDreps = stakeAddressesWithDrepsRaw.GroupBy(d => d.stake_address)
                .Select(g => new StakeAddressDrepPair
                {
                    stake_address = g.Key,
                    drep_id = g.First().drep_id
                })
                .ToList();

            if (!stakeAddressesWithDreps.Any())
            {
                _logger.LogInformation("No stake addresses found in md_drep_delegators table");
                await ScheduleRetryAfter4Hours(context);
                return;
            }

            _logger.LogInformation("Found {Count} unique stake addresses to process", stakeAddressesWithDreps.Count);

            // Process in batches using Database Sync Service
            const int batchSize = 500; // Reduced from 80 to 50 for optimal performance
            var totalBatches = (int)Math.Ceiling((double)stakeAddressesWithDreps.Count / batchSize);
            var allAccountUpdates = new List<MDAccountUpdates>();
            var failedBatches = new List<(List<StakeAddressDrepPair> batch, int batchNumber)>();

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = stakeAddressesWithDreps.Skip(i * batchSize).Take(batchSize).ToList();
                var stakeAddresses = batch.Select(x => x.stake_address).Where(addr => !string.IsNullOrEmpty(addr)).Cast<string>().ToArray();

                _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} stake addresses",
                    i + 1, totalBatches, stakeAddresses.Length);

                try
                {
                    // Use Database Sync Service for optimized processing
                    var batchResults = await _databaseSyncService.GetAccountUpdatesAsync(stakeAddresses);

                    if (batchResults?.Any() == true)
                    {
                        var flattenedUpdates = FlattenAccountUpdates(batchResults, batch);
                        allAccountUpdates.AddRange(flattenedUpdates);
                        _logger.LogInformation("✅ Batch {BatchNumber} returned {Count} account records with {UpdateCount} total updates",
                            i + 1, batchResults.Length, flattenedUpdates.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Batch {BatchNumber} returned no data", i + 1);
                    }

                    // Reduced delay since Database Sync Service handles rate limiting
                    if (i < totalBatches - 1)
                    {
                        await Task.Delay(50); // 50ms delay between batches
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing batch {BatchNumber}: {Message}", i + 1, ex.Message);
                    // Add failed batch to retry list instead of failing the entire job
                    failedBatches.Add((batch, i + 1));
                }
            }

            // Retry failed batches
            if (failedBatches.Any())
            {
                _logger.LogWarning("🔄 Retrying {Count} failed batches...", failedBatches.Count);
                await RetryFailedBatches(failedBatches, allAccountUpdates);
            }

            var duration = DateTime.UtcNow - startTime;

            if (allAccountUpdates.Any())
            {
                await BulkRefreshAccountUpdates(allAccountUpdates);

                _logger.LogInformation("🎯 AccountUpdatesSyncJob completed successfully. Processed {Count} records in {Duration}",
                    allAccountUpdates.Count, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Database Sync Service Stats: {Stats}", "Completed successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ No account updates data retrieved from API, but not scheduling retry as we have drep delegator data");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in AccountUpdatesSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task RetryFailedBatches(List<(List<StakeAddressDrepPair> batch, int batchNumber)> failedBatches, List<MDAccountUpdates> allAccountUpdates)
    {
        const int maxRetries = 2; // Maximum retry attempts per batch
        const int retryDelay = 5000; // 5 seconds delay between retries

        foreach (var (batch, originalBatchNumber) in failedBatches)
        {
            var stakeAddresses = batch.Select(x => x.stake_address).Where(addr => !string.IsNullOrEmpty(addr)).Cast<string>().ToArray();
            var retrySuccess = false;

            for (int attempt = 1; attempt <= maxRetries && !retrySuccess; attempt++)
            {
                try
                {
                    _logger.LogInformation("🔄 Retry attempt {Attempt}/{MaxRetries} for batch {BatchNumber} with {Count} stake addresses",
                        attempt, maxRetries, originalBatchNumber, stakeAddresses.Length);

                    await Task.Delay(retryDelay * attempt); // Exponential backoff

                    var batchResults = await _databaseSyncService.GetAccountUpdatesAsync(stakeAddresses);

                    if (batchResults?.Any() == true)
                    {
                        var flattenedUpdates = FlattenAccountUpdates(batchResults, batch);
                        allAccountUpdates.AddRange(flattenedUpdates);
                        _logger.LogInformation("✅ Retry successful for batch {BatchNumber}: {Count} account records with {UpdateCount} total updates",
                            originalBatchNumber, batchResults.Length, flattenedUpdates.Count);
                        retrySuccess = true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Retry attempt {Attempt} for batch {BatchNumber} returned no data", attempt, originalBatchNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Retry attempt {Attempt}/{MaxRetries} failed for batch {BatchNumber}: {Message}",
                        attempt, maxRetries, originalBatchNumber, ex.Message);

                    if (attempt == maxRetries)
                    {
                        _logger.LogError("❌ All retry attempts failed for batch {BatchNumber}, skipping this batch", originalBatchNumber);
                    }
                }
            }
        }
    }

    private async Task ScheduleRetryAfter4Hours(IJobExecutionContext context)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // Create a unique trigger name to avoid conflicts
            var triggerName = $"AccountUpdatesSyncJob-retry-{DateTime.UtcNow.Ticks}";
            var retryTime = DateTimeOffset.UtcNow.AddHours(4);

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerName)
                .ForJob("AccountUpdatesSyncJob")
                .StartAt(retryTime)
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await scheduler.ScheduleJob(trigger);

            _logger.LogInformation("⏰ Scheduled retry in 4 hours at {RetryTime}", retryTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to schedule retry: {Message}", ex.Message);
        }
    }

    private class StakeAddressDrepPair
    {
        public string? stake_address { get; set; }
        public string? drep_id { get; set; }
    }

    private List<MDAccountUpdates> FlattenAccountUpdates(AccountUpdatesApiResponse[] apiResponses, List<StakeAddressDrepPair> batch)
    {
        var result = new List<MDAccountUpdates>();

        // Check for duplicate stake addresses and log them for debugging
        var duplicateGroups = batch
            .Where(x => !string.IsNullOrEmpty(x.stake_address))
            .GroupBy(x => x.stake_address)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            _logger.LogWarning("⚠️ Found {Count} duplicate stake addresses in batch:", duplicateGroups.Count);
            foreach (var group in duplicateGroups.Take(5)) // Log first 5 for debugging
            {
                var drepIds = string.Join(", ", group.Select(x => x.drep_id));
                _logger.LogWarning("  - {StakeAddress} appears {Count} times with drep_ids: [{DrepIds}]",
                    group.Key, group.Count(), drepIds);
            }
        }

        // Create a lookup for drep_id by stake_address, handling duplicates by taking the first drep_id
        var drepLookup = batch
            .Where(x => !string.IsNullOrEmpty(x.stake_address))
            .GroupBy(x => x.stake_address)
            .ToDictionary(
                g => g.Key!,
                g => g.First().drep_id
            );

        foreach (var response in apiResponses)
        {
            if (response.updates?.Any() == true)
            {
                foreach (var update in response.updates)
                {
                    drepLookup.TryGetValue(response.stake_address ?? "", out var drepId);

                    result.Add(new MDAccountUpdates
                    {
                        stake_address = response.stake_address,
                        tx_hash = update.tx_hash,
                        epoch_no = update.epoch_no,
                        block_time = update.block_time,
                        epoch_slot = update.epoch_slot,
                        action_type = update.action_type,
                        absolute_slot = update.absolute_slot,
                        drep_id = drepId
                    });
                }
            }
        }

        return result;
    }

    private async Task BulkRefreshAccountUpdates(List<MDAccountUpdates> accountUpdates)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} account update records", accountUpdates.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllAccountUpdateRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)accountUpdates.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = accountUpdates.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", accountUpdates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<MDAccountUpdates> batch, int batchNumber, int totalBatches)
    {
        // Deduplicate within the batch to avoid 21000 (same key twice in one INSERT)
        var uniqueRecords = batch
            .Where(r => !string.IsNullOrEmpty(r.stake_address) && !string.IsNullOrEmpty(r.tx_hash))
            .GroupBy(r => new { r.stake_address, r.tx_hash })
            .Select(g => g.OrderByDescending(r => r.block_time).First())
            .ToArray();

        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in uniqueRecords)
        {
            if (string.IsNullOrEmpty(record.stake_address))
                continue;

            // Create parameters for this record
            var stakeAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.stake_address);
            var txHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.tx_hash);
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no);
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_time);
            var epochSlotParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_slot);
            var actionTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.action_type);
            var absoluteSlotParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.absolute_slot);
            var drepIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_id);

            valueParts.Add(
                $"(@{stakeAddressParam.ParameterName}, @{txHashParam.ParameterName}, @{epochNoParam.ParameterName}, @{blockTimeParam.ParameterName}, @{epochSlotParam.ParameterName}, @{actionTypeParam.ParameterName}, @{absoluteSlotParam.ParameterName}, @{drepIdParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_account_updates
            (stake_address, tx_hash, epoch_no, block_time, epoch_slot, action_type, absolute_slot, drep_id)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (stake_address, tx_hash) DO UPDATE SET
                epoch_no = EXCLUDED.epoch_no,
                block_time = EXCLUDED.block_time,
                epoch_slot = EXCLUDED.epoch_slot,
                action_type = EXCLUDED.action_type,
                absolute_slot = EXCLUDED.absolute_slot,
                drep_id = EXCLUDED.drep_id
            WHERE EXCLUDED.block_time > md_account_updates.block_time";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records, deduplicated from {OriginalCount})",
            batchNumber, totalBatches, valueParts.Count, batch.Count);
    }

    private async Task DeleteAllAccountUpdateRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing account update records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_account_updates");
            _logger.LogInformation("✅ Successfully deleted all existing account update records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}