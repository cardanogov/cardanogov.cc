using EpochSyncService.ApiResponses;
using EpochSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace EpochSyncService.Jobs;


[DisallowConcurrentExecution]
public class EpochProtocolParametersSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly ILogger<EpochProtocolParametersSyncJob> _logger;

    public EpochProtocolParametersSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        ILogger<EpochProtocolParametersSyncJob> logger
    )
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.UtcNow;
        var triggerName = context.Trigger.Key.Name;

        try
        {
            _logger.LogInformation(
                "EpochProtocolParametersSyncJob started at {StartTime} by trigger: {TriggerName}",
                startTime,
                triggerName
            );

            await SyncEpochProtocolParametersDataAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "EpochProtocolParametersSyncJob completed successfully at {Time}. Duration: {Duration}. Trigger: {TriggerName}",
                DateTime.UtcNow,
                duration,
                triggerName
            );
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "EpochProtocolParametersSyncJob failed at {Time} after {Duration}. Error: {Error}. Trigger: {TriggerName}",
                DateTime.UtcNow,
                duration,
                ex.Message,
                triggerName
            );

            // Re-throw để Quartz biết job đã fail
            throw;
        }
    }

    private async Task SyncEpochProtocolParametersDataAsync()
    {
        _logger.LogInformation("🚀 Starting to sync epoch protocol parameters data from backup database...");

        var startTime = DateTime.UtcNow;

        // Get all epoch protocol parameters data from backup database
        var epochParamsData = await _databaseSyncService.GetEpochProtocolParametersAsync();

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "✅ Database returned {Count} epoch protocol parameters records in {Duration}",
            epochParamsData.Length,
            duration
        );

        if (epochParamsData.Any())
        {
            await BulkRefreshEpochProtocolParametersData(epochParamsData);
            _logger.LogInformation(
                "🎯 EpochProtocolParametersSyncJob completed successfully. Processed {TotalCount} epoch protocol parameters records in {ElapsedTime}",
                epochParamsData.Length,
                DateTime.UtcNow - startTime
            );
        }
        else
        {
            _logger.LogWarning("⚠️ No epoch protocol parameters data retrieved from backup database");
        }
    }

    private async Task BulkRefreshEpochProtocolParametersData(
        EpochProtocolParametersApiResponse[] epochParamsData
    )
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} epoch protocol parameters records", epochParamsData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllEpochProtocolParametersRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)epochParamsData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = epochParamsData.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", epochParamsData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<EpochProtocolParametersApiResponse> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var record in batch)
        {
            // Create parameters for this record
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.epoch_no);
            var minFeeAParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.min_fee_a);
            var minFeeBParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.min_fee_b);
            var maxBlockSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_block_size);
            var maxTxSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_tx_size);
            var maxBhSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_bh_size);
            var keyDepositParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.key_deposit);
            var poolDepositParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pool_deposit);
            var maxEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_epoch);
            var optimalPoolCountParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.optimal_pool_count);
            var influenceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.influence);
            var monetaryExpandRateParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.monetary_expand_rate);
            var treasuryGrowthRateParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.treasury_growth_rate);
            var decentralisationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.decentralisation);
            var extraEntropyParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.extra_entropy);
            var protocolMajorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.protocol_major);
            var protocolMinorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.protocol_minor);
            var minUtxoValueParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.min_utxo_value);
            var minPoolCostParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.min_pool_cost);
            var nonceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.nonce);
            var blockHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.block_hash);
            var costModelsParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", record.cost_models);
            var priceMemParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.price_mem);
            var priceStepParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.price_step);
            var maxTxExMemParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_tx_ex_mem);
            var maxTxExStepsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_tx_ex_steps);
            var maxBlockExMemParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_block_ex_mem);
            var maxBlockExStepsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_block_ex_steps);
            var maxValSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_val_size);
            var collateralPercentParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.collateral_percent);
            var maxCollateralInputsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.max_collateral_inputs);
            var coinsPerUtxoSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.coins_per_utxo_size);
            var pvtMotionNoConfidenceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pvt_motion_no_confidence);
            var pvtCommitteeNormalParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pvt_committee_normal);
            var pvtCommitteeNoConfidenceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pvt_committee_no_confidence);
            var pvtHardForkInitiationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pvt_hard_fork_initiation);
            var dvtMotionNoConfidenceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_motion_no_confidence);
            var dvtCommitteeNormalParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_committee_normal);
            var dvtCommitteeNoConfidenceParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_committee_no_confidence);
            var dvtUpdateToConstitutionParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_update_to_constitution);
            var dvtHardForkInitiationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_hard_fork_initiation);
            var dvtPpNetworkGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_p_p_network_group);
            var dvtPpEconomicGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_p_p_economic_group);
            var dvtPpTechnicalGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_p_p_technical_group);
            var dvtPpGovGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_p_p_gov_group);
            var dvtTreasuryWithdrawalParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.dvt_treasury_withdrawal);
            var committeeMinSizeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.committee_min_size);
            var committeeMaxTermLengthParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.committee_max_term_length);
            var govActionLifetimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.gov_action_lifetime);
            var govActionDepositParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.gov_action_deposit);
            var drepDepositParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_deposit);
            var drepActivityParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.drep_activity);
            var pvtppSecurityGroupParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.pvtpp_security_group);
            var minFeeRefScriptCostPerByteParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", record.min_fee_ref_script_cost_per_byte);

            valueParts.Add(
                $"(@{epochNoParam.ParameterName}, @{minFeeAParam.ParameterName}, @{minFeeBParam.ParameterName}, @{maxBlockSizeParam.ParameterName}, @{maxTxSizeParam.ParameterName}, @{maxBhSizeParam.ParameterName}, @{keyDepositParam.ParameterName}, @{poolDepositParam.ParameterName}, @{maxEpochParam.ParameterName}, @{optimalPoolCountParam.ParameterName}, @{influenceParam.ParameterName}, @{monetaryExpandRateParam.ParameterName}, @{treasuryGrowthRateParam.ParameterName}, @{decentralisationParam.ParameterName}, @{extraEntropyParam.ParameterName}, @{protocolMajorParam.ParameterName}, @{protocolMinorParam.ParameterName}, @{minUtxoValueParam.ParameterName}, @{minPoolCostParam.ParameterName}, @{nonceParam.ParameterName}, @{blockHashParam.ParameterName}, @{costModelsParam.ParameterName}, @{priceMemParam.ParameterName}, @{priceStepParam.ParameterName}, @{maxTxExMemParam.ParameterName}, @{maxTxExStepsParam.ParameterName}, @{maxBlockExMemParam.ParameterName}, @{maxBlockExStepsParam.ParameterName}, @{maxValSizeParam.ParameterName}, @{collateralPercentParam.ParameterName}, @{maxCollateralInputsParam.ParameterName}, @{coinsPerUtxoSizeParam.ParameterName}, @{pvtMotionNoConfidenceParam.ParameterName}, @{pvtCommitteeNormalParam.ParameterName}, @{pvtCommitteeNoConfidenceParam.ParameterName}, @{pvtHardForkInitiationParam.ParameterName}, @{dvtMotionNoConfidenceParam.ParameterName}, @{dvtCommitteeNormalParam.ParameterName}, @{dvtCommitteeNoConfidenceParam.ParameterName}, @{dvtUpdateToConstitutionParam.ParameterName}, @{dvtHardForkInitiationParam.ParameterName}, @{dvtPpNetworkGroupParam.ParameterName}, @{dvtPpEconomicGroupParam.ParameterName}, @{dvtPpTechnicalGroupParam.ParameterName}, @{dvtPpGovGroupParam.ParameterName}, @{dvtTreasuryWithdrawalParam.ParameterName}, @{committeeMinSizeParam.ParameterName}, @{committeeMaxTermLengthParam.ParameterName}, @{govActionLifetimeParam.ParameterName}, @{govActionDepositParam.ParameterName}, @{drepDepositParam.ParameterName}, @{drepActivityParam.ParameterName}, @{pvtppSecurityGroupParam.ParameterName}, @{minFeeRefScriptCostPerByteParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        // Insert new records
        command.CommandText =
            $@"INSERT INTO md_epoch_protocol_parameters 
            (epoch_no, min_fee_a, min_fee_b, max_block_size, max_tx_size, max_bh_size, key_deposit, pool_deposit, max_epoch, optimal_pool_count, influence, monetary_expand_rate, treasury_growth_rate, decentralisation, extra_entropy, protocol_major, protocol_minor, min_utxo_value, min_pool_cost, nonce, block_hash, cost_models, price_mem, price_step, max_tx_ex_mem, max_tx_ex_steps, max_block_ex_mem, max_block_ex_steps, max_val_size, collateral_percent, max_collateral_inputs, coins_per_utxo_size, pvt_motion_no_confidence, pvt_committee_normal, pvt_committee_no_confidence, pvt_hard_fork_initiation, dvt_motion_no_confidence, dvt_committee_normal, dvt_committee_no_confidence, dvt_update_to_constitution, dvt_hard_fork_initiation, dvt_p_p_network_group, dvt_p_p_economic_group, dvt_p_p_technical_group, dvt_p_p_gov_group, dvt_treasury_withdrawal, committee_min_size, committee_max_term_length, gov_action_lifetime, gov_action_deposit, drep_deposit, drep_activity, pvtpp_security_group, min_fee_ref_script_cost_per_byte)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (epoch_no) DO UPDATE SET 
              min_fee_a = EXCLUDED.min_fee_a,
              min_fee_b = EXCLUDED.min_fee_b,
              max_block_size = EXCLUDED.max_block_size,
              max_tx_size = EXCLUDED.max_tx_size,
              max_bh_size = EXCLUDED.max_bh_size,
              key_deposit = EXCLUDED.key_deposit,
              pool_deposit = EXCLUDED.pool_deposit,
              max_epoch = EXCLUDED.max_epoch,
              optimal_pool_count = EXCLUDED.optimal_pool_count,
              influence = EXCLUDED.influence,
              monetary_expand_rate = EXCLUDED.monetary_expand_rate,
              treasury_growth_rate = EXCLUDED.treasury_growth_rate,
              decentralisation = EXCLUDED.decentralisation,
              extra_entropy = EXCLUDED.extra_entropy,
              protocol_major = EXCLUDED.protocol_major,
              protocol_minor = EXCLUDED.protocol_minor,
              min_utxo_value = EXCLUDED.min_utxo_value,
              min_pool_cost = EXCLUDED.min_pool_cost,
              nonce = EXCLUDED.nonce,
              block_hash = EXCLUDED.block_hash,
              cost_models = EXCLUDED.cost_models,
              price_mem = EXCLUDED.price_mem,
              price_step = EXCLUDED.price_step,
              max_tx_ex_mem = EXCLUDED.max_tx_ex_mem,
              max_tx_ex_steps = EXCLUDED.max_tx_ex_steps,
              max_block_ex_mem = EXCLUDED.max_block_ex_mem,
              max_block_ex_steps = EXCLUDED.max_block_ex_steps,
              max_val_size = EXCLUDED.max_val_size,
              collateral_percent = EXCLUDED.collateral_percent,
              max_collateral_inputs = EXCLUDED.max_collateral_inputs,
              coins_per_utxo_size = EXCLUDED.coins_per_utxo_size,
              pvt_motion_no_confidence = EXCLUDED.pvt_motion_no_confidence,
              pvt_committee_normal = EXCLUDED.pvt_committee_normal,
              pvt_committee_no_confidence = EXCLUDED.pvt_committee_no_confidence,
              pvt_hard_fork_initiation = EXCLUDED.pvt_hard_fork_initiation,
              dvt_motion_no_confidence = EXCLUDED.dvt_motion_no_confidence,
              dvt_committee_normal = EXCLUDED.dvt_committee_normal,
              dvt_committee_no_confidence = EXCLUDED.dvt_committee_no_confidence,
              dvt_update_to_constitution = EXCLUDED.dvt_update_to_constitution,
              dvt_hard_fork_initiation = EXCLUDED.dvt_hard_fork_initiation,
              dvt_p_p_network_group = EXCLUDED.dvt_p_p_network_group,
              dvt_p_p_economic_group = EXCLUDED.dvt_p_p_economic_group,
              dvt_p_p_technical_group = EXCLUDED.dvt_p_p_technical_group,
              dvt_p_p_gov_group = EXCLUDED.dvt_p_p_gov_group,
              dvt_treasury_withdrawal = EXCLUDED.dvt_treasury_withdrawal,
              committee_min_size = EXCLUDED.committee_min_size,
              committee_max_term_length = EXCLUDED.committee_max_term_length,
              gov_action_lifetime = EXCLUDED.gov_action_lifetime,
              gov_action_deposit = EXCLUDED.gov_action_deposit,
              drep_deposit = EXCLUDED.drep_deposit,
              drep_activity = EXCLUDED.drep_activity,
              pvtpp_security_group = EXCLUDED.pvtpp_security_group,
              min_fee_ref_script_cost_per_byte = EXCLUDED.min_fee_ref_script_cost_per_byte";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} epoch protocol parameters)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllEpochProtocolParametersRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing epoch protocol parameters records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_epoch_protocol_parameters");
            _logger.LogInformation("✅ Successfully deleted all existing epoch protocol parameters records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing epoch protocol parameters records: {Message}", ex.Message);
            throw;
        }
    }
}
