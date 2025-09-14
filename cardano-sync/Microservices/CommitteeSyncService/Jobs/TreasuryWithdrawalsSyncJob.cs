using CommitteeSyncService.ApiResponses;
using CommitteeSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace CommitteeSyncService.Jobs;


[DisallowConcurrentExecution]
public class TreasuryWithdrawalsSyncJob : IJob
{
    private readonly ILogger<TreasuryWithdrawalsSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly CardanoDbContext _context;

    public TreasuryWithdrawalsSyncJob(
        ILogger<TreasuryWithdrawalsSyncJob> logger,
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
        _logger.LogInformation("🚀 Starting TreasuryWithdrawalsSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Test connections to backup databases first
            var connectionStatus = await _databaseSyncService.TestConnectionsAsync();
            _logger.LogInformation("🔍 Database connection status: {@ConnectionStatus}", connectionStatus);

            // Get treasury withdrawals data from backup databases
            var treasuryData = await _databaseSyncService.GetTreasuryWithdrawalsAsync();

            if (treasuryData?.Any() != true)
            {
                _logger.LogInformation("No treasury withdrawals data retrieved from backup databases");
                return;
            }

            _logger.LogInformation("Retrieved {Count} treasury withdrawals records from backup databases", treasuryData.Length);

            // Full refresh approach: Delete all then insert new data
            await BulkRefreshTreasuryWithdrawals(treasuryData);

            _logger.LogInformation("🎯 TreasuryWithdrawalsSyncJob completed successfully. Processed {Count} treasury withdrawals records",
                treasuryData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in TreasuryWithdrawalsSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshTreasuryWithdrawals(TreasuryWithdrawalsApiResponse[] treasuryData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} treasury withdrawals records", treasuryData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllTreasuryWithdrawalsRecords();

            // Step 2: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)treasuryData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = treasuryData.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", treasuryData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(TreasuryWithdrawalsApiResponse[] batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var treasury in batch)
        {
            // Create parameters for this record
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", treasury.epoch_no);
            var sumParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", treasury.sum);

            valueParts.Add(
                $"(@{epochNoParam.ParameterName}, @{sumParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_treasury_withdrawals 
            (epoch_no, sum)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (epoch_no) DO UPDATE SET sum = EXCLUDED.sum";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllTreasuryWithdrawalsRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing treasury withdrawals records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_treasury_withdrawals");
            _logger.LogInformation("✅ Successfully deleted all existing treasury withdrawals records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}