using CommitteeSyncService.ApiResponses;
using CommitteeSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace CommitteeSyncService.Jobs;


[DisallowConcurrentExecution]
public class TotalsSyncJob : IJob
{
    private readonly ILogger<TotalsSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly CardanoDbContext _context;

    public TotalsSyncJob(
        ILogger<TotalsSyncJob> logger,
        DatabaseSyncService databaseSyncService,
        CardanoDbContext context
    )
    {
        _logger = logger;
        _databaseSyncService = databaseSyncService;
        _context = context;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting TotalsSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Test connections to backup databases first
            var connectionStatus = await _databaseSyncService.TestConnectionsAsync();
            _logger.LogInformation("🔍 Database connection status: {@ConnectionStatus}", connectionStatus);

            // Get totals data from backup databases
            var totalsData = await _databaseSyncService.GetTotalsAsync();

            if (totalsData?.Any() != true)
            {
                _logger.LogInformation("No totals data retrieved from backup databases");
                return;
            }

            _logger.LogInformation("Retrieved {Count} totals records from backup databases", totalsData.Length);

            // Full refresh approach: Delete all then insert new data
            await BulkRefreshTotals(totalsData);

            _logger.LogInformation("🎯 TotalsSyncJob completed successfully. Processed {Count} totals records",
                totalsData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in TotalsSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshTotals(TotalsApiResponse[] totalsData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} totals records", totalsData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllTotalsRecords();

            // Step 2: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)totalsData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = totalsData.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", totalsData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(TotalsApiResponse[] batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var total in batch)
        {
            // Create parameters for this record
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.epoch_no);
            var circulationParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.circulation);
            var treasuryParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.treasury);
            var rewardParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.reward);
            var supplyParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.supply);
            var reservesParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.reserves);
            var feesParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.fees);
            var depositsStakeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.deposits_stake);
            var depositsDrepParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.deposits_drep);
            var depositsProposalParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", total.deposits_proposal);

            valueParts.Add(
                $"(@{epochNoParam.ParameterName}, @{circulationParam.ParameterName}, @{treasuryParam.ParameterName}, @{rewardParam.ParameterName}, @{supplyParam.ParameterName}, @{reservesParam.ParameterName}, @{feesParam.ParameterName}, @{depositsStakeParam.ParameterName}, @{depositsDrepParam.ParameterName}, @{depositsProposalParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_totals 
            (epoch_no, circulation, treasury, reward, supply, reserves, fees, deposits_stake, deposits_drep, deposits_proposal)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (epoch_no) DO UPDATE SET 
              circulation = EXCLUDED.circulation,
              treasury = EXCLUDED.treasury,
              reward = EXCLUDED.reward,
              supply = EXCLUDED.supply,
              reserves = EXCLUDED.reserves,
              fees = EXCLUDED.fees,
              deposits_stake = EXCLUDED.deposits_stake,
              deposits_drep = EXCLUDED.deposits_drep,
              deposits_proposal = EXCLUDED.deposits_proposal";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllTotalsRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing totals records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_totals");
            _logger.LogInformation("✅ Successfully deleted all existing totals records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}