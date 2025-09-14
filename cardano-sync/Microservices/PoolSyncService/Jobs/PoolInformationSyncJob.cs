using Microsoft.EntityFrameworkCore;
using PoolSyncService.ApiResponses;
using PoolSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace PoolSyncService.Jobs;


[DisallowConcurrentExecution]
public class PoolInformationSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<PoolInformationSyncJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Maximum pool_ids per batch for gateway processing
    private const int MaxPoolIdsPerRequest = 50; // Reduced from 80 to 50 to work within 60s Koios timeout

    public PoolInformationSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<PoolInformationSyncJob> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting PoolInformationSyncJob at {Time}", DateTime.UtcNow);

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
            const int batchSize = 200; // Batch size for processing
            var totalBatches = (int)Math.Ceiling((double)poolIds.Count / batchSize);
            var allPoolInformation = new List<PoolInformationApiResponse>();

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = poolIds.Skip(i * batchSize).Take(batchSize).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToArray();
                _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} pool_ids",
                    i + 1, totalBatches, batch.Length);

                try
                {
                    // Use Database Sync Service for processing
                    var batchResults = await _databaseSyncService.GetPoolInformationAsync(batch);

                    if (batchResults?.Any() == true)
                    {
                        allPoolInformation.AddRange(batchResults);
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

            if (allPoolInformation.Any())
            {
                await BulkRefreshPoolInformation(allPoolInformation);

                _logger.LogInformation("🎯 PoolInformationSyncJob completed successfully. Processed {Count} records in {Duration}",
                    allPoolInformation.Count, duration);

                // Log adapter stats
                _logger.LogInformation("📊 Database Sync Stats: {Stats}", _databaseSyncService.GetDatabaseStats());
            }
            else
            {
                _logger.LogWarning("⚠️ No pool information retrieved from API");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in PoolInformationSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshPoolInformation(List<PoolInformationApiResponse> poolInformation)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} pool information records", poolInformation.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllPoolInformationRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)poolInformation.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = poolInformation.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", poolInformation.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<PoolInformationApiResponse> batch, int batchNumber, int totalBatches)
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

            foreach (var record in batch)
            {
                if (string.IsNullOrWhiteSpace(record.pool_id_bech32))
                    continue;

                // Create parameters for this record
                var poolIdBech32Param = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_bech32);
                var poolIdHexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_id_hex);
                var activeEpochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_epoch_no);
                var vrfKeyHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.vrf_key_hash);
                var marginParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.margin);
                var fixedCostParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.fixed_cost);
                var pledgeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pledge);
                var depositParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.deposit);
                var rewardAddrParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.reward_addr);
                var rewardAddrDelegatedDrepParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.reward_addr_delegated_drep);
                var ownersParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.owners);
                var relaysParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.relays);
                var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_url);
                var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_hash);
                var metaJsonParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.meta_json);
                var poolStatusParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_status);
                var retiringEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.retiring_epoch);
                var opCertParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.op_cert);
                var opCertCounterParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.op_cert_counter);
                var activeStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.active_stake);
                var sigmaParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.sigma);
                var blockCountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_count);
                var livePledgeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.live_pledge);
                var liveStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.live_stake);
                var liveDelegatorsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.live_delegators);
                var liveSaturationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.live_saturation);
                var votingPowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.voting_power);

                valueParts.Add(
                    $"(@{poolIdBech32Param.ParameterName}, @{poolIdHexParam.ParameterName}, @{activeEpochNoParam.ParameterName}, @{vrfKeyHashParam.ParameterName}, @{marginParam.ParameterName}, @{fixedCostParam.ParameterName}, @{pledgeParam.ParameterName}, @{depositParam.ParameterName}, @{rewardAddrParam.ParameterName}, @{rewardAddrDelegatedDrepParam.ParameterName}, @{ownersParam.ParameterName}, @{relaysParam.ParameterName}, @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName}, @{metaJsonParam.ParameterName}, @{poolStatusParam.ParameterName}, @{retiringEpochParam.ParameterName}, @{opCertParam.ParameterName}, @{opCertCounterParam.ParameterName}, @{activeStakeParam.ParameterName}, @{sigmaParam.ParameterName}, @{blockCountParam.ParameterName}, @{livePledgeParam.ParameterName}, @{liveStakeParam.ParameterName}, @{liveDelegatorsParam.ParameterName}, @{liveSaturationParam.ParameterName}, @{votingPowerParam.ParameterName})"
                );
            }

            if (valueParts.Count == 0)
                return;

            command.CommandText = $@"INSERT INTO md_pool_information
                (pool_id_bech32, pool_id_hex, active_epoch_no, vrf_key_hash, margin, fixed_cost, pledge, deposit, reward_addr, reward_addr_delegated_drep, owners, relays, meta_url, meta_hash, meta_json, pool_status, retiring_epoch, op_cert, op_cert_counter, active_stake, sigma, block_count, live_pledge, live_stake, live_delegators, live_saturation, voting_power)
                VALUES {string.Join(", ", valueParts)}
                ON CONFLICT (pool_id_bech32) DO UPDATE SET 
                  pool_id_hex = EXCLUDED.pool_id_hex,
                  active_epoch_no = EXCLUDED.active_epoch_no,
                  vrf_key_hash = EXCLUDED.vrf_key_hash,
                  margin = EXCLUDED.margin,
                  fixed_cost = EXCLUDED.fixed_cost,
                  pledge = EXCLUDED.pledge,
                  deposit = EXCLUDED.deposit,
                  reward_addr = EXCLUDED.reward_addr,
                  reward_addr_delegated_drep = EXCLUDED.reward_addr_delegated_drep,
                  owners = EXCLUDED.owners,
                  relays = EXCLUDED.relays,
                  meta_url = EXCLUDED.meta_url,
                  meta_hash = EXCLUDED.meta_hash,
                  meta_json = EXCLUDED.meta_json,
                  pool_status = EXCLUDED.pool_status,
                  retiring_epoch = EXCLUDED.retiring_epoch,
                  op_cert = EXCLUDED.op_cert,
                  op_cert_counter = EXCLUDED.op_cert_counter,
                  active_stake = EXCLUDED.active_stake,
                  sigma = EXCLUDED.sigma,
                  block_count = EXCLUDED.block_count,
                  live_pledge = EXCLUDED.live_pledge,
                  live_stake = EXCLUDED.live_stake,
                  live_delegators = EXCLUDED.live_delegators,
                  live_saturation = EXCLUDED.live_saturation,
                  voting_power = EXCLUDED.voting_power";

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

    private async Task DeleteAllPoolInformationRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing pool information records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_pool_information");
            _logger.LogInformation("✅ Successfully deleted all existing pool information records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}