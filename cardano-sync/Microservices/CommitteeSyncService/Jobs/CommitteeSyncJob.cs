using CommitteeSyncService.ApiResponses;
using CommitteeSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace CommitteeSyncService.Jobs;


[DisallowConcurrentExecution]
public class CommitteeSyncJob : IJob
{
    private readonly ILogger<CommitteeSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly CardanoDbContext _context;

    public CommitteeSyncJob(
        ILogger<CommitteeSyncJob> logger,
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
        _logger.LogInformation("🚀 Starting CommitteeSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Test connections to backup databases first
            var connectionStatus = await _databaseSyncService.TestConnectionsAsync();
            _logger.LogInformation("🔍 Database connection status: {@ConnectionStatus}", connectionStatus);

            // Get committee info data from backup databases
            var committeeData = await _databaseSyncService.GetCommitteeInfoAsync();

            if (committeeData?.Any() != true)
            {
                _logger.LogInformation("No committee info data retrieved from backup databases");
                return;
            }

            _logger.LogInformation("Retrieved {Count} committee info records from backup databases", committeeData.Length);

            // Full refresh approach: Delete all then insert new data
            await BulkRefreshCommitteeInformation(committeeData);

            _logger.LogInformation("🎯 CommitteeSyncJob completed successfully. Processed {Count} committee info records",
                committeeData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in CommitteeSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task BulkRefreshCommitteeInformation(CommitteeInfoApiResponse[] committeeData)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} committee information records", committeeData.Length);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllCommitteeInformationRecords();

            // Step 2: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)committeeData.Length / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = committeeData.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", committeeData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(CommitteeInfoApiResponse[] batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var committee in batch)
        {
            // Create parameters for this record
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", committee.proposal_id);
            var proposalTxHashParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", committee.proposal_tx_hash);
            var proposalIndexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", committee.proposal_index);
            var quorumNumeratorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", committee.quorum_numerator);
            var quorumDenominatorParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", committee.quorum_denominator);

            // Serialize members to JSON
            var membersParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", committee.members);

            valueParts.Add(
                $"(@{proposalIdParam.ParameterName}, @{proposalTxHashParam.ParameterName}, @{proposalIndexParam.ParameterName}, @{quorumNumeratorParam.ParameterName}, @{quorumDenominatorParam.ParameterName}, @{membersParam.ParameterName})"
            );
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_committee_information 
            (proposal_id, proposal_tx_hash, proposal_index, quorum_numerator, quorum_denominator, members)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (proposal_id, proposal_tx_hash, proposal_index) DO UPDATE SET 
              quorum_numerator = EXCLUDED.quorum_numerator,
              quorum_denominator = EXCLUDED.quorum_denominator,
              members = EXCLUDED.members";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllCommitteeInformationRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing committee information records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_committee_information");
            _logger.LogInformation("✅ Successfully deleted all existing committee information records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}