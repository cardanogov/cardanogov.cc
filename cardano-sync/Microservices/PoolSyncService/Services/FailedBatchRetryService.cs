using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace PoolSyncService.Services;

public class FailedBatchRetryService : IDisposable
{
    private readonly ILogger<FailedBatchRetryService> _logger;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _retryTimer;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private readonly ConcurrentQueue<FailedBatch> _failedBatchesQueue = new();

    private const int MaxRetries = 7;
    private const int BaseDelayMs = 1000;
    private const int RetryIntervalMinutes = 5;

    public FailedBatchRetryService(
        ILogger<FailedBatchRetryService> logger,
        DatabaseSyncService databaseSyncService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _databaseSyncService = databaseSyncService;
        _serviceProvider = serviceProvider;

        // Initialize retry timer
        _retryTimer = new Timer(async _ => await ProcessFailedBatchesAsync(), null,
            (int)TimeSpan.FromMinutes(RetryIntervalMinutes).TotalMilliseconds,
            (int)TimeSpan.FromMinutes(RetryIntervalMinutes).TotalMilliseconds);
    }

    private void ApplyCommandTimeout(DbCommand command)
    {
        command.CommandTimeout = _databaseSyncService.CommandTimeoutSeconds;
    }


    public class FailedBatch
    {
        public List<string> UtxoRefs { get; set; } = new();
        public int BatchNumber { get; set; }
        public int TotalBatches { get; set; }
        public int RetryCount { get; set; }
        public DateTime FirstFailureTime { get; set; }
        public DateTime LastRetryTime { get; set; }
        public string FailureReason { get; set; } = "";
        public Exception? LastException { get; set; }
        public string ServiceName { get; set; } = "";
    }

    public void AddFailedBatch(FailedBatch failedBatch)
    {
        _failedBatchesQueue.Enqueue(failedBatch);
        _logger.LogInformation("📋 Added failed batch {BatchNumber}/{TotalBatches} to retry queue. Queue size: {QueueSize}",
            failedBatch.BatchNumber + 1, failedBatch.TotalBatches, _failedBatchesQueue.Count);
    }

    public int GetQueueSize()
    {
        return _failedBatchesQueue.Count;
    }

    public bool IsQueueEmpty()
    {
        return _failedBatchesQueue.IsEmpty;
    }

