using EpochSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using SharedLibrary.Models;
using System.Data;
using System.Text.Json;

namespace EpochSyncService.Jobs;


[DisallowConcurrentExecution]
public class AdastatEpochSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly IAdastatApiClient _adastatApiClient;
    private readonly ILogger<AdastatEpochSyncJob> _logger;

    public AdastatEpochSyncJob(
        CardanoDbContext context,
        IAdastatApiClient adastatApiClient,
        ILogger<AdastatEpochSyncJob> logger)
    {
        _context = context;
        _adastatApiClient = adastatApiClient;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting AdastatEpochSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;

            // Get epochs from Adastat API
            var adastatResponse = await _adastatApiClient.GetEpochsAsync(1000);

            if (adastatResponse?.Rows?.Any() != true)
            {
                _logger.LogWarning("⚠️ No epochs data retrieved from Adastat API");
                return;
            }

            _logger.LogInformation("📊 Retrieved {Count} epochs from Adastat API", adastatResponse.Rows.Length);

            // Convert to MDEpochs models
            var epochsToInsert = new List<MDEpochs>();

            foreach (var row in adastatResponse.Rows)
            {
                var epoch = new MDEpochs
                {
                    no = row.No,
                    tx_amount = row.TxAmount,
                    circulating_supply = row.CirculatingSupply,
                    pool = row.Pool,
                    pool_with_block = row.PoolWithBlock,
                    pool_with_stake = row.PoolWithStake,
                    pool_fee = row.PoolFee,
                    reward_amount = row.RewardAmount,
                    stake = row.Stake,
                    delegator = row.Delegator,
                    account = row.Account,
                    account_with_reward = row.AccountWithReward,
                    pool_register = row.PoolRegister,
                    pool_retire = row.PoolRetire,
                    orphaned_reward_amount = row.OrphanedRewardAmount,
                    block_with_tx = row.BlockWithTx,
                    byron = row.Byron,
                    byron_with_amount = row.ByronWithAmount,
                    byron_amount = row.ByronAmount,
                    account_with_amount = row.AccountWithAmount,
                    delegator_with_stake = row.DelegatorWithStake,
                    token = row.Token,
                    token_policy = row.TokenPolicy,
                    token_holder = row.TokenHolder,
                    token_tx = row.TokenTx,
                    out_sum = row.OutSum,
                    fees = row.Fees,
                    tx = row.Tx,
                    block = row.Block,
                    start_time = row.StartTime,
                    end_time = row.EndTime,
                    optimal_pool_count = row.OptimalPoolCount,
                    decentralisation = row.Decentralisation,
                    nonce = row.Nonce,
                    holder_range = row.HolderRange != null ? JsonSerializer.Serialize(row.HolderRange) : null,
                    exchange_rate = row.ExchangeRate
                };

                epochsToInsert.Add(epoch);
            }

            if (epochsToInsert.Any())
            {
                await BulkRefreshEpochs(epochsToInsert);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("🎯 AdastatEpochSyncJob completed successfully. Processed {Count} epochs in {Duration}",
                    epochsToInsert.Count, duration);
            }
            else
            {
                _logger.LogWarning("⚠️ No epochs to insert");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in AdastatEpochSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshEpochs(List<MDEpochs> epochs)
    {
        _logger.LogInformation("🔄 Starting bulk refresh for {Count} epochs", epochs.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllEpochRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)epochs.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = epochs.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Bulk refresh completed successfully for {Count} epochs", epochs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during bulk refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<MDEpochs> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var epoch in batch)
        {
            // Create parameters for this record
            var noParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.no);
            var txAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.tx_amount);
            var circulatingSupplyParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.circulating_supply);
            var poolParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool);
            var poolWithBlockParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool_with_block);
            var poolWithStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool_with_stake);
            var poolFeeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool_fee);
            var rewardAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.reward_amount);
            var stakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.stake);
            var delegatorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.delegator);
            var accountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.account);
            var accountWithRewardParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.account_with_reward);
            var poolRegisterParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool_register);
            var poolRetireParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.pool_retire);
            var orphanedRewardAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.orphaned_reward_amount);
            var blockWithTxParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.block_with_tx);
            var byronParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.byron);
            var byronWithAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.byron_with_amount);
            var byronAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.byron_amount);
            var accountWithAmountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.account_with_amount);
            var delegatorWithStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.delegator_with_stake);
            var tokenParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.token);
            var tokenPolicyParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.token_policy);
            var tokenHolderParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.token_holder);
            var tokenTxParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.token_tx);
            var outSumParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.out_sum);
            var feesParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.fees);
            var txParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.tx);
            var blockParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.block);
            var startTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.start_time);
            var endTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.end_time);
            var optimalPoolCountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.optimal_pool_count);
            var decentralisationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.decentralisation);
            var nonceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.nonce);
            var holderRangeParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", epoch.holder_range);
            var exchangeRateParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", epoch.exchange_rate);

            valueParts.Add($@"(@{noParam.ParameterName}, @{txAmountParam.ParameterName}, @{circulatingSupplyParam.ParameterName}, 
                @{poolParam.ParameterName}, @{poolWithBlockParam.ParameterName}, @{poolWithStakeParam.ParameterName}, 
                @{poolFeeParam.ParameterName}, @{rewardAmountParam.ParameterName}, @{stakeParam.ParameterName}, 
                @{delegatorParam.ParameterName}, @{accountParam.ParameterName}, @{accountWithRewardParam.ParameterName}, 
                @{poolRegisterParam.ParameterName}, @{poolRetireParam.ParameterName}, @{orphanedRewardAmountParam.ParameterName}, 
                @{blockWithTxParam.ParameterName}, @{byronParam.ParameterName}, @{byronWithAmountParam.ParameterName}, 
                @{byronAmountParam.ParameterName}, @{accountWithAmountParam.ParameterName}, @{delegatorWithStakeParam.ParameterName}, 
                @{tokenParam.ParameterName}, @{tokenPolicyParam.ParameterName}, @{tokenHolderParam.ParameterName}, 
                @{tokenTxParam.ParameterName}, @{outSumParam.ParameterName}, @{feesParam.ParameterName}, 
                @{txParam.ParameterName}, @{blockParam.ParameterName}, @{startTimeParam.ParameterName}, 
                @{endTimeParam.ParameterName}, @{optimalPoolCountParam.ParameterName}, @{decentralisationParam.ParameterName}, 
                @{nonceParam.ParameterName}, @{holderRangeParam.ParameterName}, @{exchangeRateParam.ParameterName})");
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_epochs 
            (no, tx_amount, circulating_supply, pool, pool_with_block, pool_with_stake, 
             pool_fee, reward_amount, stake, delegator, account, account_with_reward, 
             pool_register, pool_retire, orphaned_reward_amount, block_with_tx, byron, 
             byron_with_amount, byron_amount, account_with_amount, delegator_with_stake, 
             token, token_policy, token_holder, token_tx, out_sum, fees, tx, block, 
             start_time, end_time, optimal_pool_count, decentralisation, nonce, 
             holder_range, exchange_rate)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (no) DO UPDATE SET 
              tx_amount = EXCLUDED.tx_amount,
              circulating_supply = EXCLUDED.circulating_supply,
              pool = EXCLUDED.pool,
              pool_with_block = EXCLUDED.pool_with_block,
              pool_with_stake = EXCLUDED.pool_with_stake,
              pool_fee = EXCLUDED.pool_fee,
              reward_amount = EXCLUDED.reward_amount,
              stake = EXCLUDED.stake,
              delegator = EXCLUDED.delegator,
              account = EXCLUDED.account,
              account_with_reward = EXCLUDED.account_with_reward,
              pool_register = EXCLUDED.pool_register,
              pool_retire = EXCLUDED.pool_retire,
              orphaned_reward_amount = EXCLUDED.orphaned_reward_amount,
              block_with_tx = EXCLUDED.block_with_tx,
              byron = EXCLUDED.byron,
              byron_with_amount = EXCLUDED.byron_with_amount,
              byron_amount = EXCLUDED.byron_amount,
              account_with_amount = EXCLUDED.account_with_amount,
              delegator_with_stake = EXCLUDED.delegator_with_stake,
              token = EXCLUDED.token,
              token_policy = EXCLUDED.token_policy,
              token_holder = EXCLUDED.token_holder,
              token_tx = EXCLUDED.token_tx,
              out_sum = EXCLUDED.out_sum,
              fees = EXCLUDED.fees,
              tx = EXCLUDED.tx,
              block = EXCLUDED.block,
              start_time = EXCLUDED.start_time,
              end_time = EXCLUDED.end_time,
              optimal_pool_count = EXCLUDED.optimal_pool_count,
              decentralisation = EXCLUDED.decentralisation,
              nonce = EXCLUDED.nonce,
              holder_range = EXCLUDED.holder_range,
              exchange_rate = EXCLUDED.exchange_rate";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} epochs)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllEpochRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing epoch records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_epochs");
            _logger.LogInformation("✅ Successfully deleted all existing epoch records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}