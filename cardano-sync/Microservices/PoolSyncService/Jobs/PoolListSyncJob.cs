using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolListSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolListSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PoolListSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolListSyncJob> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("🚀 Starting PoolListSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("🔄 Submitting pool list request to Database Sync Service...");

            var poolListData = await _databaseSyncService.GetPoolListAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Database Sync Service returned {Count} pool list records in {Duration}",
                poolListData.Length, duration);

            if (poolListData.Any())
            {
                await BulkRefreshPoolList(poolListData.ToArray());
                _logger.LogInformation("🎯 PoolListSyncJob completed successfully. Processed {TotalCount} pool list records in {ElapsedTime}",
                    poolListData.Length, DateTime.UtcNow - startTime);
            }
            else
            {
                _logger.LogWarning("⚠️ No pool list data received from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolListSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshPoolList(PoolListApiResponse[] poolListData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool list records", poolListData.Length);

        try
        {
            // Step 1: Delete all existing records (throttled)
            await _databaseSyncService.WithDbThrottleAsync(DeleteAllPoolListRecords);

            // Ensure EF commands use the configured command timeout
            _context.Database.SetCommandTimeout(_databaseSyncService.CommandTimeoutSeconds);

            // Step 2: Insert all new records in batches
            var processedCount = 0;
            const int batchSize = 500;

            for (int i = 0; i < poolListData.Length; i += batchSize)
            {
                var batch = poolListData.Skip(i).Take(batchSize).ToArray();
                if (batch.Length > 0)
                {
                    await InsertBatchWithRawSql(batch);
                    processedCount += batch.Length;
                    _logger.LogDebug("💾 Inserted batch {BatchStart}-{BatchEnd} ({Processed}/{Total})",
                        i + 1, Math.Min(i + batchSize, poolListData.Length), processedCount, poolListData.Length);
                }
            }

            _logger.LogInformation("✅ Full refresh completed successfully. Processed {ProcessedCount} pool list records",
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllPoolListRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool list records...");
        try
        {
            // Ensure EF uses longer command timeout for full-table delete
            _context.Database.SetCommandTimeout(_databaseSyncService.CommandTimeoutSeconds);

            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_list");
            _logger.LogInformation("✅ Successfully deleted all existing pool list records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(PoolListApiResponse[] batch)
    {
        // Create a new DbContext scope for each batch to avoid connection pool issues
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CardanoDbContext>();

        using var command = context.Database.GetDbConnection().CreateCommand();



        try
        {
            var uniqueRecords = batch
                .Where(r => !string.IsNullOrEmpty(r.pool_id_bech32))
                .GroupBy(r => r.pool_id_bech32)
                .Select(g => g.OrderByDescending(r => r.active_epoch_no).First())
                .ToArray();

            if (uniqueRecords.Length == 0) return;

            var valueParts = new List<string>();
            var paramIndex = 0;

            foreach (var record in uniqueRecords)
            {
                // Create parameters
                var poolIdBech32Param = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_bech32);
                var poolIdHexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_hex);
                var activeEpochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_epoch_no);
                var marginParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.margin);
                var fixedCostParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.fixed_cost);
                var pledgeParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.pledge);
                var depositParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.deposit);
                var rewardAddrParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.reward_addr);
                var ownersParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.owners);
                var relaysParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.relays);
                var tickerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.ticker);
                var poolGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_group);
                var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
                var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
                var poolStatusParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_status);
                var activeStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_stake);
                var retiringEpochParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.retiring_epoch);

                valueParts.Add($"(@{poolIdBech32Param.ParameterName}, @{poolIdHexParam.ParameterName}, @{activeEpochNoParam.ParameterName}, @{marginParam.ParameterName}, @{fixedCostParam.ParameterName}, @{pledgeParam.ParameterName}, @{depositParam.ParameterName}, @{rewardAddrParam.ParameterName}, @{ownersParam.ParameterName}, @{relaysParam.ParameterName}, @{tickerParam.ParameterName}, @{poolGroupParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{poolStatusParam.ParameterName}, @{activeStakeParam.ParameterName}, @{retiringEpochParam.ParameterName})");
            }

            command.CommandText = $@"INSERT INTO md_pool_list
                (pool_id_bech32, pool_id_hex, active_epoch_no, margin, fixed_cost,
                 pledge, deposit, reward_addr, owners, relays, ticker, pool_group,
                 meta_url, meta_hash, pool_status, active_stake, retiring_epoch)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32) DO UPDATE SET 
                  pool_id_hex = EXCLUDED.pool_id_hex,
                  active_epoch_no = EXCLUDED.active_epoch_no,
                  margin = EXCLUDED.margin,
                  fixed_cost = EXCLUDED.fixed_cost,
                  pledge = EXCLUDED.pledge,
                  deposit = EXCLUDED.deposit,
                  reward_addr = EXCLUDED.reward_addr,
                  owners = EXCLUDED.owners,
                  relays = EXCLUDED.relays,
                  ticker = EXCLUDED.ticker,
                  pool_group = EXCLUDED.pool_group,
                  meta_url = EXCLUDED.meta_url,
                  meta_hash = EXCLUDED.meta_hash,
                  pool_status = EXCLUDED.pool_status,
                  active_stake = EXCLUDED.active_stake,
                  retiring_epoch = EXCLUDED.retiring_epoch";
            // Set explicit command timeout for long-running bulk insert
            command.CommandTimeout = _databaseSyncService.CommandTimeoutSeconds;

            await _databaseSyncService.WithDbThrottleAsync(async () =>
            {
                if (command.Connection?.State != ConnectionState.Open)
                    await command.Connection!.OpenAsync();

                await command.ExecuteNonQueryAsync();

                // Explicitly close the connection to return it to the pool
                command.Connection.Close();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inserting batch: {Message}", ex.Message);

            // Ensure connection is closed even on error
            if (command.Connection?.State == ConnectionState.Open)
                command.Connection.Close();

            throw;
        }
    }


}