    private async Task ProcessFailedBatchesAsync()
    {
        await _processingSemaphore.WaitAsync();

        try
        {
            var processedCount = 0;
            var successCount = 0;
            var failedCount = 0;

            while (_failedBatchesQueue.TryDequeue(out var failedBatch))
            {
                processedCount++;

                if (failedBatch.RetryCount >= MaxRetries)
                {
                    _logger.LogError(failedBatch.LastException,
                        "❌ Failed batch {BatchNumber}/{TotalBatches} after {Retries} retries. Giving up. Service: {ServiceName}",
                        failedBatch.BatchNumber + 1, failedBatch.TotalBatches, failedBatch.RetryCount, failedBatch.ServiceName);
                    failedCount++;
                    continue;
                }

                failedBatch.RetryCount++;
                failedBatch.LastRetryTime = DateTime.UtcNow;

                // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 64s
                var delay = BaseDelayMs * Math.Pow(2, failedBatch.RetryCount - 1);

                _logger.LogWarning("⚠️ Retrying failed batch {BatchNumber}/{TotalBatches} (Attempt {RetryCount}/{MaxRetries}) in {Delay}ms. Service: {ServiceName}. Reason: {Reason}",
                    failedBatch.BatchNumber + 1, failedBatch.TotalBatches, failedBatch.RetryCount, MaxRetries, delay, failedBatch.ServiceName, failedBatch.FailureReason);

                await Task.Delay(TimeSpan.FromMilliseconds(delay));

                try
                {
                    // Handle different service types
                    if (failedBatch.ServiceName == "PoolDelegatorsSyncJob")
                    {
                        successCount += await ProcessPoolDelegatorsRetryAsync(failedBatch);
                    }
                    else if (failedBatch.ServiceName == "PoolStakeSnapshotSyncJob")
                    {
                        successCount += await ProcessPoolStakeSnapshotRetryAsync(failedBatch);
                    }
                    else
                    {
                        // Default UTXO processing
                        var batchUtxoData = await _databaseSyncService.GetUtxoInfoAsync(failedBatch.UtxoRefs);

                        if (batchUtxoData?.Any() == true)
                        {
                            _logger.LogInformation("✅ Successfully retried batch {BatchNumber}/{TotalBatches} (Attempt {RetryCount}/{MaxRetries}). Service: {ServiceName}",
                                failedBatch.BatchNumber + 1, failedBatch.TotalBatches, failedBatch.RetryCount, MaxRetries, failedBatch.ServiceName);
                            successCount++;

                            // Save the data to database
                            await SaveUtxoDataToDatabaseAsync(batchUtxoData);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Retry returned no data for batch {BatchNumber}/{TotalBatches} (Attempt {RetryCount}/{MaxRetries}). Service: {ServiceName}",
                                failedBatch.BatchNumber + 1, failedBatch.TotalBatches, failedBatch.RetryCount, MaxRetries, failedBatch.ServiceName);

                            // Re-enqueue for another retry
                            _failedBatchesQueue.Enqueue(failedBatch);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedBatch.LastException = ex;
                    failedBatch.FailureReason = ex.Message;

                    _logger.LogError(ex, "❌ Retry failed for batch {BatchNumber}/{TotalBatches} (Attempt {RetryCount}/{MaxRetries}). Service: {ServiceName}",
                        failedBatch.BatchNumber + 1, failedBatch.TotalBatches, failedBatch.RetryCount, MaxRetries, failedBatch.ServiceName);

                    // Re-enqueue for another retry
                    _failedBatchesQueue.Enqueue(failedBatch);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("📊 Failed batch processing summary: Processed={Processed}, Success={Success}, Failed={Failed}, Remaining={Remaining}",
                    processedCount, successCount, failedCount, _failedBatchesQueue.Count);
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    public async Task WaitForCompletionAsync(int maxWaitTimeMinutes = 30)
    {
        var startTime = DateTime.UtcNow;

        while (!_failedBatchesQueue.IsEmpty && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(maxWaitTimeMinutes))
        {
            _logger.LogInformation("⏳ Waiting for failed batches to be processed. Queue size: {QueueSize}", _failedBatchesQueue.Count);
            await Task.Delay(TimeSpan.FromMinutes(1));
        }

        if (!_failedBatchesQueue.IsEmpty)
        {
            _logger.LogWarning("⚠️ Failed batches queue still has {QueueSize} items after {MaxWaitTime} minutes. Some data may be incomplete.",
                _failedBatchesQueue.Count, maxWaitTimeMinutes);
        }
        else
        {
            _logger.LogInformation("✅ All failed batches have been processed successfully.");
        }
    }

    private async Task SaveUtxoDataToDatabaseAsync(UtxoInfoApiResponse[] utxoData)
    {
        try
        {
            _logger.LogDebug("💾 Saving {Count} UTXO records to database from failed batch retry", utxoData.Length);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

            using var command = context.Database.GetDbConnection().CreateCommand();

            if (command.Connection?.State != ConnectionState.Open)
                await command.Connection!.OpenAsync();

            try
            {
                var valueParts = new List<string>();
                var paramIndex = 0;

                foreach (var utxo in utxoData)
                {
                    if (string.IsNullOrEmpty(utxo.tx_hash))
                        continue;

                    // Create parameters for this record
                    var txHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.tx_hash);
                    var txIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.tx_index);
                    var addressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.address);
                    var valueParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.value);

                    var stakeAddressParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.stake_address);
                    var paymentCredParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.payment_cred);

                    var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.epoch_no);
                    var blockHeightParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.block_height);
                    var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.block_time);

