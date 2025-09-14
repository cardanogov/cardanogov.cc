using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class CombineService : ICombineService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<CombineService> _logger;

        public CombineService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<CombineService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<MembershipDataResponseDto?> GetTotalsMembershipAsync()
        {
            try
            {
                _logger.LogInformation("Getting totals membership data");

                using var context = await _contextFactory.CreateDbContextAsync();

                var totalStake = await context.epochs.AsNoTracking().OrderByDescending(d => d.no).FirstOrDefaultAsync();
                var totalPool = await context.pool_list.AsNoTracking().Where(d => d.active_stake != null && d.pool_status != null && d.pool_status == "registered" && d.active_stake != "0").CountAsync();
                var committee = await context.committee_information.AsNoTracking().Select(ci => ci.members).FirstOrDefaultAsync();
                var drepSummaries = await context.dreps_epoch_summary
                        .AsNoTracking()
                        .ToListAsync();

                drepSummaries.Add(new SharedLibrary.Models.MDDrepsEpochSummary
                {
                    epoch_no = 507,
                    dreps = 0
                });

                var lastEpochNo = drepSummaries.Max(d => d.epoch_no ?? 0);

                var drepZores = await context.dreps
                    .AsNoTracking()
                    .ToListAsync();

                var drepZeroData = drepZores
                    .Where(s => s.delegator == 0)
                    .Select(d => new { epoch_no = d.last_active_epoch - 19 })
                    .ToList();

                var drepZeroCount = drepZeroData
                    .GroupBy(d => d.epoch_no)
                    .Select(g => new { epoch_no = g.Key, count = g.Count() })
                    .ToList();

                var lastSummary = drepSummaries.FirstOrDefault(d => d.epoch_no == lastEpochNo);
                int totalDreps = lastSummary?.dreps ?? 0;

                totalDreps += drepZeroCount
                    .Where(zeroEntry => zeroEntry.epoch_no <= lastEpochNo)
                    .Sum(zeroEntry => zeroEntry.count);

                var committeeMembers = JsonUtils.ParseJsonList(committee);


                var result = new MembershipDataResponseDto
                {
                    total_stake_addresses = totalStake?.delegator,
                    total_pool = totalPool,
                    total_drep = totalDreps,
                    total_committee = committeeMembers?.Count
                };

                _logger.LogInformation("Successfully retrieved totals membership data");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving totals membership data");
                throw;
            }
        }

        public async Task<ParticipateInVotingResponseDto?> GetParticipateInVotingAsync()
        {
            try
            {
                _logger.LogInformation("Getting participate in voting data");

                using var context = await _contextFactory.CreateDbContextAsync();

                var pools = await context.pool_list.AsNoTracking().Where(d => d.active_stake != null && d.active_stake != "0").ToListAsync();
                var drepSummaries = await context.dreps_epoch_summary.AsNoTracking().ToListAsync();
                var drepZores = await context.dreps.AsNoTracking().ToListAsync();
                var currentEpoch = drepSummaries.Max(d => d.epoch_no);

                var epochStats = pools.Aggregate(
                        new Dictionary<int, EpochStats>(),
                        (acc, data) =>
                        {
                            if (!int.TryParse(data.active_epoch_no, out int epoch)) return acc;
                            if (!acc.ContainsKey(epoch))
                            {
                                acc[epoch] = new EpochStats { registered = 0 };
                            }
                            if (data.pool_status == "registered")
                            {
                                acc[epoch].registered += 1;
                            }
                            return acc;
                        });

                // Convert to array and calculate cumulative sum
                int cumulativeRegistered = 0;
                var poolResult = epochStats.Keys
                    .Select(k => (int)k)
                    .OrderBy(epoch => epoch)
                    .Select(epoch => new PoolDataDto
                    {
                        epoch_no = epoch,
                        total = cumulativeRegistered += epochStats[epoch].registered
                    })
                    .Select(item => new PoolDataDto
                    {
                        epoch_no = item.epoch_no - 3,
                        total = item.total
                    })
                    .Where(item => item.epoch_no >= 507 && item.epoch_no <= currentEpoch)
                    .ToList();

                drepSummaries.Add(new SharedLibrary.Models.MDDrepsEpochSummary { epoch_no = 507, dreps = 0 });
                drepSummaries = drepSummaries.OrderBy(d => d.epoch_no).ToList();

                var drepZeroData = drepZores.Where(s => s.delegator == 0).Select(d => new { epoch_no = d.last_active_epoch - 19 }).ToList();
                var drepZeroCount = drepZeroData.GroupBy(d => d.epoch_no)
                    .Select(g => new { epoch_no = g.Key, count = g.Count() })
                    .OrderBy(d => d.epoch_no)
                    .ToList();

                var drepResult = new List<DrepDataDto>();

                foreach (var drepEntry in drepSummaries)
                {
                    int epoch = drepEntry.epoch_no ?? 0;
                    int total = drepEntry.dreps ?? 0;

                    // Sum counts from drepZeroCountData for current and previous epochs
                    total += drepZeroCount
                        .Where(zeroEntry => zeroEntry.epoch_no <= epoch)
                        .Sum(zeroEntry => zeroEntry.count);

                    drepResult.Add(new DrepDataDto
                    {
                        epoch_no = epoch,
                        dreps = total
                    });
                }

                var result = new ParticipateInVotingResponseDto
                {
                    pool = poolResult,
                    drep = drepResult,
                    committee = new int[poolResult.Count].Select(_ => 7).ToList()
                };

                _logger.LogInformation("Successfully retrieved participate in voting data");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving participate in voting data");
                throw;
            }
        }

        public async Task<List<GovernanceParametersResponseDto>> GetGovernanceParametersAsync()
        {
            try
            {
                _logger.LogInformation("Getting governance parameters data");

                using var context = await _contextFactory.CreateDbContextAsync();

                var governanceParams = await context.epoch_protocol_parameters.AsNoTracking().Where(d => d.epoch_no >= 507).ToListAsync();

                return governanceParams.Select(s => new GovernanceParametersResponseDto
                {
                    min_fee_a = JsonUtils.ParseJsonDouble(s.min_fee_a) / 1000000,
                    min_fee_b = JsonUtils.ParseJsonDouble(s.min_fee_b) / 1000000,
                    key_deposit = JsonUtils.ParseJsonDouble(s.key_deposit) / 1000000,
                    pool_deposit = JsonUtils.ParseJsonDouble(s.pool_deposit) / 1000000,
                    min_pool_cost = JsonUtils.ParseJsonDouble(s.min_pool_cost) / 1000000,
                    monetary_expand_rate = JsonUtils.ParseJsonDouble(s.monetary_expand_rate) * 100,
                    treasury_growth_rate = JsonUtils.ParseJsonDouble(s.treasury_growth_rate) * 100,
                    gov_action_deposit = JsonUtils.ParseJsonDouble(s.gov_action_deposit) / 1000000,
                    drep_deposit = JsonUtils.ParseJsonDouble(s.drep_deposit) / 1000000,
                    epoch_no = s.epoch_no,
                    max_block_size = JsonUtils.ParseJsonDouble(s.max_block_size),
                    max_tx_size = JsonUtils.ParseJsonDouble(s.max_tx_size),
                    max_bh_size = JsonUtils.ParseJsonDouble(s.max_bh_size),
                    max_val_size = JsonUtils.ParseJsonDouble(s.max_val_size),
                    max_tx_ex_mem = JsonUtils.ParseJsonDouble(s.max_tx_ex_mem),
                    max_tx_ex_steps = JsonUtils.ParseJsonDouble(s.max_tx_ex_steps),
                    max_block_ex_mem = JsonUtils.ParseJsonDouble(s.max_block_ex_mem),
                    max_block_ex_steps = JsonUtils.ParseJsonDouble(s.max_block_ex_steps),
                    max_collateral_inputs = JsonUtils.ParseJsonDouble(s.max_collateral_inputs),
                    coins_per_utxo_size = JsonUtils.ParseJsonDouble(s.coins_per_utxo_size),
                    price_mem = JsonUtils.ParseJsonDouble(s.price_mem),
                    price_step = JsonUtils.ParseJsonDouble(s.price_step),
                    influence = JsonUtils.ParseJsonDouble(s.influence),
                    max_epoch = JsonUtils.ParseJsonInt(s.max_epoch),
                    optimal_pool_count = JsonUtils.ParseJsonInt(s.optimal_pool_count),
                    collateral_percent = JsonUtils.ParseJsonDouble(s.collateral_percent),
                    gov_action_lifetime = JsonUtils.ParseJsonDouble(s.gov_action_lifetime),
                    drep_activity = JsonUtils.ParseJsonDouble(s.drep_activity),
                    committee_min_size = Math.Round((double)(JsonUtils.ParseJsonDouble(s.committee_min_size) ?? 0)),
                    committee_max_term_length = Math.Round((double)(JsonUtils.ParseJsonDouble(s.committee_max_term_length) ?? 0)),
                    cost_models = JsonUtils.ParseJsonCostModels(s.cost_models)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving governance parameters");
                throw;
            }
        }

        public async Task<AllocationResponseDto?> GetAllocationAsync()
        {
            try
            {
                _logger.LogInformation("Getting allocation data");

                using var context = await _contextFactory.CreateDbContextAsync();

                var curentEpoch = await context.dreps_epoch_summary.AsNoTracking().MaxAsync(d => d.epoch_no);

                var dreps = await context.dreps_voting_power_history.AsNoTracking().Where(s => s.drep_id != null && s.drep_id.StartsWith("drep1") && s.epoch_no == curentEpoch).ToListAsync();
                var total = await context.totals.AsNoTracking().FirstOrDefaultAsync(s => s.epoch_no == curentEpoch);
                var delegation = await context.dreps_epoch_summary.FirstOrDefaultAsync(s => s.epoch_no == curentEpoch);
                var staking = await context.epoch.FirstOrDefaultAsync(s => s.epoch_no == curentEpoch);

                var totalActive = dreps
                    .Sum(s => JsonUtils.ParseJsonDouble(s.amount));

                var result = new AllocationResponseDto
                {
                    totalActive = NumberUtils.DivideAndTruncate(totalActive.ToString(), 1000000000000000, 1),
                    circulatingSupply = NumberUtils.DivideAndTruncate(total?.supply, 1000000000000000, 1),
                    delegation = NumberUtils.DivideAndTruncate(delegation?.amount, 1000000000000000, 1),
                    adaStaking = NumberUtils.DivideAndTruncate(JsonUtils.FormatJsonBField(staking?.active_stake), 1000000000000000, 1),
                    total = 45
                };

                _logger.LogInformation("Successfully retrieved allocation data");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allocation data");
                throw;
            }
        }

        public async Task<SearchApiResponseDto?> GetSearchAsync(string? searchTerm)
        {
            try
            {
                _logger.LogInformation("Getting search data for term: {SearchTerm}", searchTerm);

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _logger.LogInformation("Empty search term, returning empty results");
                    return new SearchApiResponseDto
                    {
                        charts = new List<ChartDto>(),
                        proposals = new List<ProposalSearchDto>(),
                        dreps = new List<DrepSearchDto>(),
                        pools = new List<PoolSearchDto>(),
                        ccs = new List<CcSearchDto>()
                    };
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var searchTermLower = searchTerm.Trim().ToLower();

                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .MaxAsync(d => d.epoch_no);

                // 1. Charts (hardcoded, filter by search term)
                var charts = new List<ChartDto>
                {
                    new ChartDto { title = "Active Votes Chart", url = "/dashboard#active-votes-chart" },
                    new ChartDto { title = "Stake Chart", url = "/dashboard#stake-chart" },
                    new ChartDto { title = "Treasury Card Chart", url = "/dashboard#treasury-card-chart" },
                    new ChartDto { title = "DReps Voting Chart", url = "/dashboard#dreps-voting-power-chart" },
                    new ChartDto { title = "SPO Voting Chart", url = "/dashboard#spo-voting-power-chart" },
                    new ChartDto { title = "Constitutional Committee Chart", url = "/dashboard#constitutional-committee-chart" },
                    new ChartDto { title = "Participation Chart", url = "/dashboard#participate-in-voting-chart" },
                    new ChartDto { title = "Wallet Address Stats Chart", url = "/dashboard#wallet-address-stats-chart" },
                    new ChartDto { title = "Governance Actions Chart", url = "/dashboard#governance-actions-chart" },
                    new ChartDto { title = "Governance Actions By Epoch Chart", url = "/dashboard#governance-actions-by-epoch-chart" },
                    new ChartDto { title = "DRep Voting Threshold Chart", url = "/dashboard#drep-voting-threshold-chart" },
                    new ChartDto { title = "Pool Voting Threshold Chart", url = "/dashboard#pool-voting-threshold-chart" },
                    new ChartDto { title = "ADA Stats Chart", url = "/dashboard#ada-stats-chart" },
                    new ChartDto { title = "ADA Stats Percentage Chart", url = "/dashboard#ada-stats-perentage-chart" },
                    new ChartDto { title = "Treasury Chart", url = "/dashboard#treasury-chart" },
                    new ChartDto { title = "Treasury Volatility Chart", url = "/dashboard#treasury-volatility-chart" },
                    new ChartDto { title = "Network Group Chart", url = "/dashboard#network-group-chart" },
                    new ChartDto { title = "Economic Group Chart", url = "/dashboard#economic-group-chart" },
                    new ChartDto { title = "Technical Group Chart", url = "/dashboard#technical-group-chart" },
                    new ChartDto { title = "Governance Group Chart", url = "/dashboard#governance-group-chart" }
                };
                var filteredCharts = charts.Where(c => c.title != null && c.title.ToLower().Contains(searchTermLower)).ToList();

                // 2. Proposals
                var proposalsQuery = await context.proposals_list
                            .AsNoTracking()
                            .GroupBy(p => p.proposal_id)
                            .Select(g => g.OrderByDescending(p => p.block_time).First())
                            .ToListAsync();

                var proposals = proposalsQuery.Select(d => new
                {
                    proposal_id = d.proposal_id,
                    title = JsonUtils.ParseJsonBTitle(d.meta_json) ?? "No Title",
                    url = "/activity/" + d.proposal_id
                }).ToList();

                var filteredProposals = proposals
                    .Where(p => p.title != null && p.title.ToLower().Trim().Contains(searchTermLower) || p.proposal_id != null && p.proposal_id.ToLower().Trim().Contains(searchTerm))
                    .Take(10)
                    .ToList();

                var matchProposals = await context.proposal_voting_summary
                    .AsNoTracking()
                    .Where(s => filteredProposals.Select(d => d.proposal_id).Contains(s.proposal_id))
                    .Select(s => new
                    {
                        proposal_id = s.proposal_id,
                        type = s.proposal_type,
                        yes = s.drep_active_yes_vote_power,
                        no = s.drep_active_no_vote_power,
                        abstain = s.drep_active_abstain_vote_power,
                    }).ToListAsync();


                var resultProposals = (from p in filteredProposals
                                       join m in matchProposals on p.proposal_id equals m.proposal_id into pm
                                       from m in pm.DefaultIfEmpty()
                                       select new ProposalSearchDto
                                       {
                                           id = p.proposal_id,
                                           title = p.title,
                                           url = p.url,
                                           type = m?.type,
                                           yes = double.TryParse(m?.yes, out double pyes) ? pyes / 1000000 : 0,
                                           no = double.TryParse(m?.no, out double pno) ? pno / 1000000 : 0,
                                           abstain = double.TryParse(m?.abstain, out double pab) ? pab / 1000000 : 0,
                                       }).ToList();




                // 3. Pools

                var poolQuery = context.pool_list.AsNoTracking().AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    poolQuery = poolQuery.Where(p => (p.pool_id_bech32 != null && p.pool_id_bech32.ToLower().Contains(searchTerm)) ||
                                           (p.ticker != null && p.ticker.ToLower().Contains(searchTerm))).OrderBy(d => d.ticker).Take(10);
                }

                var poolList = await poolQuery
                    .Select(p => new
                    {
                        pool_id_bech32 = p.pool_id_bech32,
                        ticker = p.ticker ?? "N/A",
                        url = "/spo/" + p.pool_id_bech32
                    })
                    .ToListAsync();

                var poolIds = await poolQuery
                    .Select(p => p.pool_id_bech32)
                    .ToListAsync();

                var poolInfos = await context.pool_information
                    .AsNoTracking()
                    .Where(d => poolIds.Contains(d.pool_id_bech32))
                    .Select(d => new
                    {
                        pool_id_bech32 = d.pool_id_bech32,
                        live_stake = d.live_stake,
                        margin = d.margin,
                        fixed_cost = d.fixed_cost,
                        pledge = d.live_pledge
                    })
                    .ToListAsync();

                var poolResult = new List<PoolSearchDto>();
                foreach (var pool in poolList)
                {
                    var poolInfo = poolInfos.FirstOrDefault(p => p.pool_id_bech32 == pool.pool_id_bech32);

                    double liveStakeValue = 0;
                    if (!double.TryParse(poolInfo?.live_stake, out liveStakeValue))
                        liveStakeValue = 0;
                    var liveStake = Math.Round(liveStakeValue / 1e6);

                    var margin = poolInfo?.margin;

                    double fixedCostValue = 0;
                    if (!double.TryParse(poolInfo?.fixed_cost, out fixedCostValue))
                        fixedCostValue = 0;
                    var fixedCost = Math.Round(fixedCostValue / 1e6);

                    double pledgeValue = 0;
                    if (!double.TryParse(poolInfo?.pledge, out pledgeValue))
                        pledgeValue = 0;
                    var pledge = Math.Round(pledgeValue / 1e6);

                    poolResult.Add(new PoolSearchDto
                    {
                        id = pool.pool_id_bech32,
                        title = pool.ticker,
                        url = pool.url,
                        live_stake = liveStake,
                        margin = margin,
                        fixed_cost = fixedCost,
                        pledge = pledge
                    });
                }


                // 4. CCs (hardcoded, filter by search term)
                var ccs = new List<CcSearchDto>
                {
                    new CcSearchDto { id = "cc_cold1zv6fu40c86d0yjqnum9ndr0k4qxn39gm9ge5mlxly6q42kqmjmzyj", title = "Cardano Atlantic Council", url = "/cc#cc_cold1zv6fu40c86d0yjqnum9ndr0k4qxn39gm9ge5mlxly6q42kqmjmzyj" },
                    new CcSearchDto { id = "cc_cold1zvvcpkl3443ykr94gyp4nddtzngqs4sejjnv9dk98747cqqeatx67", title = "Tingvard", url = "/cc#cc_cold1zvvcpkl3443ykr94gyp4nddtzngqs4sejjnv9dk98747cqqeatx67" },
                    new CcSearchDto { id = "cc_cold1zwz2a08a8cqdp7r6lyv0cj67qqf47sr7x7vf8hm705ujc6s4m87eh", title = "Eastern Cardano Council", url = "/cc#cc_cold1zwz2a08a8cqdp7r6lyv0cj67qqf47sr7x7vf8hm705ujc6s4m87eh" },
                    new CcSearchDto { id = "cc_cold1ztwq6mh5jkgwk6yq559qptw7zavkumtk7u2e2uh6rlu972slkt0rz", title = "KtorZ", url = "/cc#cc_cold1ztwq6mh5jkgwk6yq559qptw7zavkumtk7u2e2uh6rlu972slkt0rz" },
                    new CcSearchDto { id = "cc_cold1zwt49epsdedwsezyr5ssvnmez96v3d3xrxdcu7j9l8srk3g5xu74h", title = "Ace Alliance", url = "/cc#cc_cold1zwt49epsdedwsezyr5ssvnmez96v3d3xrxdcu7j9l8srk3g5xu74h" },
                    new CcSearchDto { id = "cc_cold1zwwv8uu8vgl5tkhx569hp94sctjq8krqr2pdcspzr6k5rcsxw2az4", title = "Cardano Japan Council", url = "/cc#cc_cold1zwwv8uu8vgl5tkhx569hp94sctjq8krqr2pdcspzr6k5rcsxw2az4" },
                    new CcSearchDto { id = "cc_cold1zgf5jdusmxcrfqapu8ngf6j04u0wfzjc7sp9wnnlyfr0f4q68as9w", title = "Phil_uplc", url = "/cc#cc_cold1zgf5jdusmxcrfqapu8ngf6j04u0wfzjc7sp9wnnlyfr0f4q68as9w" }
                };

                var filteredCCs = ccs.Where(c => c.title != null && c.title.ToLower().Contains(searchTermLower) || c.id != null && c.id.ToLower().Contains(searchTermLower)).ToList();

                // 5. DReps
                // Start with dreps_metadata and fetch all potential records
                var metadataQuery = context.dreps_metadata.AsNoTracking().AsQueryable();

                // Fetch metadata with additional fields
                var metadataList = await metadataQuery
                    .Select(m => new
                    {
                        drep_id = m.drep_id,
                        givenName = JsonUtils.ParseGivenName(m.meta_json) ?? "N/A",
                        references = m.meta_json,
                        image = m.meta_json
                    })
                    .ToListAsync();

                // Apply search filter on givenName in-memory
                if (!string.IsNullOrWhiteSpace(searchTermLower))
                {
                    metadataList = metadataList
                        .Where(m => m.givenName.ToLower().Contains(searchTermLower) || m.drep_id != null && m.drep_id.ToLower().Contains(searchTermLower))
                        .OrderBy(s => s.givenName)
                        .Take(10)
                        .ToList();
                }

                // Get drep_ids from metadata
                var drepIds = metadataList.Select(m => m.drep_id).ToList();

                // Get voting power data for current epoch
                var votingPowerData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(v => v.epoch_no == currentEpoch && drepIds.Contains(v.drep_id))
                    .Select(v => new { v.drep_id, v.amount })
                    .ToListAsync();


                // Get delegators count
                var delegatorsData = await context.dreps_delegators
                    .AsNoTracking()
                    .Where(d => drepIds.Contains(d.drep_id))
                    .GroupBy(d => d.drep_id)
                    .Select(g => new { drep_id = g.Key, count = g.Count() })
                    .ToListAsync();


                // Perform in-memory join and pagination
                var drepResult = new List<DrepSearchDto>();
                foreach (var meta in metadataList)
                {
                    var drepId = meta.drep_id;
                    var votingPower = JsonUtils.ParseJsonDouble(votingPowerData.FirstOrDefault(v => v.drep_id == drepId)?.amount) ?? 0;
                    var delegatorCount = delegatorsData.FirstOrDefault(d => d.drep_id == drepId)?.count ?? 0;
                    double liveStake = Math.Round((double)(votingPower / 1e6), 0);

                    drepResult.Add(new DrepSearchDto
                    {
                        id = drepId,
                        title = meta.givenName,
                        url = $"/dreps/{drepId}",
                        delegator = delegatorCount,
                        live_stake = liveStake
                    });
                }

                var result = new SearchApiResponseDto
                {
                    charts = filteredCharts,
                    proposals = resultProposals,
                    dreps = drepResult,
                    pools = poolResult,
                    ccs = filteredCCs
                };

                _logger.LogInformation("Successfully retrieved search results");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving search data for term: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}