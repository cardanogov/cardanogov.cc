using Microsoft.EntityFrameworkCore;
using ProposalSyncService.ApiResponses;
using ProposalSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace ProposalSyncService.Jobs;


[DisallowConcurrentExecution]
public class ProposalVotesSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly ILogger<ProposalVotesSyncJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;

    public ProposalVotesSyncJob(
        CardanoDbContext context,
        ILogger<ProposalVotesSyncJob> logger,
        DatabaseSyncService databaseSyncService)
    {
        _context = context;
        _logger = logger;
        _databaseSyncService = databaseSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting ProposalVotesSyncJob at {Time}", DateTime.UtcNow);

        try
        {
            // Get all proposal_ids from md_proposals_lists table
            var proposalIds = await _context.MDProposalsLists
                .AsNoTracking()
                .Select(p => p.proposal_id)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToListAsync();

            if (!proposalIds.Any())
            {
                _logger.LogInformation("No proposal_ids found in md_proposals_lists table");
                return;
            }

            _logger.LogInformation("Found {Count} proposal_ids to process", proposalIds.Count);

            // Get proposal votes data from backup database
            var allProposalVotes = await ProcessAllProposalIdsWithDatabase(proposalIds);

            var totalVotes = allProposalVotes.Values.SelectMany(x => x).Count();
            _logger.LogInformation("Collected {Count} total votes records from {ProposalCount} proposals",
                totalVotes, allProposalVotes.Count);

            if (allProposalVotes.Any())
            {
                await BulkRefreshProposalVotes(allProposalVotes);
                _logger.LogInformation("🎯 ProposalVotesSyncJob completed successfully. Processed {Count} vote records",
                    totalVotes);
            }
            else
            {
                _logger.LogWarning("No proposal votes data retrieved from backup database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in ProposalVotesSyncJob: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get proposal votes data from backup database for all proposal IDs
    /// </summary>
    private async Task<Dictionary<string, List<ProposalVotesApiResponse>>> ProcessAllProposalIdsWithDatabase(List<string> proposalIds)
    {
        _logger.LogInformation("🚀 Starting database processing: {Count} proposal_ids", proposalIds.Count);

        var startTime = DateTime.UtcNow;
        var allProposalVotes = new Dictionary<string, List<ProposalVotesApiResponse>>();

        // Process in batches to avoid memory issues
        const int batchSize = 50;
        var totalBatches = (int)Math.Ceiling((double)proposalIds.Count / batchSize);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = proposalIds.Skip(batchIndex * batchSize).Take(batchSize).ToList();
            _logger.LogInformation("🔄 Processing batch {BatchNumber}/{TotalBatches} with {Count} proposal_ids",
                batchIndex + 1, totalBatches, batch.Count);

            var batchStartTime = DateTime.UtcNow;

            foreach (var proposalId in batch)
            {
                try
                {
                    if (proposalId == null) continue;
                    _logger.LogDebug("📡 Processing proposal_id: {ProposalId}", proposalId);

                    var votes = await _databaseSyncService.GetProposalVotesAsync(proposalId);

                    if (votes?.Any() == true)
                    {
                        _logger.LogDebug("✅ Proposal {ProposalId} has {Count} votes", proposalId, votes.Length);
                        allProposalVotes[proposalId] = votes.ToList();
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Proposal {ProposalId} has no votes", proposalId);
                        allProposalVotes[proposalId] = new List<ProposalVotesApiResponse>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing proposal_id {ProposalId}: {Message}", proposalId, ex.Message);
                    allProposalVotes[proposalId] = new List<ProposalVotesApiResponse>();
                }
            }

            var batchDuration = DateTime.UtcNow - batchStartTime;
            var batchSuccessCount = allProposalVotes.Count(kvp => kvp.Value.Any());
            _logger.LogInformation("✅ Batch {BatchNumber} completed in {Duration}. Success: {SuccessCount}/{BatchSize}",
                batchIndex + 1, batchDuration, batchSuccessCount, batch.Count);
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var totalVotes = allProposalVotes.Values.SelectMany(x => x).Count();

        _logger.LogInformation("🎯 Database processing completed in {Duration}. Processed {ProcessedCount}/{TotalCount} proposals successfully. Total votes: {VoteCount}",
            totalDuration, allProposalVotes.Count, proposalIds.Count, totalVotes);

        return allProposalVotes;
    }

    private async Task BulkRefreshProposalVotes(Dictionary<string, List<ProposalVotesApiResponse>> proposalVotesDict)
    {
        var totalCount = proposalVotesDict.Values.SelectMany(x => x).Count();
        _logger.LogInformation("🔄 Starting full refresh for {Count} proposal votes records", totalCount);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllProposalVotesRecords();

            // Step 2: Flatten all votes with their proposal_id for batch processing
            var allVotes = new List<(string proposalId, ProposalVotesApiResponse vote)>();
            foreach (var kvp in proposalVotesDict)
            {
                var proposalId = kvp.Key;
                var votes = kvp.Value;

                if (string.IsNullOrWhiteSpace(proposalId) || !votes.Any())
                    continue;

                foreach (var vote in votes)
                {
                    allVotes.Add((proposalId, vote));
                }
            }

            // Step 3: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)allVotes.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allVotes.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records across {ProposalCount} proposals",
                totalCount, proposalVotesDict.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string proposalId, ProposalVotesApiResponse vote)> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var (proposalId, vote) in batch)
        {
            // Create parameters for this record - mapping all fields from the API response to database model
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposalId);
            var blockTimeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.block_time);
            var voterRoleParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.voter_role);
            var voterIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.voter_id);
            var voterHexParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.voter_hex);
            var voterHasScriptParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.voter_has_script);
            var voteParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", vote.vote);
            var metaUrlParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", vote.meta_url);
            var metaHashParam = DbParameterHelper.CreateJsonbParameter(command, $"p{paramIndex++}", vote.meta_hash);

            valueParts.Add($@"(@{proposalIdParam.ParameterName}, @{blockTimeParam.ParameterName}, @{voterRoleParam.ParameterName}, @{voterIdParam.ParameterName}, 
                @{voterHexParam.ParameterName}, @{voterHasScriptParam.ParameterName}, @{voteParam.ParameterName}, 
                @{metaUrlParam.ParameterName}, @{metaHashParam.ParameterName})");
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_proposal_votes 
            (proposal_id, block_time, voter_role, voter_id, voter_hex, voter_has_script, vote, meta_url, meta_hash)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (proposal_id, voter_id, voter_role) DO UPDATE SET 
              block_time = EXCLUDED.block_time,
              voter_hex = EXCLUDED.voter_hex,
              voter_has_script = EXCLUDED.voter_has_script,
              vote = EXCLUDED.vote,
              meta_url = EXCLUDED.meta_url,
              meta_hash = EXCLUDED.meta_hash";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllProposalVotesRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing proposal votes records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_proposal_votes");
            _logger.LogInformation("✅ Successfully deleted all existing proposal votes records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}