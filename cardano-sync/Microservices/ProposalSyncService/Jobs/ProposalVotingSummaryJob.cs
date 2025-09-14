using Microsoft.EntityFrameworkCore;
using ProposalSyncService.ApiResponses;
using ProposalSyncService.Services;
using Quartz;
using SharedLibrary.DatabaseContext;
using SharedLibrary.DatabaseHelpers;
using System.Data;

namespace ProposalSyncService.Jobs;


[DisallowConcurrentExecution]
public class ProposalVotingSummaryJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly ILogger<ProposalVotingSummaryJob> _logger;
    private readonly DatabaseSyncService _databaseSyncService;

    public ProposalVotingSummaryJob(
        CardanoDbContext context,
        ILogger<ProposalVotingSummaryJob> logger,
        DatabaseSyncService databaseSyncService)
    {
        _context = context;
        _logger = logger;
        _databaseSyncService = databaseSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("🚀 Starting ProposalVotingSummaryJob at {Time}", DateTime.UtcNow);

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

            // Get proposal voting summary data from backup database
            var allProposalVotingSummaries = await ProcessAllProposalIdsWithDatabase(proposalIds);

            var totalSummaries = allProposalVotingSummaries.Values.SelectMany(x => x).Count();
            _logger.LogInformation("Collected {Count} total voting summary records from {ProposalCount} proposals",
                totalSummaries, allProposalVotingSummaries.Count);

            if (allProposalVotingSummaries.Any())
            {
                await BulkRefreshProposalVotingSummaries(allProposalVotingSummaries);
                _logger.LogInformation("🎯 ProposalVotingSummaryJob completed successfully. Processed {Count} voting summary records",
                    totalSummaries);
            }
            else
            {
                _logger.LogWarning("No proposal voting summary data retrieved from backup database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in ProposalVotingSummaryJob: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get proposal voting summary data from backup database for all proposal IDs
    /// </summary>
    private async Task<Dictionary<string, List<ProposalVotingSummaryApiResponse>>> ProcessAllProposalIdsWithDatabase(List<string> proposalIds)
    {
        _logger.LogInformation("🚀 Starting database processing: {Count} proposal_ids", proposalIds.Count);

        var startTime = DateTime.UtcNow;
        var allProposalVotingSummaries = new Dictionary<string, List<ProposalVotingSummaryApiResponse>>();

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

                    var votingSummaries = await _databaseSyncService.GetProposalVotingSummaryAsync(proposalId);

                    if (votingSummaries?.Any() == true)
                    {
                        _logger.LogDebug("✅ Proposal {ProposalId} has {Count} voting summaries", proposalId, votingSummaries.Length);
                        allProposalVotingSummaries[proposalId] = votingSummaries.ToList();
                    }
                    else
                    {
                        _logger.LogDebug("⚪ Proposal {ProposalId} has no voting summaries", proposalId);
                        allProposalVotingSummaries[proposalId] = new List<ProposalVotingSummaryApiResponse>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing proposal_id {ProposalId}: {Message}", proposalId, ex.Message);
                    allProposalVotingSummaries[proposalId] = new List<ProposalVotingSummaryApiResponse>();
                }
            }

            var batchDuration = DateTime.UtcNow - batchStartTime;
            var batchSuccessCount = allProposalVotingSummaries.Count(kvp => kvp.Value.Any());
            _logger.LogInformation("✅ Batch {BatchNumber} completed in {Duration}. Success: {SuccessCount}/{BatchSize}",
                batchIndex + 1, batchDuration, batchSuccessCount, batch.Count);
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var totalSummaries = allProposalVotingSummaries.Values.SelectMany(x => x).Count();

        _logger.LogInformation("🎯 Database processing completed in {Duration}. Processed {ProcessedCount}/{TotalCount} proposals successfully. Total voting summaries: {SummaryCount}",
            totalDuration, allProposalVotingSummaries.Count, proposalIds.Count, totalSummaries);

        return allProposalVotingSummaries;
    }

    private async Task BulkRefreshProposalVotingSummaries(Dictionary<string, List<ProposalVotingSummaryApiResponse>> proposalVotingSummariesDict)
    {
        var totalCount = proposalVotingSummariesDict.Values.SelectMany(x => x).Count();
        _logger.LogInformation("🔄 Starting full refresh for {Count} proposal voting summary records", totalCount);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllProposalVotingSummaryRecords();

            // Step 2: Flatten all voting summaries with their proposal_id for batch processing
            var allVotingSummaries = new List<(string proposalId, ProposalVotingSummaryApiResponse summary)>();
            foreach (var kvp in proposalVotingSummariesDict)
            {
                var proposalId = kvp.Key;
                var summaries = kvp.Value;

                if (string.IsNullOrWhiteSpace(proposalId) || !summaries.Any())
                    continue;

                foreach (var summary in summaries)
                {
                    allVotingSummaries.Add((proposalId, summary));
                }
            }

            // Step 3: Insert all records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)allVotingSummaries.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = allVotingSummaries.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records across {ProposalCount} proposals",
                totalCount, proposalVotingSummariesDict.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task InsertBatchWithRawSql(List<(string proposalId, ProposalVotingSummaryApiResponse summary)> batch, int batchNumber, int totalBatches)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();

        if (command.Connection?.State != ConnectionState.Open)
            await command.Connection!.OpenAsync();

        var valueParts = new List<string>();
        var paramIndex = 0;

        foreach (var (proposalId, summary) in batch)
        {
            // Create parameters for this record - mapping all fields from the API response to database model
            var proposalIdParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", proposalId);
            var proposalTypeParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.proposal_type);
            var epochNoParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.epoch_no);
            var drepYesVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_yes_votes_cast);
            var drepActiveYesVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_active_yes_vote_power);
            var drepYesVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_yes_vote_power);
            var drepYesPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_yes_pct);
            var drepNoVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_no_votes_cast);
            var drepActiveNoVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_active_no_vote_power);
            var drepNoVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_no_vote_power);
            var drepNoPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_no_pct);
            var drepAbstainVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_abstain_votes_cast);
            var drepActiveAbstainVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_active_abstain_vote_power);
            var drepAlwaysNoConfidenceVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_always_no_confidence_vote_power);
            var drepAlwaysAbstainVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.drep_always_abstain_vote_power);
            var poolYesVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_yes_votes_cast);
            var poolActiveYesVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_active_yes_vote_power);
            var poolYesVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_yes_vote_power);
            var poolYesPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_yes_pct);
            var poolNoVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_no_votes_cast);
            var poolActiveNoVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_active_no_vote_power);
            var poolNoVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_no_vote_power);
            var poolNoPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_no_pct);
            var poolAbstainVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_abstain_votes_cast);
            var poolActiveAbstainVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_active_abstain_vote_power);
            var poolPassiveAlwaysAbstainVotesAssignedParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_passive_always_abstain_votes_assigned);
            var poolPassiveAlwaysAbstainVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_passive_always_abstain_vote_power);
            var poolPassiveAlwaysNoConfidenceVotesAssignedParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_passive_always_no_confidence_votes_assigned);
            var poolPassiveAlwaysNoConfidenceVotePowerParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.pool_passive_always_no_confidence_vote_power);
            var committeeYesVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.committee_yes_votes_cast);
            var committeeYesPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.committee_yes_pct);
            var committeeNoVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.committee_no_votes_cast);
            var committeeNoPctParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.committee_no_pct);
            var committeeAbstainVotesCastParam = DbParameterHelper.CreateParameter(command, $"p{paramIndex++}", summary.committee_abstain_votes_cast);

            valueParts.Add($@"(@{proposalIdParam.ParameterName}, @{proposalTypeParam.ParameterName}, @{epochNoParam.ParameterName}, @{drepYesVotesCastParam.ParameterName}, 
                @{drepActiveYesVotePowerParam.ParameterName}, @{drepYesVotePowerParam.ParameterName}, @{drepYesPctParam.ParameterName}, 
                @{drepNoVotesCastParam.ParameterName}, @{drepActiveNoVotePowerParam.ParameterName}, @{drepNoVotePowerParam.ParameterName}, 
                @{drepNoPctParam.ParameterName}, @{drepAbstainVotesCastParam.ParameterName}, @{drepActiveAbstainVotePowerParam.ParameterName}, 
                @{drepAlwaysNoConfidenceVotePowerParam.ParameterName}, @{drepAlwaysAbstainVotePowerParam.ParameterName}, 
                @{poolYesVotesCastParam.ParameterName}, @{poolActiveYesVotePowerParam.ParameterName}, @{poolYesVotePowerParam.ParameterName}, 
                @{poolYesPctParam.ParameterName}, @{poolNoVotesCastParam.ParameterName}, @{poolActiveNoVotePowerParam.ParameterName}, 
                @{poolNoVotePowerParam.ParameterName}, @{poolNoPctParam.ParameterName}, @{poolAbstainVotesCastParam.ParameterName}, 
                @{poolActiveAbstainVotePowerParam.ParameterName}, @{poolPassiveAlwaysAbstainVotesAssignedParam.ParameterName}, 
                @{poolPassiveAlwaysAbstainVotePowerParam.ParameterName}, @{poolPassiveAlwaysNoConfidenceVotesAssignedParam.ParameterName}, 
                @{poolPassiveAlwaysNoConfidenceVotePowerParam.ParameterName}, @{committeeYesVotesCastParam.ParameterName}, 
                @{committeeYesPctParam.ParameterName}, @{committeeNoVotesCastParam.ParameterName}, @{committeeNoPctParam.ParameterName}, 
                @{committeeAbstainVotesCastParam.ParameterName})");
        }

        if (valueParts.Count == 0)
            return;

        command.CommandText = $@"INSERT INTO md_proposal_voting_summary 
            (proposal_id, proposal_type, epoch_no, drep_yes_votes_cast, drep_active_yes_vote_power, drep_yes_vote_power, drep_yes_pct, 
             drep_no_votes_cast, drep_active_no_vote_power, drep_no_vote_power, drep_no_pct, drep_abstain_votes_cast, 
             drep_active_abstain_vote_power, drep_always_no_confidence_vote_power, drep_always_abstain_vote_power, 
             pool_yes_votes_cast, pool_active_yes_vote_power, pool_yes_vote_power, pool_yes_pct, pool_no_votes_cast, 
             pool_active_no_vote_power, pool_no_vote_power, pool_no_pct, pool_abstain_votes_cast, pool_active_abstain_vote_power, 
             pool_passive_always_abstain_votes_assigned, pool_passive_always_abstain_vote_power, 
             pool_passive_always_no_confidence_votes_assigned, pool_passive_always_no_confidence_vote_power, 
             committee_yes_votes_cast, committee_yes_pct, committee_no_votes_cast, committee_no_pct, committee_abstain_votes_cast)
            VALUES {string.Join(", ", valueParts)}
            ON CONFLICT (proposal_id, epoch_no) DO UPDATE SET 
              proposal_type = EXCLUDED.proposal_type,
              drep_yes_votes_cast = EXCLUDED.drep_yes_votes_cast,
              drep_active_yes_vote_power = EXCLUDED.drep_active_yes_vote_power,
              drep_yes_vote_power = EXCLUDED.drep_yes_vote_power,
              drep_yes_pct = EXCLUDED.drep_yes_pct,
              drep_no_votes_cast = EXCLUDED.drep_no_votes_cast,
              drep_active_no_vote_power = EXCLUDED.drep_active_no_vote_power,
              drep_no_vote_power = EXCLUDED.drep_no_vote_power,
              drep_no_pct = EXCLUDED.drep_no_pct,
              drep_abstain_votes_cast = EXCLUDED.drep_abstain_votes_cast,
              drep_active_abstain_vote_power = EXCLUDED.drep_active_abstain_vote_power,
              drep_always_no_confidence_vote_power = EXCLUDED.drep_always_no_confidence_vote_power,
              drep_always_abstain_vote_power = EXCLUDED.drep_always_abstain_vote_power,
              pool_yes_votes_cast = EXCLUDED.pool_yes_votes_cast,
              pool_active_yes_vote_power = EXCLUDED.pool_active_yes_vote_power,
              pool_yes_vote_power = EXCLUDED.pool_yes_vote_power,
              pool_yes_pct = EXCLUDED.pool_yes_pct,
              pool_no_votes_cast = EXCLUDED.pool_no_votes_cast,
              pool_active_no_vote_power = EXCLUDED.pool_active_no_vote_power,
              pool_no_vote_power = EXCLUDED.pool_no_vote_power,
              pool_no_pct = EXCLUDED.pool_no_pct,
              pool_abstain_votes_cast = EXCLUDED.pool_abstain_votes_cast,
              pool_active_abstain_vote_power = EXCLUDED.pool_active_abstain_vote_power,
              pool_passive_always_abstain_votes_assigned = EXCLUDED.pool_passive_always_abstain_votes_assigned,
              pool_passive_always_abstain_vote_power = EXCLUDED.pool_passive_always_abstain_vote_power,
              pool_passive_always_no_confidence_votes_assigned = EXCLUDED.pool_passive_always_no_confidence_votes_assigned,
              pool_passive_always_no_confidence_vote_power = EXCLUDED.pool_passive_always_no_confidence_vote_power,
              committee_yes_votes_cast = EXCLUDED.committee_yes_votes_cast,
              committee_yes_pct = EXCLUDED.committee_yes_pct,
              committee_no_votes_cast = EXCLUDED.committee_no_votes_cast,
              committee_no_pct = EXCLUDED.committee_no_pct,
              committee_abstain_votes_cast = EXCLUDED.committee_abstain_votes_cast";

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("💾 Inserted batch {BatchNumber}/{TotalBatches} ({Count} records)",
            batchNumber, totalBatches, valueParts.Count);
    }

    private async Task DeleteAllProposalVotingSummaryRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing proposal voting summary records...");
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_proposal_voting_summary");
            _logger.LogInformation("✅ Successfully deleted all existing proposal voting summary records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting existing records: {Message}", ex.Message);
            throw;
        }
    }
}