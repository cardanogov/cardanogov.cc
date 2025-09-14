using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class UtxoInfoData
    {
        public string? stake_address { get; set; }
        public string? tx_hash { get; set; }
        public int? tx_index { get; set; }
        public long? block_time { get; set; }
    }

    public class PoolService : IPoolService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PoolService> _logger;
        private readonly HttpClient _httpClient;
        const decimal trillion = 1_000_000_000_000_000m;
        const decimal million = 1_000_000m;

        public PoolService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<PoolService> logger, HttpClient httpClient)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Get total pool count - matching getTotalPool in poolApi.js
        /// </summary>
        public async Task<int?> GetTotalPoolAsync()
        {
            try
            {
                _logger.LogInformation("Getting total pool count from database");

                using var context = await _contextFactory.CreateDbContextAsync();
                var count = await context.pool_list
                    .AsNoTracking()
                    .CountAsync();

                _logger.LogInformation("Total pool count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total pool count");
                throw;
            }
        }

        /// <summary>
        /// Get totals for specific epoch - matching getTotals in poolApi.js
        /// </summary>
        public async Task<List<TotalInfoResponseDto>?> GetTotalsAsync(int epochNo)
        {
            try
            {
                _logger.LogInformation("Getting totals for epoch: {EpochNo}", epochNo);

                using var context = await _contextFactory.CreateDbContextAsync();
                var totals = await context.totals
                    .AsNoTracking()
                    .Where(t => t.epoch_no == epochNo)
                    .FirstOrDefaultAsync();

                if (totals == null)
                {
                    _logger.LogWarning("No totals found for epoch: {EpochNo}", epochNo);
                    return new List<TotalInfoResponseDto>();
                }

                // Return as single item list to match JavaScript format
                var result = new List<TotalInfoResponseDto>
                {
                    new TotalInfoResponseDto
                    {
                        epoch_no = totals.epoch_no,
                        circulation = totals.circulation,
                        treasury = totals.treasury,
                        reward = totals.reward,
                        supply = totals.supply,
                        reserves = totals.reserves,
                        fees = totals.fees,
                        deposits_stake = totals.deposits_stake,
                        deposits_drep = totals.deposits_drep,
                        deposits_proposal = totals.deposits_proposal
                    }
                };

                _logger.LogInformation("Successfully retrieved totals for epoch: {EpochNo}", epochNo);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting totals for epoch: {EpochNo}", epochNo);
                throw;
            }
        }

        /// <summary>
        /// POST pool metadata - matching postPoolMetadata in poolApi.js
        /// </summary>
        public async Task<object?> GetPoolMetadataAsync(string poolId)
        {
            try
            {
                _logger.LogInformation("Getting pool metadata for pool {pool}", poolId);

                using var context = await _contextFactory.CreateDbContextAsync();
                var pool = await context.pool_metadata
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.pool_id_bech32 == poolId);

                if (pool == null) return pool;

                var result = new
                {
                    pool_id_bech32 = pool.pool_id_bech32,
                    meta_url = JsonUtils.FormatJsonBField(pool.meta_url),
                    meta_hash = JsonUtils.FormatJsonBField(pool.meta_hash),
                    meta_json = JsonUtils.FormatJsonBField(pool.meta_json)
                };

                _logger.LogInformation("Successfully retrieved metadata for pool {pool}", poolId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool metadata");
                throw;
            }
        }

        /// <summary>
        /// Get pool stake snapshot - matching getPoolStakeSnapshot in poolApi.js
        /// </summary>
        public async Task<object?> GetPoolStakeSnapshotAsync(string poolBech32)
        {
            try
            {
                _logger.LogInformation("Getting pool stake snapshot for pool: {PoolBech32}", poolBech32);

                using var context = await _contextFactory.CreateDbContextAsync();
                var stakeSnapshots = await context.pool_stake_snapshot
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32)
                    .ToListAsync();

                // Return formatted data to match JavaScript format
                var result = stakeSnapshots.Select(s => new
                {
                    pool_id_bech32 = s.pool_id_bech32,
                    epoch_no = s.epoch_no,
                    active_stake = s.active_stake
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} stake snapshots for pool: {PoolBech32}", result.Count, poolBech32);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool stake snapshot for pool: {PoolBech32}", poolBech32);
                throw;
            }
        }

        /// <summary>
        /// Get SPO voting power history - matching getSpoVotingPowerHistory in poolApi.js
        /// </summary>
        public async Task<List<SpoVotingPowerHistoryResponseDto>?> GetSpoVotingPowerHistoryAsync()
        {
            try
            {
                _logger.LogInformation("Getting SPO voting power history");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get current epoch
                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No current epoch found");
                    return new List<SpoVotingPowerHistoryResponseDto>();
                }

                var epoch = currentEpoch.epoch_no ?? 0;

                // Get active pools with voting power (matching JavaScript filter)
                var poolsVotingPower = await context.pools_voting_power_history
                    .AsNoTracking()
                    .Where(p => p.epoch_no == epoch && p.amount != null)
                    .ToListAsync();

                // Get pool information for registered pools
                var poolInfo = await context.pool_list
                    .AsNoTracking()
                    .Where(p => p.pool_status == "registered" && p.active_stake != null)
                    .ToListAsync();

                // Calculate total stake for percentage calculations
                var totalStake = poolsVotingPower.Sum(p => JsonUtils.ParseJsonBDecimal(p.amount) ?? 0) / million;

                // Group and aggregate data by ticker (matching JavaScript groupAndSum logic)
                var groupedData = new Dictionary<string, SpoVotingPowerHistoryResponseDto>();

                foreach (var pool in poolsVotingPower)
                {
                    var poolData = poolInfo.FirstOrDefault(pi => pi.pool_id_bech32 == pool.pool_id_bech32);
                    if (poolData == null) continue;

                    var ticker = string.IsNullOrWhiteSpace(JsonUtils.FormatJsonBField(poolData.ticker)) ? "N/A" : JsonUtils.FormatJsonBField(poolData.ticker);
                    var amount = JsonUtils.ParseJsonBDecimal(pool.amount) ?? 0;
                    var amountInMillions = amount / million;

                    if (!groupedData.ContainsKey(ticker))
                    {
                        groupedData[ticker] = new SpoVotingPowerHistoryResponseDto
                        {
                            ticker = ticker,
                            active_stake = 0,
                            pool_status = poolData.pool_status,
                            percentage = 0,
                            group = JsonUtils.FormatJsonBField(poolData.pool_group)
                        };
                    }

                    groupedData[ticker].active_stake += (double)amountInMillions;
                }

                // Calculate percentages and format results
                var result = groupedData.Values.Select(item =>
                {
                    var percentage = 0.0;
                    if (item.active_stake > 0 && totalStake > 0)
                    {
                        var rawPercentage = (item.active_stake.Value / (double)totalStake) * 100;

                        // Match JavaScript precision logic
                        if (Math.Abs(rawPercentage) < 0.000001)
                        {
                            percentage = 0;
                        }
                        else if (Math.Abs(rawPercentage) < 0.01)
                        {
                            percentage = Math.Round(rawPercentage, 8);
                        }
                        else
                        {
                            percentage = rawPercentage < 0 ?
                                Math.Round(rawPercentage, 4) :
                                Math.Round(rawPercentage, 2);
                        }
                    }

                    return new SpoVotingPowerHistoryResponseDto
                    {
                        ticker = !string.IsNullOrWhiteSpace(item.ticker) ? item.ticker : "N/A",
                        active_stake = Math.Round(item.active_stake ?? 0),
                        pool_status = item.pool_status,
                        percentage = percentage,
                        group = JsonUtils.FormatJsonBField(item.group) == "" ? null : JsonUtils.FormatJsonBField(item.group)
                    };
                }).OrderByDescending(p => p.percentage).ToList();

                _logger.LogInformation("Successfully retrieved SPO voting power history with {Count} pools", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SPO voting power history");
                throw;
            }
        }

        /// <summary>
        /// Get ADA statistics - matching getAdaStatistics in poolApi.js
        /// </summary>
        public async Task<AdaStatisticsResponseDto?> GetAdaStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting ADA statistics");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get concurrent data for epochs >= 507 (matching JavaScript)
                var epochData = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no >= 507)
                    .OrderBy(e => e.epoch_no)
                    .ToListAsync();

                var drepData = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .Where(d => d.epoch_no >= 507)
                    .OrderBy(d => d.epoch_no)
                    .ToListAsync();

                var totalsData = await context.totals
                    .AsNoTracking()
                    .Where(t => t.epoch_no >= 507)
                    .OrderBy(t => t.epoch_no)
                    .ToListAsync();

                // Format pool result (convert to ADA and format as string)
                var poolResult = epochData.Select(e => new PoolResultDto
                {
                    epoch_no = e.epoch_no,
                    total_active_stake = (JsonUtils.ParseJsonBDecimal(e.active_stake) / million).ToString()
                }).ToList();

                // Format drep result (add epoch 507 with amount 0, matching JavaScript)
                var drepResult = new List<DrepResultDto>
                {
                    new DrepResultDto { epoch_no = 507, amount = "0" }
                };
                drepResult.AddRange(drepData.Select(d => new DrepResultDto
                {
                    epoch_no = d.epoch_no,
                    amount = (JsonUtils.ParseJsonBDecimal(d.amount) / million).ToString()
                }));

                // Format supply result (convert to ADA and format as string)
                var supplyResult = totalsData.Select(t => new SupplyResultDto
                {
                    epoch_no = t.epoch_no,
                    supply = (JsonUtils.ParseJsonBDecimal(t.supply) / million).ToString()
                }).ToList();

                var result = new AdaStatisticsResponseDto
                {
                    pool_result = poolResult,
                    drep_result = drepResult,
                    supply_result = supplyResult
                };

                _logger.LogInformation("Successfully retrieved ADA statistics - Pool: {PoolCount}, Drep: {DrepCount}, Supply: {SupplyCount}",
                    poolResult.Count, drepResult.Count, supplyResult.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ADA statistics");
                throw;
            }
        }

        /// <summary>
        /// Get ADA statistics percentage - matching getAdaStatisticsPercentage in poolApi.js
        /// </summary>
        public async Task<AdaStatisticsPercentageResponseDto?> GetAdaStatisticsPercentageAsync()
        {
            try
            {
                _logger.LogInformation("Getting ADA statistics percentage");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get current epoch
                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No current epoch found");
                    return null;
                }

                var lastEpoch = currentEpoch.epoch_no ?? 0;

                // Get concurrent data for current epoch (matching JavaScript Promise.all)
                var adaStakingData = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no == lastEpoch)
                    .Select(e => e.active_stake)
                    .FirstOrDefaultAsync();

                var adaRegisterToVoteData = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .Where(d => d.epoch_no == lastEpoch)
                    .Select(d => d.amount)
                    .FirstOrDefaultAsync();

                var circulatingSupplyData = await context.totals
                    .AsNoTracking()
                    .Where(t => t.epoch_no == lastEpoch)
                    .Select(t => t.supply)
                    .FirstOrDefaultAsync();

                var adaAbstainData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(d => d.drep_id == "drep_always_abstain" && d.epoch_no == lastEpoch)
                    .Select(d => d.amount)
                    .FirstOrDefaultAsync();


                // Parse values and convert to ADA (matching JavaScript logic)
                var adaStaking = (JsonUtils.ParseJsonBDecimal(adaStakingData) ?? 0) / million;
                var adaRegisterToVote = (JsonUtils.ParseJsonBDecimal(adaRegisterToVoteData) ?? 0) / million;
                var circulatingSupply = (JsonUtils.ParseJsonBDecimal(circulatingSupplyData) ?? 0) / million;
                var adaAbstain = (JsonUtils.ParseJsonBDecimal(adaAbstainData) ?? 0) / million;

                const decimal total = 45_000_000_000m; // 45 billion ADA total supply

                // Calculate percentages (matching JavaScript logic)
                var toPercentage = (decimal value) =>
                {
                    if (circulatingSupply == 0) return 0.0;
                    var percentage = (double)((value * 100) / circulatingSupply);
                    return Math.Round(percentage, 1);
                };

                var circulatingSupplyPercentage = (double)((circulatingSupply * 100) / total);

                var result = new AdaStatisticsPercentageResponseDto
                {
                    ada_staking = NumberUtils.FormatValue(adaStaking),
                    ada_staking_percentage = toPercentage(adaStaking),
                    ada_register_to_vote = NumberUtils.FormatValue(adaRegisterToVote),
                    ada_register_to_vote_percentage = toPercentage(adaRegisterToVote),
                    circulating_supply = NumberUtils.FormatValue(circulatingSupply),
                    circulating_supply_percentage = Math.Round(circulatingSupplyPercentage, 1),
                    ada_abstain = NumberUtils.FormatValue(adaAbstain),
                    ada_abstain_percentage = toPercentage(adaAbstain)
                };

                _logger.LogInformation("Successfully retrieved ADA statistics percentage for epoch: {Epoch}", lastEpoch);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ADA statistics percentage");
                throw;
            }
        }

        /// <summary>
        /// Get pool list with pagination - optimized version (fixed DbContext concurrency issue)
        /// </summary>
        public async Task<PoolResponseDto?> GetPoolListAsync(int page, int pageSize, string? status, string? search)
        {
            try
            {
                _logger.LogInformation("Getting pool list - Page: {Page}, PageSize: {PageSize}, Status: {Status}, Search: {Search}",
                    page, pageSize, status, search);
                pageSize = pageSize > 1000 ? 1000 : pageSize;

                var lowerStatus = status?.Trim().ToLower();
                var lowerSearch = search?.Trim().ToLower();

                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.pool_list.AsNoTracking()
                      .OrderBy(x => string.IsNullOrWhiteSpace(x.ticker) ? 1 : 0)
                     .ThenBy(p => p.pool_status != null && p.pool_status.ToLower() == "registered" ? 0 : 1)
                    .AsQueryable();

                // Apply status filter (matching JavaScript logic)
                if (!string.IsNullOrEmpty(lowerStatus))
                {
                    if (lowerStatus == "active")
                    {
                        query = query.Where(p => p.pool_status != null && p.pool_status.ToLower() == "registered");
                    }
                    else
                    {
                        query = query.Where(p => p.pool_status != null && p.pool_status.ToLower() != "registered");
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(lowerSearch))
                {
                    query = query.Where(p => (p.pool_id_bech32 != null && p.pool_id_bech32.ToLower().Contains(lowerSearch)) ||
                                           (p.ticker != null && p.ticker.ToLower().Contains(lowerSearch)));
                }

                // Get total count first
                var totalCount = await query.CountAsync();

                // Get current epoch
                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No current epoch found");
                    return new PoolResponseDto();
                }

                // Get paginated pools
                var pools = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!pools.Any())
                {
                    return new PoolResponseDto
                    {
                        items = new List<PoolDto>(),
                        pageNumber = page,
                        pageSize = pageSize,
                        total = totalCount
                    };
                }

                var poolIds = pools.Select(p => p.pool_id_bech32).ToList();

                // Execute database queries sequentially to avoid DbContext concurrency issues
                var poolHistories = await context.pools_voting_power_history
                    .AsNoTracking()
                    .Where(p => p.epoch_no == currentEpoch.epoch_no && poolIds.Contains(p.pool_id_bech32))
                    .Select(p => new
                    {
                        pool_id_bech32 = p.pool_id_bech32,
                        voting_amount = p.amount
                    })
                    .ToListAsync();

                var activitiesGrouped = await context.vote_list
                    .AsNoTracking()
                    .Where(p => poolIds.Contains(p.voter_id))
                    .GroupBy(p => p.voter_id)
                    .Select(g => new
                    {
                        pool_id_bech32 = g.Key,
                        vote = g.Count(),
                        max_block_time = g.Max(p => p.block_time)
                    })
                    .ToListAsync();

                // Fetch references separately to avoid EF Core translation issues
                var votesWithReferences = await context.vote_list
                    .AsNoTracking()
                    .Where(p => poolIds.Contains(p.voter_id))
                    .ToListAsync();

                var referencesGrouped = votesWithReferences
                    .GroupBy(p => p.voter_id)
                    .Select(g => new
                    {
                        voter_id = g.Key,
                        proposal_ids = g.Select(p => p.proposal_id).ToList()
                    })
                    .ToDictionary(g => g.voter_id!, g => g.proposal_ids);

                var delegatorGrouped = await context.pool_delegators
                    .AsNoTracking()
                    .Where(p => poolIds.Contains(p.pool_id_bech32) && p.stake_address != "")
                    .GroupBy(p => p.pool_id_bech32)
                    .Select(g => new
                    {
                        pool_id_bech32 = g.Key,
                        delegator = g.Count()
                    })
                    .ToListAsync();

                var poolMetas = await context.pool_metadata
                    .Where(p => poolIds.Contains(p.pool_id_bech32) && p.meta_json != null)
                    .Select(p => new { p.pool_id_bech32, meta_json = p.meta_json })
                    .Distinct()
                    .ToListAsync();

                var metaDictionary = poolMetas
                       .Where(x => x.pool_id_bech32 != null)
                       .ToDictionary(x => x.pool_id_bech32!, x => JsonUtils.ParseHomepage(x.meta_json));

                // Fetch all proposal metadata in one query to avoid concurrent DbContext operations
                var allProposalIds = referencesGrouped.Values.SelectMany(x => x).Distinct().ToList();
                var proposalMetaData = await context.voters_proposal_list
                    .AsNoTracking()
                    .Where(v => v.proposal_id != null && allProposalIds.Contains(v.proposal_id))
                    .Select(v => new { v.proposal_id, v.meta_json })
                    .ToListAsync();

                var proposalMetaDict = proposalMetaData
                    .GroupBy(x => x.proposal_id)
                    .ToDictionary(g => g.Key!, g => g.Select(x => x.meta_json).ToList());

                // Build result
                var result = new PoolResponseDto
                {
                    items = pools.Select(p =>
                    {
                        var proposalIds = referencesGrouped.TryGetValue(p.pool_id_bech32!, out var refs) ? refs : new List<string>();
                        var metaJsons = proposalIds
                            .Where(id => proposalMetaDict.ContainsKey(id))
                            .SelectMany(id => proposalMetaDict[id])
                            .ToList();

                        //var references = JsonUtils.ParseJsonBReferencesList(metaJsons);
                        var voting_power = double.TryParse(JsonUtils.FormatJsonBField(poolHistories.FirstOrDefault(ph => ph.pool_id_bech32 == p.pool_id_bech32)?.voting_amount), out double power) ? power / 1000000 : 0;
                        var block_time = activitiesGrouped.FirstOrDefault(a => a.pool_id_bech32 == p.pool_id_bech32)?.max_block_time ?? 0;

                        return new PoolDto
                        {
                            ticker = JsonUtils.FormatJsonBField(p.ticker),
                            status = p.pool_status?.ToLower() == "registered" ? "Active" : "Inactive",
                            active_stake = (double?)(JsonUtils.ParseJsonBDecimal(p.active_stake) / million),
                            pool_id_bech32 = p.pool_id_bech32,
                            active_epoch_no = JsonUtils.ParseJsonInt(p.active_epoch_no),
                            meta_url = JsonUtils.FormatJsonBField(p.meta_url),
                            delegator = delegatorGrouped.FirstOrDefault(d => d.pool_id_bech32 == p.pool_id_bech32)?.delegator ?? 0,
                            voting_amount = voting_power,
                            block_time = block_time,
                            vote = votesWithReferences.Count(a => a.voter_id == p.pool_id_bech32),
                            homepage = metaDictionary.TryGetValue(p.pool_id_bech32!, out var homepage) ? homepage : null,
                            pool_status = p.pool_status,
                            //references = references,
                        };
                    }).ToList(),
                    pageNumber = page,
                    pageSize = pageSize,
                    total = totalCount,
                };

                _logger.LogInformation("Successfully retrieved pool list with {Count} pools", result.items.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool list");
                throw;
            }
        }

        /// <summary>
        /// Get pool information by pool_bech32 - matching getPoolInfo in poolApi.js
        /// </summary>
        public async Task<PoolInfoDto?> GetPoolInfoAsync(string poolBech32)
        {
            try
            {
                _logger.LogInformation("Getting pool info for: {PoolBech32}", poolBech32);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Initialize poolInfo with default values
                var poolInfo = new PoolInfoDto
                {
                    ticker = "",
                    pool_id_bech32 = poolBech32,
                    voting_power = new List<VotingPowerDto> { new VotingPowerDto { epoch_no = 0, amount = 0 } },
                    status = new PoolStatusDto
                    {
                        registration = 0,
                        last_activity = 0,
                        status = ""
                    },
                    information = new PoolInformationDto
                    {
                        description = "",
                        name = "",
                        ticker = "",
                        live_stake = 0,
                        deposit = 0,
                        margin = 0,
                        fixed_cost = 0,
                        active_epoch_no = 0,
                        block_count = 0,
                        created = 0,
                        delegators = 0
                    },
                    vote_info = new List<VoteInfoDto>
                    {
                        new VoteInfoDto
                        {
                            proposal_id = "",
                            title = "",
                            proposal_type = "",
                            block_time = 0,
                            vote = "",
                            meta_url = ""
                        }
                    },
                    delegation = new List<DelegationDto>
                    {
                        new DelegationDto
                        {
                            stake_address = "",
                            amount = 0,
                            latest_delegation_tx_hash = "",
                            block_time = 0
                        }
                    },
                    registration = new List<RegistrationDto>
                    {
                        new RegistrationDto
                        {
                            block_time = 0,
                            ticker = "",
                            meta_url = ""
                        }
                    }
                };

                // Gộp pool_updates
                var poolUpdates = await context.pool_updates
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32)
                    .ToListAsync();

                var statusRegistration = poolUpdates
                    .Where(p => p.update_type == "registration")
                    .OrderBy(p => p.block_time)
                    .FirstOrDefault();

                var registrationData = poolUpdates
                    .Select(p => new { p.block_time, p.meta_url, p.meta_json })
                    .ToList();

                // Gộp vote_list
                var voteList = await context.vote_list
                    .AsNoTracking()
                    .Where(v => v.voter_id == poolBech32)
                    .ToListAsync();

                var lastActivity = voteList
                    .OrderByDescending(v => v.block_time)
                    .Select(v => new { v.block_time })
                    .FirstOrDefault();

                var voteInfoData = voteList
                    .Select(v => new { v.meta_json, v.proposal_type, v.block_time, v.meta_url, v.proposal_id })
                    .ToList();

                var voteListData = voteList
                    .Select(v => new { v.block_time, v.vote, v.voter_id })
                    .ToList();

                // 1. Get pool status
                var poolListData = await context.pool_list
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32)
                    .Select(p => new { p.pool_status, p.pool_id_bech32 })
                    .FirstOrDefaultAsync();

                // 2. Get pool information
                var poolData = await context.pool_information
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32)
                    .FirstOrDefaultAsync();

                // 3. Get voting power history
                var votingPowerData = await context.pools_voting_power_history
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32 && p.amount != null)
                    .OrderByDescending(p => p.epoch_no)
                    .ToListAsync();

                // 4. Get delegators
                var delegatorsData = await context.pool_delegators
                    .AsNoTracking()
                    .Where(p => p.pool_id_bech32 == poolBech32)
                    .ToListAsync();

                // Assign status data
                poolInfo.status.status = poolListData?.pool_status == "registered" ? "Active" : "Inactive";
                poolInfo.status.registration = (int)(statusRegistration?.block_time ?? 0);
                poolInfo.status.last_activity = (int)(lastActivity?.block_time ?? 0);

                // Assign information data
                if (poolData != null)
                {
                    var ticker = JsonUtils.ParseJsonBString(poolData.meta_json, "ticker");
                    var description = JsonUtils.ParseJsonBString(poolData.meta_json, "description");
                    var name = JsonUtils.ParseJsonBString(poolData.meta_json, "name");

                    poolInfo.ticker = ticker ?? "";
                    poolInfo.information = new PoolInformationDto
                    {
                        description = description ?? "",
                        name = name ?? "",
                        ticker = ticker ?? "",
                        live_stake = Math.Round((double.TryParse(poolData.active_stake, out double active_stake) ? active_stake / (double)million : 0)),
                        deposit = Math.Round((double.TryParse(poolData.pledge, out double pledge) ? pledge / (double)million : 0)),
                        margin = poolData.margin,
                        fixed_cost = Math.Round((double.TryParse(poolData.fixed_cost, out double fixed_cost) ? fixed_cost / (double)million : 0)),
                        active_epoch_no = poolData.active_epoch_no,
                        block_count = (int)(poolData.block_count ?? 0),
                        delegators = poolData.live_delegators ?? 0,
                        created = 0 // Will be set below if active_epoch_no > 0
                    };

                    // Get created time from epoch start time   
                    if (poolInfo.information.active_epoch_no > 0)
                    {
                        var epochInfo = await context.epoch
                            .AsNoTracking()
                            .Where(e => e.epoch_no == poolInfo.information.active_epoch_no)
                            .Select(e => new { e.start_time })
                            .FirstOrDefaultAsync();

                        poolInfo.information.created = (int)(epochInfo?.start_time ?? 0);
                    }
                }

                // Assign voting power
                if (votingPowerData.Any())
                {
                    poolInfo.voting_power = votingPowerData
                        .Where(item => Math.Round((double)(JsonUtils.ParseJsonBDecimal(item.amount) ?? 0) / (double)million) > 0)
                        .Select(item => new VotingPowerDto
                        {
                            epoch_no = item.epoch_no ?? 0,
                            amount = Math.Round((double)(JsonUtils.ParseJsonBDecimal(item.amount) ?? 0) / (double)million)
                        })
                        .ToList();
                }

                // Assign vote info
                if (voteInfoData.Any())
                {
                    // Get proposal details from voters_proposal_list
                    var proposalIds = voteInfoData.Select(v => v.proposal_id).Distinct().ToList();
                    var proposalDetails = await context.voters_proposal_list
                        .AsNoTracking()
                        .Where(v => proposalIds.Contains(v.proposal_id))
                        .ToListAsync();

                    poolInfo.vote_info = voteInfoData.Select(item =>
                    {
                        var proposal = proposalDetails.FirstOrDefault(p => p.proposal_id == item.proposal_id);

                        return new VoteInfoDto
                        {
                            proposal_id = item.proposal_id ?? "",
                            title = JsonUtils.ParseJsonBTitle(proposal?.meta_json),
                            proposal_type = item.proposal_type ?? "",
                            block_time = (int)(item.block_time ?? 0),
                            vote = "", // Changed from "" to 0
                            meta_url = item.meta_url ?? ""
                        };
                    }).ToList();

                    // Assign votes
                    foreach (var voteInfoItem in poolInfo.vote_info)
                    {
                        var matchingVote = voteListData.FirstOrDefault(v => v.block_time == voteInfoItem.block_time);
                        if (matchingVote != null)
                        {
                            voteInfoItem.vote = matchingVote.vote;
                        }
                    }
                }

                // Assign delegation
                if (delegatorsData.Any())
                {
                    poolInfo.delegation = delegatorsData.Select(item => new DelegationDto
                    {
                        stake_address = item.stake_address ?? "",
                        amount = Math.Round((double)(JsonUtils.ParseJsonBDecimal(item.amount) ?? 0) / (double)million),
                        latest_delegation_tx_hash = item.latest_delegation_tx_hash ?? "",
                        block_time = 0
                    }).ToList();

                    // Get block times for delegations from utxo_info
                    var utxoRefs = poolInfo.delegation
                        .Where(d => !string.IsNullOrEmpty(d.latest_delegation_tx_hash))
                        .Select(d => d.latest_delegation_tx_hash)
                        .ToList();

                    if (utxoRefs.Any())
                    {
                        var allUtxoData = await context.utxo_info
                            .AsNoTracking()
                            .Where(u => u.stake_address != null && u.tx_hash != null && utxoRefs.Contains(u.tx_hash))
                            .Select(u => new { u.stake_address, u.tx_hash, u.tx_index, u.block_time })
                            .ToListAsync();

                        // Match block times
                        foreach (var delegation in poolInfo.delegation)
                        {
                            var matching = allUtxoData.FirstOrDefault(u =>
                                u.tx_hash == delegation.latest_delegation_tx_hash && JsonUtils.FormatJsonBField(u.stake_address) == JsonUtils.FormatJsonBField(delegation.stake_address));
                            if (matching != null)
                            {
                                delegation.block_time = (int)(matching.block_time ?? 0);
                            }
                        }
                    }
                }

                // Assign registration history
                if (registrationData.Any())
                {
                    poolInfo.registration = registrationData.Select(item =>
                    {
                        var ticker = JsonUtils.ParseJsonBPoolTicker(item.meta_json) ?? "N/A";
                        return new RegistrationDto
                        {
                            block_time = (int)(item.block_time ?? 0),
                            ticker = ticker,
                            meta_url = JsonUtils.FormatJsonBField(item.meta_url) ?? ""
                        };
                    }).ToList();
                }

                _logger.LogInformation("Successfully retrieved pool info for: {PoolBech32}", poolBech32);
                return poolInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool info for: {PoolBech32}", poolBech32);
                throw;
            }
        }

        /// <summary>
        /// Helper method to get epoch start time
        /// </summary>
        private async Task<int> GetEpochStartTime(int epochNo)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var epoch = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no == epochNo)
                    .FirstOrDefaultAsync();

                return (int)(epoch?.start_time ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<DelegationResponseDto?> GetPoolDelegationAsync(string poolId, int page = 1, int pageSize = 20, string? sortBy = null, string? sortOrder = null)
        {
            try
            {
                _logger.LogInformation("Getting pool delegation for poolId: {PoolId}, Page: {Page}, PageSize: {PageSize}", poolId, page, pageSize);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get total count
                var totalCount = await context.pool_delegators
                    .AsNoTracking()
                    .Where(pd => pd.pool_id_bech32 == poolId)
                    .CountAsync();

                if (totalCount == 0)
                {
                    _logger.LogInformation("No delegators found for poolId: {PoolId}", poolId);
                    return new DelegationResponseDto
                    {
                        items = new List<DelegationDto>(),
                        total = 0,
                        pageNumber = page,
                        pageSize = pageSize
                    };
                }

                // Build query
                var query = context.pool_delegators
                    .AsNoTracking()
                    .Where(pd => pd.pool_id_bech32 == poolId);

                // Apply sorting
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy.ToLower())
                    {
                        case "amount":
                            query = sortOrder?.ToLower() == "desc"
                                ? query.OrderByDescending(pd => pd.amount)
                                : query.OrderBy(pd => pd.amount);
                            break;
                        case "stake_address":
                            query = sortOrder?.ToLower() == "desc"
                                ? query.OrderByDescending(pd => pd.stake_address)
                                : query.OrderBy(pd => pd.stake_address);
                            break;
                        default:
                            // Default sort by amount descending
                            query = query.OrderByDescending(pd => pd.amount);
                            break;
                    }
                }
                else
                {
                    // Default sort by amount descending
                    query = query.OrderByDescending(pd => pd.amount);
                }

                // Apply pagination
                var delegatorsData = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} delegators from database for poolId: {PoolId}", delegatorsData.Count, poolId);

                // Get block times for delegations from utxo_info
                var utxoRefs = delegatorsData
                    .Where(d => !string.IsNullOrEmpty(d.latest_delegation_tx_hash))
                    .Select(d => d.latest_delegation_tx_hash)
                    .ToList();

                var allUtxoData = new List<UtxoInfoData>();
                if (utxoRefs.Any())
                {
                    allUtxoData = await context.utxo_info
                        .AsNoTracking()
                        .Where(u => u.stake_address != null && u.tx_hash != null && utxoRefs.Contains(u.tx_hash))
                        .Select(u => new UtxoInfoData
                        {
                            stake_address = u.stake_address,
                            tx_hash = u.tx_hash,
                            tx_index = u.tx_index,
                            block_time = u.block_time
                        })
                        .ToListAsync();
                }

                // Map to DTOs
                var delegationItems = delegatorsData.Select(item =>
                {
                    var delegation = new DelegationDto
                    {
                        stake_address = item.stake_address ?? "",
                        amount = Math.Round((double)(JsonUtils.ParseJsonBDecimal(item.amount) ?? 0) / (double)million),
                        latest_delegation_tx_hash = item.latest_delegation_tx_hash ?? "",
                        block_time = 0
                    };

                    // Match block time
                    if (!string.IsNullOrEmpty(delegation.latest_delegation_tx_hash))
                    {
                        var matching = allUtxoData.FirstOrDefault(u =>
                            u.tx_hash == delegation.latest_delegation_tx_hash &&
                            JsonUtils.FormatJsonBField(u.stake_address) == JsonUtils.FormatJsonBField(delegation.stake_address));
                        if (matching != null)
                        {
                            delegation.block_time = (int)(matching.block_time ?? 0);
                        }
                    }

                    return delegation;
                }).ToList();

                var result = new DelegationResponseDto
                {
                    items = delegationItems,
                    total = totalCount,
                    pageNumber = page,
                    pageSize = pageSize
                };

                _logger.LogInformation("Successfully retrieved delegation data for poolId: {PoolId}, Total: {Total}, Page: {Page}",
                    poolId, totalCount, page);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pool delegation for poolId: {PoolId}", poolId);
                throw;
            }
        }
    }
}