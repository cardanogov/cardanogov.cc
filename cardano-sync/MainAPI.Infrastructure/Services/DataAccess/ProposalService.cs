using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class ProposalService : IProposalService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ProposalService> _logger;
        const decimal trillion = 1_000_000_000_000_000m;
        const decimal million = 1_000_000m;
        private readonly IImageService _imageService;

        // CC mapping from JavaScript
        private readonly Dictionary<string, string> _ccMapName = new()
        {
            { "cc_hot1qvr7p6ms588athsgfd0uez5m9rlhwu3g9dt7wcxkjtr4hhsq6ytv2", "Cardano Atlantic Council" },
            { "cc_hot1qdjx6xe6e9zk3fpzk6rakmz84n0cf8ckwjvz4e8e5j2tuscr7ckq4", "Tingvard" },
            { "cc_hot1qvh20fuwhy2dnz9e6d5wmzysduaunlz5y9n8m6n2xen3pmqqvyw8v", "Eastern Cardano Council" },
            { "cc_hot1qfj0jatguuhl0cqrtd96u7asszssa3h6uhq08q0dgqzn5jgjfy0l0", "KtorZ" },
            { "cc_hot1qdc65ke6jfq2q25fcn3g89tea30tvrzpptc2tw6g8cdc7pqtmus0y", "Ace Alliance" }
        };

        public ProposalService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ProposalService> logger, IImageService imageService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _imageService = imageService;
        }

        /// <summary>
        /// Get expired proposals - matching getProposalExpired in proposalApi.js
        /// </summary>
        public async Task<GovernanceActionResponseDto?> GetProposalExpiredAsync()
        {
            try
            {
                _logger.LogInformation("Getting expired proposals from database");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get expired and enacted proposals (matching JavaScript logic)
                var expiredProposals = await context.proposals_list
                 .AsNoTracking()
                 .Where(p => (p.expired_epoch != null || p.enacted_epoch != null) && p.proposed_epoch >= 507)
                 .ToListAsync(); // Đưa dữ liệu vào bộ nhớ

                expiredProposals = expiredProposals
                    .GroupBy(p => p.proposal_id)
                    .Select(g => g.OrderByDescending(p => p.block_time)
                                  .ThenByDescending(p => p.proposal_id) // Tiebreaker
                                  .FirstOrDefault())
                    .OrderByDescending(p => p.block_time)
                    .Where(d => d != null)
                    .Select(d => d!)
                    .ToList();

                // Build proposal summaries
                var proposalInfos = new List<ProposalInfoResponseDto>();
                foreach (var proposal in expiredProposals)
                {
                    var status = proposal.expired_epoch != null && proposal.enacted_epoch == null ? "Expired" : "Enacted";
                    var timeData = await CalculateTimeData(proposal.block_time ?? 0);

                    var proposalInfo = BuildProposalSummary(proposal, status, timeData);
                    proposalInfos.Add(proposalInfo);
                }

                // Get voting summaries - simplified approach
                var votingSummaries = await FetchProposalVotingSummaries(proposalInfos);

                // Calculate statistics
                var approved = expiredProposals.Count(p => JsonUtils.ParseJsonBDecimal(p.ratified_epoch) != null);
                var total = expiredProposals.Count;

                // Monthly statistics (matching JavaScript logic)
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var nowDate = DateTimeOffset.UtcNow;
                var startThisMonth = new DateTimeOffset(nowDate.Year, nowDate.Month, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var startLastMonth = new DateTimeOffset(nowDate.Year, nowDate.Month - 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var endLastMonth = new DateTimeOffset(nowDate.Year, nowDate.Month, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(-1).ToUnixTimeSeconds();

                var thisMonth = expiredProposals.Count(p => p.block_time >= startThisMonth && p.block_time <= now);
                var lastMonth = expiredProposals.Count(p => p.block_time >= startLastMonth && p.block_time <= endLastMonth);

                var percentageChange = lastMonth == 0 ? (thisMonth > 0 ? 100 : 0) : (thisMonth / (double)lastMonth) * 100;

                var result = new GovernanceActionResponseDto
                {
                    total_proposals = total,
                    approved_proposals = approved,
                    percentage_change = percentageChange,
                    proposal_info = votingSummaries
                };

                _logger.LogInformation("Successfully retrieved {Count} expired proposals", total);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired proposals");
                throw;
            }
        }

        /// <summary>
        /// Get expired proposals by ID - matching getProposalExpired with proposal_id in proposalApi.js
        /// </summary>
        public async Task<GovernanceActionResponseDto?> GetProposalExpiredByIdAsync(string? proposalId)
        {
            try
            {
                _logger.LogInformation("Getting expired proposal by ID: {ProposalId}", proposalId);

                if (string.IsNullOrEmpty(proposalId))
                {
                    return await GetProposalExpiredAsync();
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var proposal = await context.proposals_list
                    .AsNoTracking()
                    .OrderByDescending(s => s.proposed_epoch)
                    .Where(p => p.proposal_id != null && p.proposal_id.ToLower() == proposalId.ToLower())
                    .FirstOrDefaultAsync();

                if (proposal == null)
                {
                    _logger.LogWarning("Proposal not found: {ProposalId}", proposalId);
                    return null;
                }

                var status = proposal.expired_epoch != null && proposal.enacted_epoch == null ? "Expired" : "Enacted";
                var timeData = await CalculateTimeData(proposal.block_time ?? 0);
                var proposalInfo = BuildProposalSummary(proposal, status, timeData);

                var votingSummaries = await FetchProposalVotingSummaries(new List<ProposalInfoResponseDto> { proposalInfo });

                var result = new GovernanceActionResponseDto
                {
                    total_proposals = 1,
                    approved_proposals = proposal.ratified_epoch != null ? 1 : 0,
                    percentage_change = 0,
                    proposal_info = votingSummaries
                };

                _logger.LogInformation("Successfully retrieved expired proposal: {ProposalId}", proposalId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired proposal by ID: {ProposalId}", proposalId);
                throw;
            }
        }

        /// <summary>
        /// Get live proposals - matching getProposalLive in proposalApi.js
        /// </summary>
        public async Task<List<ProposalInfoResponseDto>?> GetProposalLiveAsync()
        {
            try
            {
                _logger.LogInformation("Getting live proposals from database");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get live proposals (matching JavaScript logic)

                var liveProposals = await context.proposals_list
                .AsNoTracking()
                .Where(p => p.expired_epoch == null && p.enacted_epoch == null && p.proposed_epoch >= 507)
                .ToListAsync(); // Đưa dữ liệu vào bộ nhớ

                liveProposals = liveProposals
                    .GroupBy(p => p.proposal_id)
                    .Select(g => g.OrderByDescending(p => p.block_time)
                                  .ThenByDescending(p => p.proposal_id) // Tiebreaker
                                  .FirstOrDefault())
                    .OrderByDescending(p => p.block_time)
                    .Where(d => d != null)
                    .Select(d => d!)
                    .ToList();

                // Build proposal summaries
                var proposalInfos = new List<ProposalInfoResponseDto>();
                foreach (var proposal in liveProposals)
                {
                    var timeData = await CalculateTimeData(proposal.block_time ?? 0);
                    var proposalInfo = BuildProposalSummary(proposal, "Active", timeData);
                    proposalInfos.Add(proposalInfo);
                }

                // Get voting summaries
                var votingSummaries = await FetchProposalVotingSummaries(proposalInfos);

                _logger.LogInformation("Successfully retrieved {Count} live proposals", proposalInfos.Count);
                return votingSummaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live proposals");
                throw;
            }
        }

        /// <summary>
        /// Get live proposals by ID - matching getProposalLive with proposal_id in proposalApi.js
        /// </summary>
        public async Task<List<ProposalInfoResponseDto>?> GetProposalLiveByIdAsync(string? proposalId)
        {
            try
            {
                _logger.LogInformation("Getting live proposal by ID: {ProposalId}", proposalId);

                if (string.IsNullOrEmpty(proposalId))
                {
                    return await GetProposalLiveAsync();
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var proposal = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposal_id.ToLower() == proposalId.ToLower() && p.expired_epoch == null && p.enacted_epoch == null)
                    .FirstOrDefaultAsync();

                if (proposal == null)
                {
                    _logger.LogWarning("Proposal not found: {ProposalId}", proposalId);
                    return new List<ProposalInfoResponseDto>();
                }

                var timeData = await CalculateTimeData(proposal.block_time ?? 0);
                var proposalInfo = BuildProposalSummary(proposal, "Active", timeData);

                var votingSummaries = await FetchProposalVotingSummaries(new List<ProposalInfoResponseDto> { proposalInfo });

                _logger.LogInformation("Successfully retrieved live proposal: {ProposalId}", proposalId);
                return votingSummaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live proposal by ID: {ProposalId}", proposalId);
                throw;
            }
        }

        /// <summary>
        /// Get proposal statistics - matching getProposalStats in proposalApi.js
        /// </summary>
        public async Task<ProposalStatsResponseDto?> GetProposalStatsAsync()
        {
            try
            {
                _logger.LogInformation("Getting proposal statistics from database");

                using var context = await _contextFactory.CreateDbContextAsync();

                var proposals = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => ((p.expired_epoch == null && p.enacted_epoch == null) || (p.expired_epoch != null || p.enacted_epoch != null)) && p.proposed_epoch >= 507)
                    .ToListAsync(); // Đưa dữ liệu vào bộ nhớ

                var groupProposals = proposals
                    .GroupBy(p => p.proposal_id)
                    .Select(g => g.OrderByDescending(p => p.block_time)
                                  .ThenByDescending(p => p.proposal_id) // Tiebreaker
                                  .FirstOrDefault())
                    .GroupBy(p => p.proposed_epoch)
                    .Select(g => new
                    {
                        epoch_no = g.Key,
                        total = g.Count()
                    })
                    .Where(d => d != null)
                    .OrderByDescending(d => d.epoch_no)
                    .ToList();

                var epoch = proposals.Select(d => d.proposed_epoch).ToList();

                var totalProposals = groupProposals.Sum(d => d.total);
                var approvedProposals = proposals.Where(a => a.ratified_epoch != null).GroupBy(d => d.proposal_id).Count();
                var approvalRate = totalProposals > 0 ? (approvedProposals / (double)totalProposals) * 100 : 0;

                int difference = 0;
                int currentEpoch = await context.dreps_epoch_summary.MaxAsync(d => d.epoch_no) ?? 0;

                if (groupProposals.Any(a => a.epoch_no.HasValue))
                {
                    bool hasCurrentEpoch = groupProposals.Any(a => a.epoch_no == currentEpoch);

                    if (hasCurrentEpoch)
                    {
                        var totalUntilCurrent = groupProposals.FirstOrDefault(d => d.epoch_no == currentEpoch)?.total ?? 0;
                        var indexLast = groupProposals.FindIndex(d => d.epoch_no < currentEpoch);
                        var totalUntilLast = groupProposals.ElementAtOrDefault(indexLast)?.total ?? 0;

                        difference = totalUntilCurrent - totalUntilLast;
                    }
                    else
                    {
                        difference = 0;
                    }
                }

                var result = new ProposalStatsResponseDto
                {
                    totalProposals = totalProposals,
                    approvedProposals = approvedProposals,
                    approvalRate = approvalRate,
                    difference = difference
                };

                _logger.LogInformation(
                    "Successfully retrieved proposal statistics - Total: {Total}, Approved: {Approved}, Difference: {Diff}",
                    totalProposals, approvedProposals, difference);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal statistics");
                throw;
            }
        }



        /// <summary>
        /// Get proposal voting summary - matching getProposalVotingSummary in proposalApi.js
        /// </summary>
        public async Task<ProposalVotingSummaryResponseDto?> GetProposalVotingSummaryAsync(string govId)
        {
            try
            {
                _logger.LogInformation("Getting proposal voting summary for: {GovId}", govId);

                using var context = await _contextFactory.CreateDbContextAsync();
                var votingSummary = await context.proposal_voting_summary
                    .AsNoTracking()
                    .Where(v => v.proposal_id.ToLower() == govId.ToLower())
                    .FirstOrDefaultAsync();

                if (votingSummary == null)
                {
                    _logger.LogWarning("Voting summary not found for: {GovId}", govId);
                    return null;
                }

                var result = new ProposalVotingSummaryResponseDto
                {
                    proposal_type = votingSummary.proposal_type,
                    epoch_no = votingSummary.epoch_no,
                    drep_yes_votes_cast = votingSummary.drep_yes_votes_cast,
                    drep_yes_vote_power = votingSummary.drep_yes_vote_power,
                    drep_yes_pct = votingSummary.drep_yes_pct,
                    drep_no_votes_cast = votingSummary.drep_no_votes_cast,
                    drep_no_vote_power = votingSummary.drep_no_vote_power,
                    drep_no_pct = votingSummary.drep_no_pct,
                    drep_abstain_votes_cast = votingSummary.drep_abstain_votes_cast,
                    drep_always_no_confidence_vote_power = votingSummary.drep_always_no_confidence_vote_power,
                    drep_always_abstain_vote_power = votingSummary.drep_always_abstain_vote_power,
                    pool_yes_votes_cast = votingSummary.pool_yes_votes_cast,
                    pool_yes_vote_power = votingSummary.pool_yes_vote_power,
                    pool_yes_pct = votingSummary.pool_yes_pct,
                    pool_no_votes_cast = votingSummary.pool_no_votes_cast,
                    pool_no_vote_power = votingSummary.pool_no_vote_power,
                    pool_no_pct = votingSummary.pool_no_pct,
                    pool_abstain_votes_cast = votingSummary.pool_abstain_votes_cast,
                    pool_passive_always_abstain_votes_assigned = votingSummary.pool_passive_always_abstain_votes_assigned,
                    pool_passive_always_abstain_vote_power = votingSummary.pool_passive_always_abstain_vote_power,
                    pool_passive_always_no_confidence_votes_assigned = votingSummary.pool_passive_always_no_confidence_votes_assigned,
                    pool_passive_always_no_confidence_vote_power = votingSummary.pool_passive_always_no_confidence_vote_power,
                    committee_yes_votes_cast = votingSummary.committee_yes_votes_cast,
                    committee_yes_pct = votingSummary.committee_yes_pct,
                    committee_no_votes_cast = votingSummary.committee_no_votes_cast,
                    committee_no_pct = votingSummary.committee_no_pct,
                    committee_abstain_votes_cast = votingSummary.committee_abstain_votes_cast
                };

                _logger.LogInformation("Successfully retrieved proposal voting summary for: {GovId}", govId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal voting summary for: {GovId}", govId);
                throw;
            }
        }

        /// <summary>
        /// Get proposal votes with pagination - matching getProposalVotes in proposalApi.js
        /// </summary>
        public async Task<ProposalVotesResponseDto?> GetProposalVotesAsync(string proposalId, int page, string? filter, string? search)
        {
            try
            {
                _logger.LogInformation("Getting proposal votes for: {ProposalId}, Page: {Page}, Filter: {Filter}, Search: {Search}",
                    proposalId, page, filter, search);

                var offset = (page - 1) * 10;
                var allowedVotes = new[] { "abstain", "yes", "no" };
                var allowedRoles = new[] { "constitutionalcommittee", "spo", "drep" };
                search = search?.Trim().ToLower();
                filter = filter?.Trim().ToLower();

                using var context = await _contextFactory.CreateDbContextAsync();

                // Build query for total votes count
                var totalVotesQuery = context.proposal_votes
                    .AsNoTracking()
                    .Where(v => v.proposal_id != null && v.proposal_id.Trim().ToLower() == proposalId.Trim().ToLower());

                if (!string.IsNullOrEmpty(filter))
                {
                    if (allowedVotes.Contains(filter))
                    {
                        totalVotesQuery = totalVotesQuery.Where(v => v.vote != null && v.vote.ToLower() == filter);
                    }
                    else if (allowedRoles.Contains(filter))
                    {
                        totalVotesQuery = totalVotesQuery.Where(v => v.voter_role != null && v.voter_role.ToLower() == filter);
                    }
                }

                var totalVotes = await totalVotesQuery.CountAsync();

                var voteInfo = await totalVotesQuery.ToListAsync();
                // Map names and voting power
                var finalMap = await MapVoterNamesAndPower(context, voteInfo);

                // Build query for vote info with pagination
                // Apply search filter if provided
                var filteredVoteInfo = finalMap;
                if (!string.IsNullOrEmpty(search))
                {
                    filteredVoteInfo = finalMap.Where(v =>
                        (v.voter_id?.ToLower().Contains(search) == true) ||
                        (v.name?.ToLower().Contains(search) == true)
                    ).ToList();
                }

                var filterPagingVoteInfo = filteredVoteInfo
                           .OrderByDescending(v => v.block_time)
                           .Skip(offset)
                           .Take(10)
                           .ToList();

                var totalVotesResult = !string.IsNullOrEmpty(search) ? filteredVoteInfo.Count : totalVotes;

                var result = new ProposalVotesResponseDto
                {
                    totalVotesResult = totalVotesResult,
                    voteInfo = filterPagingVoteInfo
                };

                _logger.LogInformation("Successfully retrieved votes for proposal: {ProposalId}", proposalId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal votes for: {ProposalId}", proposalId);
                throw;
            }
        }

        /// <summary>
        /// Get governance actions statistics - matching getGovernanceActionsStatistics in proposalApi.js
        /// </summary>
        public async Task<GovernanceActionsStatisticsResponseDto?> GetGovernanceActionsStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting governance actions statistics");

                var proposalTypes = new[]
                {
                    "InfoAction", "NewConstitution", "ParameterChange", "TreasuryWithdrawals",
                    "HardForkInitiation", "NoConfidence", "NewCommittee"
                };

                using var context = await _contextFactory.CreateDbContextAsync();
                var proposals = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposed_epoch >= 507)
                    .ToListAsync();

                proposals = proposals
                   .GroupBy(p => p.proposal_id)
                   .Select(g => g.OrderByDescending(p => p.block_time)
                                 .ThenByDescending(p => p.proposal_id) // Tiebreaker
                                 .FirstOrDefault())
                   .OrderByDescending(p => p.block_time)
                   .Where(d => d != null)
                   .Select(d => d!)
                   .ToList();

                var filters = proposals.Select(p => p.proposal_type).ToList();

                // Initialize count with all types set to 0
                var typeCounts = proposalTypes.ToDictionary(type => type, type => 0);

                // Count occurrences
                foreach (var proposal in filters)
                {
                    if (!string.IsNullOrEmpty(proposal) && proposalTypes.Contains(proposal))
                    {
                        typeCounts[proposal]++;
                    }
                }

                var result = new GovernanceActionsStatisticsResponseDto
                {
                    statistics = typeCounts
                };

                _logger.LogInformation("Successfully retrieved governance actions statistics");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting governance actions statistics");
                throw;
            }
        }

        /// <summary>
        /// Get governance actions statistics by epoch - matching getGovernanceActionsStatisticsByEpoch in proposalApi.js
        /// </summary>
        public async Task<GovernanceActionsStatisticsByEpochResponseDto?> GetGovernanceActionsStatisticsByEpochAsync()
        {
            try
            {
                _logger.LogInformation("Getting governance actions statistics by epoch");

                var proposalTypes = new[]
                {
                    "InfoAction", "NewConstitution", "ParameterChange", "TreasuryWithdrawals",
                    "HardForkInitiation", "NoConfidence", "NewCommittee"
                };

                using var context = await _contextFactory.CreateDbContextAsync();
                var proposals = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposed_epoch >= 507)
                    .ToListAsync();


                proposals = proposals
                   .GroupBy(p => p.proposal_id)
                   .Select(g => g.OrderByDescending(p => p.block_time)
                                 .ThenByDescending(p => p.proposal_id) // Tiebreaker
                                 .FirstOrDefault())
                   .OrderByDescending(p => p.block_time)
                   .Where(d => d != null)
                   .Select(d => d!)
                   .ToList();

                // Initialize result object to group by epoch
                var result = new Dictionary<string, Dictionary<string, int>>();

                // Group and count by epoch
                foreach (var proposal in proposals)
                {
                    var epoch = proposal.proposed_epoch?.ToString() ?? "0";
                    var type = proposal.proposal_type;

                    if (!result.ContainsKey(epoch))
                    {
                        result[epoch] = proposalTypes.ToDictionary(t => t, t => 0);
                    }

                    if (!string.IsNullOrEmpty(type) && proposalTypes.Contains(type))
                    {
                        result[epoch][type]++;
                    }
                }

                var response = new GovernanceActionsStatisticsByEpochResponseDto
                {
                    statistics_by_epoch = result
                };

                _logger.LogInformation("Successfully retrieved governance actions statistics by epoch");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting governance actions statistics by epoch");
                throw;
            }
        }

        /// <summary>
        /// Get proposal action types - matching getProposalActionType in proposalApi.js
        /// </summary>
        public async Task<List<ProposalActionTypeResponseDto>?> GetProposalActionTypeAsync()
        {
            try
            {
                _logger.LogInformation("Getting proposal action types");

                using var context = await _contextFactory.CreateDbContextAsync();
                var proposals = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposed_epoch >= 507)
                    .Select(p => new { p.proposal_id, p.proposal_type, p.meta_json })
                    .ToListAsync();

                var result = proposals.Select(p => new ProposalActionTypeResponseDto
                {
                    proposal_id = p.proposal_id,
                    proposal_type = p.proposal_type,
                    meta_json = JsonUtils.FormatJsonBField(p.meta_json)
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} proposal action types", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal action types");
                throw;
            }
        }

        #region Helper Methods

        public async Task<(string time, string timeline, string startTime, string endTime)> CalculateTimeData(long block_time)
        {
            const long STEP_7_EPOCH_SECONDS = 5 * 7 * 86400; // Assuming 7 days in seconds

            // Get current time in seconds
            long now = (long)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Calculate end time and remaining time
            long endTime = block_time + STEP_7_EPOCH_SECONDS;
            long remainingTime = endTime - now;

            // Calculate days and hours
            int days = (int)(remainingTime / (24 * 3600));
            int hours = (int)((remainingTime % (24 * 3600)) / 3600);
            string time = remainingTime > 0 ? $"{days}d {hours}h" : "End";

            return (time, $"{NumberUtils.ConvertTimestampToDate(block_time)} - {NumberUtils.ConvertTimestampToDate(endTime)}", NumberUtils.ConvertTimestampToDate(block_time), NumberUtils.ConvertTimestampToDate(endTime));
        }

        private ProposalInfoResponseDto BuildProposalSummary(MDProposalsList proposal, string status, (string time, string timeline, string startTime, string endTime) timeData)
        {
            var result = new ProposalInfoResponseDto
            {
                proposalId = proposal.proposal_id,
                proposedEpoch = proposal.proposed_epoch,
                expiration = JsonUtils.ParseJsonInt(proposal.expiration),
                imageUrl = null,
                title = JsonUtils.ParseJsonBTitle(proposal.meta_json) ?? "No Title",
                proposalType = proposal.proposal_type ?? "Unknown",
                abstract_ = JsonUtils.ParseJsonBAbstract(proposal.meta_json) ?? "No abstract available.",
                hash = proposal.proposal_tx_hash,
                status = status,
                motivation = JsonUtils.ParseJsonBMotivation(proposal.meta_json),
                rationale = JsonUtils.ParseJsonBRationale(proposal.meta_json),
                anchorLink = JsonUtils.FormatJsonBField(proposal.meta_url),
                timeLine = timeData.timeline,
                time = timeData.time,
                deposit = NumberUtils.DivideAndTruncate(JsonUtils.FormatJsonBField(proposal.deposit), million, 0),
                startTime = timeData.startTime,
                endTime = timeData.endTime,
                supportLink = JsonUtils.ParseJsonBReferences(proposal.meta_json),
                param_proposal = JsonUtils.FormatJsonBField(proposal.param_proposal).ToLower(),
            };

            return result;
        }

        private async Task<List<ProposalInfoResponseDto>>
            FetchProposalVotingSummaries(List<ProposalInfoResponseDto> proposals)
        {
            var results = new List<ProposalInfoResponseDto>();

            using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var proposal in proposals)
            {
                // Fetch image URL using the image service
                _logger.LogInformation("Fetching image for proposal: {Title}", proposal.title);
                proposal.imageUrl = await _imageService.GetImageAsync(proposal.title ?? "", proposal.abstract_ ?? "");

                // Fetch voting summary from database
                var votingSummary = await context.proposal_voting_summary
                    .AsNoTracking()
                    .Where(v => v.proposal_id.ToLower() == proposal.proposalId.ToLower())
                    .FirstOrDefaultAsync();

                if (votingSummary != null)
                {
                    // Add total voter counts
                    proposal.totalVoter = new TotalVoterDto
                    {
                        drep = new VoterCountsDto
                        {
                            yes = votingSummary.drep_yes_votes_cast ?? 0,
                            no = votingSummary.drep_no_votes_cast ?? 0,
                            abstain = votingSummary.drep_abstain_votes_cast ?? 0
                        },
                        spo = new VoterCountsDto
                        {
                            yes = votingSummary.pool_yes_votes_cast ?? 0,
                            no = votingSummary.pool_no_votes_cast ?? 0,
                            abstain = votingSummary.pool_abstain_votes_cast ?? 0,
                            abstainAlways = votingSummary.pool_passive_always_abstain_votes_assigned ?? 0
                        },
                        cc = new VoterCountsDto
                        {
                            yes = votingSummary.committee_yes_votes_cast ?? 0,
                            no = votingSummary.committee_no_votes_cast ?? 0,
                            abstain = votingSummary.committee_abstain_votes_cast ?? 0
                        }
                    };

                    // Add DRep total stake information
                    proposal.drepYesVotes = NumberUtils.DivideAndTruncate(votingSummary.drep_yes_vote_power, trillion, 2);
                    proposal.drepYesPct = votingSummary.drep_yes_pct;
                    proposal.drepNoVotes = NumberUtils.DivideAndTruncate(votingSummary.drep_no_vote_power, trillion, 2);
                    proposal.drepNoPct = votingSummary.drep_no_pct;
                    proposal.drepActiveNoVotePower = NumberUtils.DivideAndTruncate(votingSummary.drep_active_no_vote_power, trillion, 2);
                    proposal.drepNoConfidence = NumberUtils.DivideAndTruncate(votingSummary.drep_always_no_confidence_vote_power, trillion, 2);
                    proposal.drepAbstainAlways = NumberUtils.DivideAndTruncate(votingSummary.drep_always_abstain_vote_power, trillion, 2);
                    proposal.drepAbstainActive = NumberUtils.DivideAndTruncate(votingSummary.drep_active_abstain_vote_power, trillion, 2);


                    // Add SPO total stake information
                    proposal.poolYesVotes = NumberUtils.DivideAndTruncate(votingSummary.pool_yes_vote_power, trillion, 2);
                    proposal.poolYesPct = votingSummary.pool_yes_pct;
                    proposal.poolNoVotes = NumberUtils.DivideAndTruncate(votingSummary.pool_no_vote_power, trillion, 2);
                    proposal.poolNoPct = votingSummary.pool_no_pct;
                    proposal.poolActiveNoVotePower = NumberUtils.DivideAndTruncate(votingSummary.pool_active_no_vote_power, trillion, 2);
                    proposal.poolNoConfidence = NumberUtils.DivideAndTruncate(votingSummary.pool_passive_always_no_confidence_vote_power, trillion, 2);
                    proposal.poolAbstainAlways = NumberUtils.DivideAndTruncate(votingSummary.pool_passive_always_abstain_vote_power, trillion, 2);
                    proposal.poolAbstainActive = NumberUtils.DivideAndTruncate(votingSummary.pool_active_abstain_vote_power, trillion, 2);

                    proposal.committeeYesPct = votingSummary.committee_yes_pct;

                    results.Add(proposal);
                }
                else
                {
                    // If no voting summary found, use default values (matching JavaScript logic)
                    results.Add(proposal);
                }
            }

            return results;
        }

        /// <summary>
        /// Map voter names and voting power - matching mapDrepNames, mapSpoCcNames, and getDrepAndPoolVoting in proposalApi.js
        /// </summary>
        private async Task<List<ProposalVotesDto>> MapVoterNamesAndPower(ApplicationDbContext context, List<MDProposalVotes> voteInfo)
        {
            var result = new List<ProposalVotesDto>();
            var drepIds = voteInfo.Where(v => v.voter_role == "DRep").Select(v => v.voter_id).ToList();
            var poolIds = voteInfo.Where(v => v.voter_role == "SPO").Select(v => v.voter_id).ToList();

            // Get DRep metadata for names
            var drepMetadata = new Dictionary<string, string>();
            if (drepIds.Any())
            {
                var dreps = await context.dreps_metadata
                    .AsNoTracking()
                    .Where(d => drepIds.Any(id => id.ToLower() == d.drep_id.ToLower()))
                    .ToListAsync();

                foreach (var drep in dreps)
                {
                    var name = JsonUtils.ParseGivenName(drep.meta_json) ??
                              $"{drep.drep_id?.Substring(0, Math.Min(8, drep.drep_id?.Length ?? 0))}...{drep.drep_id?.Substring(Math.Max(0, (drep.drep_id?.Length ?? 0) - 6))}";
                    drepMetadata[drep.drep_id.ToLower()] = name;
                }
            }

            // Get SPO metadata for names
            var spoMetadata = new Dictionary<string, string>();
            if (poolIds.Any())
            {
                var pools = await context.pool_list
                    .AsNoTracking()
                    .Where(p => poolIds.Any(id => id.ToLower() == p.pool_id_bech32.ToLower()))
                    .ToListAsync();

                foreach (var pool in pools)
                {
                    spoMetadata[pool.pool_id_bech32.ToLower()] = pool.ticker ?? "N/A";
                }
            }

            // Get DRep voting power
            var drepVotingPower = new Dictionary<string, double>();
            if (drepIds.Any())
            {
                var dreps = await context.dreps_info
                    .AsNoTracking()
                    .Where(d => drepIds.Any(id => id.ToLower() == d.drep_id.ToLower()))
                    .ToListAsync();

                foreach (var drep in dreps)
                {
                    drepVotingPower[drep.drep_id] = double.TryParse(drep.amount, out double amount) ? amount / 1000000 : 0.0d;
                }
            }

            // Get SPO voting power
            var spoVotingPower = new Dictionary<string, double>();
            if (poolIds.Any())
            {
                var pools = await context.pool_information
                    .AsNoTracking()
                    .Where(p => poolIds.Any(id => id.ToLower() == p.pool_id_bech32.ToLower()))
                    .ToListAsync();

                foreach (var pool in pools)
                {
                    spoVotingPower[pool.pool_id_bech32] = double.TryParse(pool.active_stake, out double active_stake) ? active_stake / 1000000 : 0.0d;
                }
            }

            // Update vote info with names and voting power
            foreach (var vote in voteInfo)
            {
                var dto = new ProposalVotesDto
                {
                    block_time = NumberUtils.ConvertTimestampToDate(vote.block_time ?? 0),
                    voter_role = vote.voter_role,
                    vote = vote.vote,
                    voter_id = vote.voter_id
                };


                if (vote.voter_role == "DRep")
                {
                    dto.name = drepMetadata.GetValueOrDefault(vote.voter_id, "N/A");
                    dto.voting_power = drepVotingPower.GetValueOrDefault(vote.voter_id, 0);
                }
                else if (vote.voter_role == "SPO")
                {
                    dto.name = spoMetadata.GetValueOrDefault(vote.voter_id, "N/A");
                    dto.voting_power = spoVotingPower.GetValueOrDefault(vote.voter_id, 0);
                }
                else if (vote.voter_role == "ConstitutionalCommittee")
                {
                    dto.name = _ccMapName.GetValueOrDefault(vote.voter_id, "N/A");
                    dto.voting_power = 1;
                }

                result.Add(dto);
            }

            return result;
        }

        #endregion

        #region Consolidated Methods

        /// <summary>
        /// Get proposals with consolidated logic - supports live, expired, or both
        /// </summary>
        public async Task<GovernanceActionResponseDto?> GetProposalsAsync(bool? isLive)
        {
            try
            {
                _logger.LogInformation("Getting proposals with IsLive: {IsLive}", isLive);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Build the query based on isLive parameter
                IQueryable<MDProposalsList> query = context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposed_epoch >= 507);

                if (isLive == true)
                {
                    // Live proposals only
                    query = query.Where(p => p.expired_epoch == null && p.enacted_epoch == null);
                }
                else if (isLive == false)
                {
                    // Expired/enacted proposals only
                    query = query.Where(p => p.expired_epoch != null || p.enacted_epoch != null);
                }
                // If isLive is null, get both live and expired proposals (no additional filter)

                var proposals = await query.ToListAsync();

                // Group and sort proposals
                proposals = proposals
                    .GroupBy(p => p.proposal_id)
                    .Select(g => g.OrderByDescending(p => p.block_time)
                                  .ThenByDescending(p => p.proposal_id)
                                  .FirstOrDefault())
                    .OrderByDescending(p => p.block_time)
                    .Where(d => d != null)
                    .Select(d => d!)
                    .ToList();

                // Build proposal summaries
                var proposalInfos = new List<ProposalInfoResponseDto>();
                foreach (var proposal in proposals)
                {
                    var status = await DetermineProposalStatus(proposal);
                    var timeData = await CalculateTimeData(proposal.block_time ?? 0);
                    var proposalInfo = BuildProposalSummary(proposal, status, timeData);
                    proposalInfos.Add(proposalInfo);
                }

                // Fetch voting summaries
                var votingSummaries = await FetchProposalVotingSummaries(proposalInfos);

                // Calculate statistics
                var (total, approved, percentageChange) = await CalculateProposalStatistics(proposals, isLive);

                var result = new GovernanceActionResponseDto
                {
                    total_proposals = total,
                    approved_proposals = approved,
                    percentage_change = percentageChange,
                    proposal_info = votingSummaries
                };

                _logger.LogInformation("Successfully retrieved {Count} proposals with IsLive: {IsLive}", total, isLive);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposals with IsLive: {IsLive}", isLive);
                throw;
            }
        }

        /// <summary>
        /// Get proposal detail with consolidated logic - supports live, expired, or both
        /// </summary>
        public async Task<GovernanceActionResponseDto?> GetProposalDetailAsync(string? proposalId, bool? isLive)
        {
            try
            {
                _logger.LogInformation("Getting proposal detail for ProposalId: {ProposalId}, IsLive: {IsLive}", proposalId, isLive);

                if (string.IsNullOrEmpty(proposalId))
                {
                    return await GetProposalsAsync(isLive);
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                // Build the query based on isLive parameter
                IQueryable<MDProposalsList> query = context.proposals_list
                    .AsNoTracking()
                    .Where(p => p.proposal_id != null && p.proposal_id.ToLower() == proposalId.ToLower());

                if (isLive == true)
                {
                    // Live proposals only
                    query = query.Where(p => p.expired_epoch == null && p.enacted_epoch == null);
                }
                else if (isLive == false)
                {
                    // Expired/enacted proposals only - no additional filter needed for detail query
                }
                // If isLive is null, get both live and expired proposals (no additional filter)

                var proposal = await query
                    .OrderByDescending(s => s.proposed_epoch)
                    .FirstOrDefaultAsync();

                if (proposal == null)
                {
                    _logger.LogWarning("Proposal not found: {ProposalId}", proposalId);
                    return null;
                }

                var status = await DetermineProposalStatus(proposal);
                var timeData = await CalculateTimeData(proposal.block_time ?? 0);
                var proposalInfo = BuildProposalSummary(proposal, status, timeData);

                var votingSummaries = await FetchProposalVotingSummaries(new List<ProposalInfoResponseDto> { proposalInfo });

                var result = new GovernanceActionResponseDto
                {
                    total_proposals = 1,
                    approved_proposals = proposal.ratified_epoch != null ? 1 : 0,
                    percentage_change = 0,
                    proposal_info = votingSummaries
                };

                _logger.LogInformation("Successfully retrieved proposal detail: {ProposalId}", proposalId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal detail for ProposalId: {ProposalId}, IsLive: {IsLive}", proposalId, isLive);
                throw;
            }
        }

        #endregion

        #region Helper Methods for Consolidated Logic

        private async Task<string> DetermineProposalStatus(MDProposalsList proposal)
        {
            //var (time, _, _, _) = await CalculateTimeData(proposal.block_time ?? 0);
            //// Trường hợp đang trong thời gian bỏ phiếu
            //if (time != "End")
            //{
            //    if (proposal.ratified_epoch != null && proposal.enacted_epoch == null)
            //    {
            //        return "Ratified";
            //    }
            //    else if (proposal.ratified_epoch != null && proposal.enacted_epoch != null)
            //    {
            //        return "Enacted";
            //    }
            //    else if (proposal.expired_epoch == null && proposal.enacted_epoch == null)
            //    {
            //        return "Active";
            //    }
            //}
            //// Trường hợp ngoài thời gian bỏ phiếu
            //else if (time == "End")
            //{
            //    if (proposal.ratified_epoch != null && proposal.enacted_epoch != null)
            //    {
            //        return "Enacted";
            //    }
            //    else if (proposal.expired_epoch != null)
            //    {
            //        return "Expired";
            //    }
            //}

            // 1. ENACTED - Proposal đã được thực thi
            if (proposal.enacted_epoch != null)
            {
                return "Enacted";
            }
            // 2. RATIFIED - Proposal đã được phê duyệt nhưng chưa thực thi
            else if (proposal.ratified_epoch != null && proposal.enacted_epoch == null)
            {
                return "Ratified";
            }
            // 3. EXPIRED - Proposal đã hết hạn hoặc bị dropped
            else if ((proposal.expired_epoch != null || proposal.dropped_epoch != null) &&
                     proposal.enacted_epoch == null && proposal.ratified_epoch == null)
            {
                return "Expired";
            }
            // 4. ACTIVE - Proposal đang hoạt động
            else if (proposal.expired_epoch == null && proposal.dropped_epoch == null &&
                     proposal.enacted_epoch == null && proposal.ratified_epoch == null)
            {
                return "Active";
            }

            return "";
        }


        private async Task<(int total, int approved, double percentageChange)> CalculateProposalStatistics(List<MDProposalsList> proposals, bool? isLive)
        {
            var total = proposals.Count;
            var approved = proposals.Count(p => JsonUtils.ParseJsonBDecimal(p.ratified_epoch) != null);

            // Calculate percentage change only for expired proposals or combined mode
            double percentageChange = 0;
            if (isLive != true && proposals.Any())
            {
                // Monthly statistics (matching JavaScript logic from GetProposalExpiredAsync)
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var nowDate = DateTimeOffset.UtcNow;
                var startThisMonth = new DateTimeOffset(nowDate.Year, nowDate.Month, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var startLastMonth = new DateTimeOffset(nowDate.Year, nowDate.Month - 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var endLastMonth = new DateTimeOffset(nowDate.Year, nowDate.Month, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(-1).ToUnixTimeSeconds();

                var thisMonth = proposals.Count(p => p.block_time >= startThisMonth && p.block_time <= now);
                var lastMonth = proposals.Count(p => p.block_time >= startLastMonth && p.block_time <= endLastMonth);

                percentageChange = lastMonth == 0 ? (thisMonth > 0 ? 100 : 0) : (thisMonth / (double)lastMonth) * 100;
            }

            return (total, approved, percentageChange);
        }

        #endregion
    }
}