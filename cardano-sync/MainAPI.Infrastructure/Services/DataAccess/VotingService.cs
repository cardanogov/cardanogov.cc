using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class VotingService : IVotingService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<VotingService> _logger;

        public VotingService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<VotingService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<VotingCardInfoDto?> GetVotingCardDataAsync()
        {
            try
            {
                _logger.LogInformation("Getting voting card data");
                using var context = await _contextFactory.CreateDbContextAsync();

                // Lấy epoch hiện tại và epoch trước
                var currentDrepSummary = await context.dreps_epoch_summary.AsNoTracking().OrderByDescending(d => d.epoch_no).FirstOrDefaultAsync();
                var currentEpoch = currentDrepSummary?.epoch_no;
                var lastEpoch = currentEpoch - 1;

                var lastDrepSummary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .Where(d => d.epoch_no == lastEpoch)
                    .FirstOrDefaultAsync();

                var totals = await context.totals
                    .AsNoTracking()
                    .Where(s => s.epoch_no >= lastEpoch)
                    .Take(2)
                    .ToListAsync();

                var abstain = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(d => d.epoch_no >= lastEpoch && d.drep_id == "drep_always_abstain")
                    .Select(s => new
                    {
                        epoch_no = s.epoch_no,
                        amount = s.amount
                    })
                    .ToListAsync();

                var epochInfo = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no >= lastEpoch)
                    .ToListAsync();

                var currentRegister = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(currentDrepSummary?.amount), 1000000, 1);

                var lastRegister = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(lastDrepSummary?.amount), 1000000, 1);

                var registerChange = lastRegister == 0
                    ? currentRegister > 0
                        ? 100
                        : 0
                    : Math.Round((currentRegister - lastRegister) / lastRegister * 100, 2);

                var currentAbstain = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(abstain.FirstOrDefault(a => a.epoch_no == currentEpoch)?.amount), 1000000, 1);

                var lastAbstain = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(abstain.FirstOrDefault(a => a.epoch_no == lastEpoch)?.amount), 1000000, 1);

                var abstainChange = lastAbstain == 0
                    ? currentAbstain > 0
                        ? 100
                        : 0
                    : Math.Round((currentAbstain - lastAbstain) / lastAbstain * 100, 2);

                var currentStake = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(epochInfo?.FirstOrDefault(a => a.epoch_no == currentEpoch)?.active_stake), 1000000, 1);

                var lastStake = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(epochInfo?.FirstOrDefault(a => a.epoch_no == lastEpoch)?.active_stake), 1000000, 1);

                var stakeChange = lastStake == 0
                    ? currentStake > 0
                        ? 100
                        : 0
                    : Math.Round((currentStake - lastStake) / lastStake * 100, 2);

                var currentSupply = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(totals.FirstOrDefault(t => t.epoch_no == currentEpoch)?.supply), 1000000, 1);

                var lastSupply = NumberUtils.DivideAndTruncate(
                    JsonUtils.FormatJsonBField(totals.FirstOrDefault(t => t.epoch_no == lastEpoch)?.supply), 1000000, 1);

                var supplyChange = lastSupply == 0
                    ? currentSupply > 0
                        ? 100
                        : 0
                    : Math.Round((currentSupply - lastSupply) / lastSupply * 100, 2);

                var registerRate = Math.Round((currentRegister / currentSupply) * 100, 1);
                var supplyRate = Math.Round((currentSupply / 45e9) * 100, 1);



                var result = new VotingCardInfoDto
                {
                    currentRegister = currentRegister,
                    registerChange = registerChange.ToString(),
                    registerRate = registerRate.ToString(),
                    abstainAmount = currentAbstain,
                    abstainChange = abstainChange.ToString(),
                    currentStake = currentStake,
                    stakeChange = stakeChange.ToString(),
                    currentSuplly = currentSupply,
                    supplyChange = supplyChange.ToString(),
                    supplyRate = supplyRate.ToString()
                };
                _logger.LogInformation("Successfully retrieved voting card data");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voting card data");
                throw;
            }
        }

        public async Task<VotingHistoryResponseDto?> GetVotingHistoryAsync(int page, string? filter, string? search)
        {
            try
            {
                _logger.LogInformation("Getting voting history - Page: {Page}, Filter: {Filter}, Search: {Search}", page, filter, search);
                using var context = await _contextFactory.CreateDbContextAsync();
                int pageSize = 10;
                int offset = (page - 1) * pageSize;
                var query = context.vote_list.AsNoTracking();
                search = search?.Trim().ToLower();
                filter = filter?.Trim().ToLower();
                if (!string.IsNullOrEmpty(filter))
                {
                    switch (filter)
                    {
                        case "drep":
                            query = query.Where(v => v.voter_role != null && v.voter_role.ToLower().Trim() == "drep");
                            break;
                        case "spo":
                            query = query.Where(v => v.voter_role != null && v.voter_role.ToLower().Trim() == "spo");
                            break;
                        case "constitutionalcommittee":
                            query = query.Where(v => v.voter_role != null && v.voter_role.ToLower().Trim() == "constitutionalcommittee");
                            break;
                        case "yes":
                            query = query.Where(v => v.vote != null && v.vote.ToLower().Trim() == "yes");
                            break;
                        case "no":
                            query = query.Where(v => v.vote != null && v.vote.ToLower().Trim() == "no");
                            break;
                        case "abstain":
                            query = query.Where(v => v.vote != null && v.vote.ToLower().Trim() == "abstain");
                            break;
                        default:
                            break;
                    }
                }

                // Nếu có search, lọc theo voter_id
                if (!string.IsNullOrEmpty(search))
                {
                    var voteIdFilter = new List<string?>();

                    // Lấy drep metadata và pool list để check name
                    var metaDatas = await context.dreps_metadata.AsNoTracking()
                        .Select(d => new { voter_id = d.drep_id, name = d.meta_json }).ToListAsync();

                    var poolLists = await context.pool_list.AsNoTracking().Select(s => new { voter_id = s.pool_id_bech32, name = s.ticker })
                        .ToListAsync();

                    var filteredId = query.Where(v => v.voter_id != null && v.voter_id.ToLower().Contains(search)).Select(s => s.voter_id);
                    var filteredMetadata = metaDatas
                        .Where(d => d.name != null && JsonUtils.ParseGivenName(d.name)?.ToLower().Contains(search) == true)
                        .Select(d => d.voter_id);

                    var filteredPool = poolLists
                        .Where(p => p.name != null && p.name.ToLower().Contains(search))
                            .Select(s => s.voter_id);

                    voteIdFilter = [.. filteredId
                        .Union(filteredMetadata)
                        .Union(filteredPool)
                        .Distinct()
                        .Where(v => v != null && v.Length > 0)];

                    query = query.Where(query => voteIdFilter.Any(q => q == query.voter_id));
                }





                var totalVote = await query.CountAsync();
                var voteList = await query.OrderByDescending(v => v.block_time).Skip(offset).Take(pageSize).ToListAsync();

                var filteredVoteInfo = voteList.Select(v => new VotingHistoryDto
                {
                    amount = null, // Cần truy vấn thêm voting power nếu cần
                    block_time = NumberUtils.ConvertTimestampToDate(v.block_time ?? 0),
                    epoch_no = v.epoch_no,
                    name = "", // Có thể mapping sang tên nếu cần
                    proposal_type = v.proposal_type,
                    vote = v.vote,
                    voter_id = v.voter_id,
                    voter_role = v.voter_role
                }).ToList();

                var drepIds = filteredVoteInfo
                    .Where(v => v.voter_role == "DRep")
                    .Select(v => v.voter_id)
                    .Distinct()
                    .ToList();

                var poolIds = filteredVoteInfo
                    .Where(v => v.voter_role == "SPO")
                    .Select(v => v.voter_id)
                    .Distinct()
                    .ToList();

                var epoch = filteredVoteInfo.FirstOrDefault()?.epoch_no;

                // Lấy voting power cho DRep
                var drepVotingPowers = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(d => d.drep_id != null && drepIds.Contains(d.drep_id) && d.epoch_no == epoch)
                    .ToDictionaryAsync(d => d.drep_id!, d => NumberUtils.DivideAndTruncate(JsonUtils.FormatJsonBField(d.amount), 1000000, 1));

                // Lấy voting power cho SPO
                var poolVotingPowers = await context.pools_voting_power_history
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 != null && poolIds.Contains(p.pool_id_bech32) && p.epoch_no == epoch)
                    .ToDictionaryAsync(p => p.pool_id_bech32!, p => NumberUtils.DivideAndTruncate(JsonUtils.FormatJsonBField(p.amount), 1000000, 1));

                // Cập nhật voting power vào filteredVoteInfo
                foreach (var vote in filteredVoteInfo)
                {
                    if (vote.voter_id == null) continue;
                    if (vote.voter_role == "DRep" && drepVotingPowers.TryGetValue(vote.voter_id, out var power))
                    {
                        vote.amount = power;
                        var drep = await context.dreps_metadata.FirstOrDefaultAsync(d => d.drep_id == vote.voter_id);
                        vote.name = JsonUtils.ParseGivenName(drep?.meta_json) ??
                              $"{vote.voter_id.Substring(0, Math.Min(8, vote.voter_id.Length))}...{vote.voter_id.Substring(Math.Max(0, (vote.voter_id.Length) - 6))}";

                    }
                    else if (vote.voter_role == "SPO" && poolVotingPowers.TryGetValue(vote.voter_id, out var poolPower))
                    {
                        vote.amount = poolPower;
                        var pool = await context.pool_list.FirstOrDefaultAsync(p => p.pool_id_bech32 == vote.voter_id);
                        vote.name = pool?.ticker ?? $"{vote.voter_id.Substring(0, Math.Min(8, vote.voter_id.Length))}...{vote.voter_id.Substring(Math.Max(0, (vote.voter_id.Length) - 6))}";
                    }
                }

                var result = new VotingHistoryResponseDto
                {
                    totalVote = totalVote,
                    filteredVoteInfo = filteredVoteInfo
                };
                _logger.LogInformation("Successfully retrieved voting history");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voting history");
                throw;
            }
        }

        public async Task<List<VoteListResponseDto>?> GetVoteListAsync()
        {
            try
            {
                _logger.LogInformation("Getting vote list");
                using var context = await _contextFactory.CreateDbContextAsync();
                var voteList = await context.vote_list.AsNoTracking().Where(v => v.voter_role == "ConstitutionalCommittee").ToListAsync();
                var result = voteList.Select(v => new VoteListResponseDto
                {
                    vote_tx_hash = v.vote_tx_hash,
                    voter_role = v.voter_role,
                    voter_id = v.voter_id,
                    proposal_id = v.proposal_id,
                    proposal_tx_hash = v.proposal_tx_hash,
                    proposal_index = v.proposal_index,
                    proposal_type = v.proposal_type,
                    epoch_no = v.epoch_no,
                    block_height = v.block_height,
                    block_time = v.block_time,
                    vote = v.vote,
                    meta_url = JsonUtils.FormatJsonBField(v.meta_url),
                    meta_hash = JsonUtils.FormatJsonBField(v.meta_hash),
                    meta_json = JsonUtils.FormatJsonBField(v.meta_json)
                }).ToList();
                _logger.LogInformation("Successfully retrieved vote list");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vote list");
                throw;
            }
        }

        public async Task<List<VoteStatisticResponseDto>?> GetVoteStatisticDrepSpoAsync()
        {
            try
            {
                _logger.LogInformation("Getting vote statistic for DRep and SPO");
                using var context = await _contextFactory.CreateDbContextAsync();

                // Fetch all required data in one go
                var allVotes = await context.vote_list
                    .AsNoTracking()
                    .Where(v => (v.voter_role == "DRep" || v.voter_role == "SPO") &&
                                (v.vote == "Yes" || v.vote == "No"))
                    .Select(v => new
                    {
                        v.epoch_no,
                        v.voter_id,
                        v.voter_role,
                        v.vote,
                        v.block_time
                    })
                    .ToListAsync();

                var epochList = allVotes
                    .Where(e => e.epoch_no >= 507)
                    .Select(v => v.epoch_no)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToList();

                // Fetch all voting power data
                var allDrepPower = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(d => epochList.Contains(d.epoch_no))
                    .Select(d => new
                    {
                        d.epoch_no,
                        d.drep_id,
                        d.amount
                    })
                    .ToListAsync();

                var allPoolPower = await context.pools_voting_power_history
                    .AsNoTracking()
                    .Where(p => epochList.Contains(p.epoch_no))
                    .Select(p => new
                    {
                        p.epoch_no,
                        p.pool_id_bech32,
                        p.amount
                    })
                    .ToListAsync();

                // Pre-process lookup dictionaries for O(1) access
                var drepPowerLookup = allDrepPower
                    .GroupBy(x => (x.epoch_no, x.drep_id))
                    .ToDictionary(
                        g => g.Key,
                        g => double.TryParse(JsonUtils.FormatJsonBField(g.First().amount), out double drepPower) ? drepPower : 0
                    );

                var poolPowerLookup = allPoolPower
                    .GroupBy(x => (x.epoch_no, x.pool_id_bech32))
                    .ToDictionary(
                        g => g.Key,
                        g => double.TryParse(JsonUtils.FormatJsonBField(g.First().amount), out double poolPower) ? poolPower : 0
                    );

                var allDrepsMeta = await context.dreps_metadata
                    .AsNoTracking()
                    .Select(d => new { d.drep_id, d.meta_json })
                    .ToListAsync();

                var drepMetaLookup = allDrepsMeta
                    .ToDictionary(
                        d => d.drep_id!,
                        d => JsonUtils.ParseGivenName(d.meta_json)
                    );

                var allPoolsMeta = await context.pool_list
                    .AsNoTracking()
                    .Select(p => new { p.pool_id_bech32, p.ticker })
                    .ToListAsync();

                var poolMetaLookup = allPoolsMeta
                    .ToDictionary(
                        p => p.pool_id_bech32!,
                        p => !string.IsNullOrWhiteSpace(p.ticker) ? p.ticker : $"{p.pool_id_bech32![..Math.Min(8, p.pool_id_bech32!.Length)]}...{p.pool_id_bech32[^6..]}"
                    );

                // Group votes by epoch
                var votesByEpoch = allVotes.ToLookup(v => v.epoch_no);

                var epochResults = new List<VoteStatisticResponseDto>();
                var tasks = new List<Task<VoteStatisticResponseDto>>();

                foreach (var epoch in epochList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var db = await _contextFactory.CreateDbContextAsync();
                        var epochVotes = votesByEpoch[epoch].ToList();
                        var sumYes = new List<VoteStatisticDto>();
                        var sumNo = new List<VoteStatisticDto>();

                        foreach (var vote in epochVotes)
                        {
                            double power = 0;
                            bool isDrep = vote.voter_role == "DRep";
                            string name = "";

                            // Use dictionary lookup (thread-safe as dictionaries are read-only)
                            if (isDrep)
                            {
                                drepPowerLookup.TryGetValue((epoch, vote.voter_id), out power);
                                drepMetaLookup.TryGetValue(vote.voter_id, out name);
                                name ??= $"{vote.voter_id[..Math.Min(8, vote.voter_id.Length)]}...{vote.voter_id[^6..]}";
                            }
                            else
                            {
                                poolPowerLookup.TryGetValue((epoch, vote.voter_id), out power);
                                poolMetaLookup.TryGetValue(vote.voter_id, out name);
                                name ??= $"{vote.voter_id[..Math.Min(8, vote.voter_id.Length)]}...{vote.voter_id[^6..]}";
                            }

                            if (power == 0) continue;

                            var voteDto = new VoteStatisticDto
                            {
                                id = vote.voter_id,
                                power = power,
                                block_time = vote.block_time,
                                name = name,
                                epoch_no = vote.epoch_no
                            };

                            if (vote.vote == "Yes")
                            {
                                sumYes.Add(voteDto);
                            }
                            else if (vote.vote == "No")
                            {
                                sumNo.Add(voteDto);
                            }
                        }

                        return new VoteStatisticResponseDto
                        {
                            epoch_no = epoch,
                            sum_yes_voting_power = sumYes,
                            sum_no_voting_power = sumNo
                        };
                    }));
                }

                // Wait for all tasks to complete and collect results
                epochResults.AddRange((await Task.WhenAll(tasks)).OrderBy(r => r.epoch_no));

                _logger.LogInformation("Successfully retrieved vote statistic");
                return epochResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vote statistic");
                throw;
            }
        }

        public async Task<int?> GetVoteParticipationIndexAsync()
        {
            try
            {
                _logger.LogInformation("Getting vote participation index");
                using var context = await _contextFactory.CreateDbContextAsync();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var fiveDaysAgo = now - 5 * 24 * 60 * 60;
                var totalVotesLast5Days = await context.vote_list.AsNoTracking().Where(v => v.block_time >= fiveDaysAgo).CountAsync();

                // Lấy 4 proposal gần nhất
                var recentProposals = await context.proposals_list.AsNoTracking().OrderByDescending(p => p.block_time).Take(4).Select(d => d.proposal_id).ToListAsync();

                var voteProposals = await context.proposal_voting_summary.AsNoTracking()
                    .Where(v => recentProposals.Contains(v.proposal_id))
                    .ToListAsync();

                var voteCounts = voteProposals.Sum(p =>
                    (p.drep_yes_votes_cast ?? 0) + (p.drep_no_votes_cast ?? 0) + (p.drep_abstain_votes_cast ?? 0) +
                    (p.pool_yes_votes_cast ?? 0) + (p.pool_no_votes_cast ?? 0) + (p.pool_abstain_votes_cast ?? 0) +
                    (p.committee_yes_votes_cast ?? 0) + (p.committee_no_votes_cast ?? 0) + (p.committee_abstain_votes_cast ?? 0)
                );
                double avg = voteCounts / voteProposals.Count;
                double X = (avg / 6) * 3;

                double index = totalVotesLast5Days / X;

                if (index > 1) index = 1;

                var result = (int)Math.Round(index * 100);

                _logger.LogInformation("Successfully retrieved vote participation index");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vote participation index");
                throw;
            }
        }
    }
}