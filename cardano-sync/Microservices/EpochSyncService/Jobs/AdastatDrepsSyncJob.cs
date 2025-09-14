using EpochSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using SharedLibrary.Models;
using System.Data;

namespace EpochSyncService.Jobs;


[DisallowConcurrentExecution]
public class AdastatDrepsSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly IAdastatApiClient _adastatApiClient;
    private readonly ILogger<AdastatDrepsSyncJob> _logger;

    public AdastatDrepsSyncJob(
        CardanoDbContext context,
        IAdastatApiClient adastatApiClient,
        ILogger<AdastatDrepsSyncJob> logger)
    {
        _context = context;
        _adastatApiClient = adastatApiClient;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting AdastatDrepsSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            var startTime = DateTime.UtcNow;
            var allDreps = new List<MDDreps>();
            var totalPages = 0;
            var totalDreps = 0;

            // Fetch all pages from Adastat API
            for (int page = 1; page <= 10000; page++) // Max 10000 pages as safety limit
            {
                _logger.LogInformation("📄 Fetching page {Page} from Adastat DReps API", page);

                var adastatResponse = await _adastatApiClient.GetDrepsAsync(page, 1000);

                if (adastatResponse?.Rows?.Any() != true)
                {
                    _logger.LogInformation("🏁 No more DReps found on page {Page}. Stopping pagination.", page);
                    break;
                }

                // Convert to MDDreps models
                foreach (var row in adastatResponse.Rows)
                {
                    var drep = new MDDreps
                    {
                        hash = row.Hash,
                        bech32_legacy = row.Bech32Legacy,
                        has_script = row.HasScript,
                        tx_hash = row.TxHash,
                        url = row.Url,
                        comment = row.Comment,
                        payment_address = row.PaymentAddress,
                        given_name = row.GivenName,
                        objectives = row.Objectives,
                        motivations = row.Motivations,
                        qualifications = row.Qualifications,
                        live_stake = row.LiveStake,
                        delegator = row.Delegator,
                        tx_time = row.TxTime, // Now handles nullable long
                        last_active_epoch = row.LastActiveEpoch,
                        bech32 = row.Bech32
                    };

                    allDreps.Add(drep);
                }

                totalPages = page;
                totalDreps = allDreps.Count;

                _logger.LogInformation("📊 Page {Page}: Retrieved {Count} DReps. Total so far: {Total}",
                    page, adastatResponse.Rows.Length, totalDreps);

                // Add small delay between requests to be respectful to the API
                await Task.Delay(100);
            }

            if (allDreps.Any())
            {
                await BulkRefreshDreps(allDreps);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("🎯 AdastatDrepsSyncJob completed successfully. Processed {Count} DReps from {Pages} pages in {Duration}",
                    totalDreps, totalPages, duration);
            }
            else
            {
                _logger.LogWarning("⚠️ No DReps to insert");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in AdastatDrepsSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshDreps(List<MDDreps> dreps)
    {
        _logger.LogInformation("🔄 Starting bulk refresh for {Count} DReps", dreps.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)dreps.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = dreps.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Bulk refresh completed successfully for {Count} DReps", dreps.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during bulk refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<MDDreps> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var drep in batch)
        {
            // Create parameters for this record
            var hashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.hash);
            var bech32LegacyParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.bech32_legacy);
            var hasScriptParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.has_script);
            var txHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.tx_hash);
            var urlParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.url);
            var commentParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.comment);
            var paymentAddressParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.payment_address);
            var givenNameParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.given_name);
            var objectivesParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.objectives);
            var motivationsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.motivations);
            var qualificationsParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.qualifications);
            var liveStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.live_stake);
            var delegatorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.delegator);
            var txTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.tx_time);
            var lastActiveEpochParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.last_active_epoch);
            var bech32Param = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", drep.bech32);

            valueParts.Add($@"(@{hashParam.ParameterName}, @{bech32LegacyParam.ParameterName}, @{hasScriptParam.ParameterName}, 
                @{txHashParam.ParameterName}, @{urlParam.ParameterName}, @{commentParam.ParameterName}, 
                @{paymentAddressParam.ParameterName}, @{givenNameParam.ParameterName}, @{objectivesParam.ParameterName}, 
                @{motivationsParam.ParameterName}, @{qualificationsParam.ParameterName}, @{liveStakeParam.ParameterName}, 
                @{delegatorParam.ParameterName}, @{txTimeParam.ParameterName}, @{lastActiveEpochParam.ParameterName}, 
                @{bech32Param.ParameterName})");
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_dreps 
            (hash, bech32_legacy, has_script, tx_hash, url, comment, payment_address, 
             given_name, objectives, motivations, qualifications, live_stake, delegator, 
             tx_time, last_active_epoch, bech32)
            VALUES {string.Join(", ", valueParts)}";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} DReps)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllDrepRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing DRep records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps");
            _logger.LogInformation("✅ Successfully deleted all existing DRep records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing DRep records: {Message}", ex.Message);
            throw;
        }
    }
}