                    var datumHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.datum_hash);
                    var inlineDatumParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.inline_datum);
                    var referenceScriptParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.reference_script);
                    var assetListParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", utxo.asset_list);

                    var isSpentParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", utxo.is_spent);

                    valueParts.Add($@"(@{txHashParam.ParameterName}, @{txIndexParam.ParameterName}, @{addressParam.ParameterName},
                        @{valueParam.ParameterName}, @{stakeAddressParam.ParameterName}, @{paymentCredParam.ParameterName},
                        @{epochNoParam.ParameterName}, @{blockHeightParam.ParameterName}, @{blockTimeParam.ParameterName},
                        @{datumHashParam.ParameterName}, @{inlineDatumParam.ParameterName}, @{referenceScriptParam.ParameterName},
                        @{assetListParam.ParameterName}, @{isSpentParam.ParameterName})");
                }

                if (valueParts.Count == 0)
                    return;

                command.CommandText = $@"INSERT INTO md_utxo_info
                    (tx_hash, tx_index, address, value, stake_address, payment_cred,
                     epoch_no, block_height, block_time, datum_hash, inline_datum,
                     reference_script, asset_list, is_spent)
                    VALUES {string.Join(", ", valueParts)}
                    ON CONFLICT (tx_hash, tx_index) DO UPDATE SET
                        address = EXCLUDED.address,
                        value = EXCLUDED.value,
                        stake_address = EXCLUDED.stake_address,
                        payment_cred = EXCLUDED.payment_cred,
                        epoch_no = EXCLUDED.epoch_no,
                        block_height = EXCLUDED.block_height,
                        block_time = EXCLUDED.block_time,
                        datum_hash = EXCLUDED.datum_hash,
                        inline_datum = EXCLUDED.inline_datum,
                        reference_script = EXCLUDED.reference_script,
                        asset_list = EXCLUDED.asset_list,
                        is_spent = EXCLUDED.is_spent";

                ApplyCommandTimeout(command);
                await _databaseSyncService.WithDbThrottleAsync(async () =>
                {
                    await command.ExecuteNonQueryAsync();
                });

                // Explicitly close the connection to return it to the pool
                command.Connection.Close();

                _logger.LogDebug("✅ Successfully saved {Count} UTXO records to database from failed batch retry", valueParts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving UTXO data to database from failed batch retry: {Message}", ex.Message);

                // Ensure connection is closed even on error
                if (command.Connection?.State == ConnectionState.Open)
                    command.Connection.Close();

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating database scope for failed batch retry: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<int> ProcessPoolDelegatorsRetryAsync(FailedBatch failedBatch)
    {
        try
        {
            if (failedBatch.UtxoRefs.Count == 0)
            {
                _logger.LogWarning("⚠️ No pool ID found in failed batch for PoolDelegatorsSyncJob");
                _failedBatchesQueue.Enqueue(failedBatch);
                return 0;
            }

            var poolId = failedBatch.UtxoRefs[0]; // First item is the pool ID
            var delegators = await _databaseSyncService.GetPoolDelegatorsAsync(poolId);

            if (delegators?.Any() == true)
            {
                _logger.LogInformation("✅ Successfully retried pool delegators for {PoolId} (Attempt {RetryCount}/{MaxRetries})",
                    poolId, failedBatch.RetryCount, MaxRetries);

                // Save the data to database
                await SavePoolDelegatorsToDatabaseAsync(poolId, delegators);
                return 1;
            }
            else
            {
                _logger.LogWarning("⚠️ Retry returned no delegators for pool {PoolId} (Attempt {RetryCount}/{MaxRetries})",
                    poolId, failedBatch.RetryCount, MaxRetries);

                // Re-enqueue for another retry
                _failedBatchesQueue.Enqueue(failedBatch);
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pool delegators retry for batch {BatchNumber}: {Message}",
                failedBatch.BatchNumber + 1, ex.Message);

            failedBatch.LastException = ex;
            failedBatch.FailureReason = ex.Message;
            _failedBatchesQueue.Enqueue(failedBatch);
            return 0;
        }
    }

    private async Task<int> ProcessPoolStakeSnapshotRetryAsync(FailedBatch failedBatch)
    {
        try
        {
            if (failedBatch.UtxoRefs.Count == 0)
            {
                _logger.LogWarning("⚠️ No pool ID found in failed batch for PoolStakeSnapshotSyncJob");
                _failedBatchesQueue.Enqueue(failedBatch);
                return 0;
            }

            var poolId = failedBatch.UtxoRefs[0]; // First item is the pool ID
            var snapshots = await _databaseSyncService.GetPoolStakeSnapshotAsync(poolId);

            if (snapshots?.Any() == true)
            {
                _logger.LogInformation("✅ Successfully retried pool stake snapshots for {PoolId} (Attempt {RetryCount}/{MaxRetries})",
                    poolId, failedBatch.RetryCount, MaxRetries);

                // Save the data to database
                await SavePoolStakeSnapshotsToDatabaseAsync(poolId, snapshots);
                return 1;
            }
            else
            {
                _logger.LogWarning("⚠️ Retry returned no snapshots for pool {PoolId} (Attempt {RetryCount}/{MaxRetries})",
                    poolId, failedBatch.RetryCount, MaxRetries);

                // Re-enqueue for another retry
                _failedBatchesQueue.Enqueue(failedBatch);
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing pool stake snapshot retry for batch {BatchNumber}: {Message}",
                failedBatch.BatchNumber + 1, ex.Message);

            failedBatch.LastException = ex;
            failedBatch.FailureReason = ex.Message;
            _failedBatchesQueue.Enqueue(failedBatch);
            return 0;
        }
    }

    private async Task SavePoolDelegatorsToDatabaseAsync(string poolId, PoolDelegatorsApiResponse[] delegators)
    {
        try
        {
            _logger.LogDebug("💾 Saving {Count} pool delegators for pool {PoolId} to database", delegators.Length, poolId);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

            using var command = context.Database.GetDbConnection().CreateCommand();

            if (command.Connection?.State != ConnectionState.Open)
                await command.Connection!.OpenAsync();

            try
            {
                var valueParts = new List<string>();
                var paramIndex = 0;

                foreach (var delegator in delegators)
                {
                    if (string.IsNullOrEmpty(delegator.stake_address))
                        continue;

                    // Create parameters for this record
                    var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", poolId);
                    var stakeAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.stake_address);
                    var amountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.amount);
                    var activeEpochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.active_epoch_no);
                    var latestDelegationTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", delegator.latest_delegation_tx_hash);

                    valueParts.Add($@"(@{poolIdParam.ParameterName}, @{stakeAddressParam.ParameterName}, @{amountParam.ParameterName},
                        @{activeEpochNoParam.ParameterName}, @{latestDelegationTxHashParam.ParameterName})");
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

                ApplyCommandTimeout(command);
                await _databaseSyncService.WithDbThrottleAsync(async () =>
                {
                    await command.ExecuteNonQueryAsync();
                });

                // Explicitly close the connection to return it to the pool
                command.Connection.Close();

                _logger.LogDebug("✅ Successfully saved {Count} pool delegators for pool {PoolId} to database", valueParts.Count, poolId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving pool delegators to database for pool {PoolId}: {Message}", poolId, ex.Message);

                // Ensure connection is closed even on error
                if (command.Connection?.State == ConnectionState.Open)
                    command.Connection.Close();

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating database scope for pool delegators retry: {Message}", ex.Message);
            throw;
        }
    }

    private async Task SavePoolStakeSnapshotsToDatabaseAsync(string poolId, PoolStakeSnapshotApiResponse[] snapshots)
    {
        try
        {
            _logger.LogDebug("💾 Saving {Count} pool stake snapshots for pool {PoolId} to database", snapshots.Length, poolId);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

            using var command = context.Database.GetDbConnection().CreateCommand();

            if (command.Connection?.State != ConnectionState.Open)
                await command.Connection!.OpenAsync();

            try
            {
                var valueParts = new List<string>();
                var paramIndex = 0;

                foreach (var snapshot in snapshots)
                {
                    if (string.IsNullOrEmpty(snapshot.epoch_no?.ToString()))
                        continue;

                    // Create parameters for this record
                    var poolIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", poolId);
                    var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", snapshot.epoch_no);
                    var nonceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", snapshot.nonce);
                    var poolStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", snapshot.pool_stake);
                    var activeStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", snapshot.active_stake);

                    valueParts.Add($@"(@{poolIdParam.ParameterName}, @{epochNoParam.ParameterName}, @{nonceParam.ParameterName},
                        @{poolStakeParam.ParameterName}, @{activeStakeParam.ParameterName})");
                }

                if (valueParts.Count == 0)
                    return;

                command.CommandText = $@"INSERT INTO md_pool_stake_snapshot
                    (pool_id_bech32, epoch_no, nonce, pool_stake, active_stake)
                    VALUES {string.Join(", ", valueParts)}
                    ON CONFLICT (pool_id_bech32, epoch_no) DO UPDATE SET
                        nonce = EXCLUDED.nonce,
                        pool_stake = EXCLUDED.pool_stake,
                        active_stake = EXCLUDED.active_stake";

                ApplyCommandTimeout(command);
                await _databaseSyncService.WithDbThrottleAsync(async () =>
                {
                    await command.ExecuteNonQueryAsync();
                });

                // Explicitly close the connection to return it to the pool
                command.Connection.Close();

                _logger.LogDebug("✅ Successfully saved {Count} pool stake snapshots for pool {PoolId} to database", valueParts.Count, poolId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving pool stake snapshots to database for pool {PoolId}: {Message}", poolId, ex.Message);

                // Ensure connection is closed even on error
                if (command.Connection?.State == ConnectionState.Open)
                    command.Connection.Close();

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating database scope for pool stake snapshots retry: {Message}", ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _retryTimer?.Dispose();
        _processingSemaphore?.Dispose();
    }
}