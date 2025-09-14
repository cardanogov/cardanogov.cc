using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class DrepService : IDrepService
    {
        IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<DrepService> _logger;
        const decimal trillion = 1_000_000_000_000_000m;
        const decimal million = 1_000_000m;

        public DrepService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<DrepService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int?> GetTotalDrepAsync()
        {
            try
            {
                _logger.LogInformation("Getting total DRep count from database");
                using var context = await _contextFactory.CreateDbContextAsync();
                var count = await context.dreps_list.AsNoTracking().CountAsync();
                _logger.LogInformation("Total DRep count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total drep count");
                throw;
            }
        }

        public async Task<DrepInfoResponseDto?> GetDrepInfoAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep info for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var drepInfo = await context.dreps_info
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.drep_id == drepId);

                return new DrepInfoResponseDto
                {
                    drep_id = drepInfo.drep_id,
                    hex = drepInfo.hex,
                    has_script = drepInfo.has_script,
                    registered = drepInfo.registered,
                    deposit = JsonUtils.FormatJsonBField(drepInfo.deposit, _logger),
                    active = drepInfo.active,
                    expires_epoch_no = !string.IsNullOrEmpty(drepInfo.expires_epoch_no) && int.TryParse(JsonUtils.FormatJsonBField(drepInfo.expires_epoch_no, _logger), out var epoch) ? epoch : null,
                    amount = drepInfo.amount,
                    meta_url = JsonUtils.FormatJsonBField(drepInfo.meta_url, _logger),
                    meta_hash = JsonUtils.FormatJsonBField(drepInfo.meta_hash, _logger)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep info for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<DrepListResponseDto?> GetDrepListAsync(int page, string? search, string? status)
        {
            try
            {
                _logger.LogInformation("Getting DRep list - Page: {Page}, Search: {Search}, Status: {Status}", page, search, status);

                var pageSize = 12;
                var offset = (page - 1) * pageSize;

                using var context = await _contextFactory.CreateDbContextAsync();
                // Get current epoch for calculations
                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .Select(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == 0)
                {
                    _logger.LogWarning("No epoch data found");
                    return new DrepListResponseDto
                    {
                        total_dreps = 0,
                        drep_info = new List<DrepListDto>()
                    };
                }

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
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    metadataList = metadataList
                        .Where(m => m.givenName.ToLower().Contains(search) || m.drep_id.ToLower().Contains(search))
                        .ToList();
                }

                if (!metadataList.Any())
                {
                    return new DrepListResponseDto
                    {
                        total_dreps = 0,
                        drep_info = new List<DrepListDto>()
                    };
                }

                // Get drep_ids from metadata
                var drepIds = metadataList.Select(m => m.drep_id).ToList();

                // Query dreps_info for status filtering with additional fields
                var drepQuery = context.dreps_info.AsNoTracking()
                    .Where(d => d.drep_id != null && drepIds.Contains(d.drep_id))
                    .OrderBy(d => d.active.HasValue && d.active.Value ? 0 : 1)
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var isActive = status.ToLower() == "active";
                    drepQuery = drepQuery.Where(d => d.active == isActive);
                }

                // Fetch filtered drep_info with additional fields
                var drepList = await drepQuery
                    .Select(d => new
                    {
                        drep_id = d.drep_id,
                        active = d.active,
                        expires_epoch_no = d.expires_epoch_no
                    })
                    .ToListAsync();

                if (!drepList.Any())
                {
                    return new DrepListResponseDto
                    {
                        total_dreps = 0,
                        drep_info = new List<DrepListDto>()
                    };
                }

                // Get voting power data for current epoch
                var votingPowerData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(v => v.epoch_no == currentEpoch && drepIds.Contains(v.drep_id))
                    .Select(v => new { v.drep_id, v.amount })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} voting power records for {DrepCount} DReps", votingPowerData.Count, drepIds.Count);

                // Get delegators count
                var delegatorsData = await context.dreps_delegators
                    .AsNoTracking()
                    .Where(d => drepIds.Contains(d.drep_id))
                    .GroupBy(d => d.drep_id)
                    .Select(g => new { drep_id = g.Key, count = g.Count() })
                    .ToListAsync();

                // Get times voted count
                var timesVotedData = await context.vote_list
                    .AsNoTracking()
                    .Where(v => drepIds.Contains(v.voter_id))
                    .GroupBy(v => v.voter_id)
                    .Select(g => new { drep_id = g.Key, count = g.Count() })
                    .ToListAsync();

                // Perform in-memory join, ordering, and pagination per requirements
                var result = drepList
                    .Join(metadataList,
                        drep => drep.drep_id,
                        meta => meta.drep_id,
                        (drep, meta) => new
                        {
                            drep_id = drep.drep_id,
                            name = meta.givenName,
                            active = drep.active,
                            expires_epoch_no = drep.expires_epoch_no,
                            references = meta.references,
                            image = meta.image
                        })
                    // Primary: by givenName A-Z, but "N/A" should be at the end
                    .OrderBy(x => string.Equals(x.name, "N/A", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase)
                    // Secondary: by active status (active first)
                    .ThenByDescending(x => x.active == true)
                    .Skip(offset)
                    .Take(pageSize)
                    .ToList();

                // Map to DrepListDto with all fields
                var drepInfoList = new List<DrepListDto>();
                foreach (var item in result)
                {
                    var activeUntil = await CalculateActiveUntil(item.expires_epoch_no, currentEpoch);

                    // Parse references as List<object>
                    var contactList = new List<object>();
                    try
                    {
                        var referencesStr = JsonUtils.ParseJsonBReferences(item.references, _logger);
                        if (!string.IsNullOrEmpty(referencesStr))
                        {
                            var references = JsonUtils.ParseJsonList(referencesStr);
                            contactList = references ?? new List<object>();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse references for drep_id: {DrepId}", item.drep_id);
                    }

                    drepInfoList.Add(new DrepListDto
                    {
                        drep_id = item.drep_id,
                        name = item.name,
                        status = item.active == true ? "Active" : "Inactive",
                        active_until = activeUntil,
                        contact = JsonConvert.SerializeObject(contactList),
                        image = JsonUtils.ParseJsonBImage(item.image, _logger),
                        voting_power = GetVotingPower(votingPowerData.Cast<object>().ToList(), item.drep_id),
                        delegators = GetDelegatorsCount(delegatorsData.Cast<object>().ToList(), item.drep_id),
                        times_voted = GetTimesVotedCount(timesVotedData.Cast<object>().ToList(), item.drep_id)
                    });
                }

                // Calculate total count for pagination
                var totalCount = drepList.Count;

                return new DrepListResponseDto
                {
                    total_dreps = totalCount,
                    drep_info = drepInfoList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DRep list");
                throw;
            }
        }

        public async Task<TotalDrepResponseDto?> GetTotalStakeNumbersAsync()
        {
            try
            {
                _logger.LogInformation("Getting total stake numbers");

                using var context = await _contextFactory.CreateDbContextAsync();
                // Lấy epoch mới nhất
                // Gộp truy vấn để lấy resData và maxEpoch
                var resData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(h => h.epoch_no == context.dreps_voting_power_history.Max(h2 => h2.epoch_no))
                    .ToListAsync();

                // Lấy epoch summary
                var epochSummary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .Where(e => e.epoch_no == context.dreps_epoch_summary.Max(e2 => e2.epoch_no))
                    .FirstOrDefaultAsync();

                var activeVotes = resData.Where(d => d.drep_id != null && d.drep_id.StartsWith("drep1")).ToList();
                var totalActive = SumFilteredAmounts(activeVotes.Select(s => s.amount).ToList());
                var totalNoConfidence = SumFilteredAmounts(resData.Where(d => d.drep_id == "drep_always_no_confidence").Select(s => s.amount).ToList());
                var totalAbstain = SumFilteredAmounts(resData.Where(d => d.drep_id == "drep_always_abstain").Select(s => s.amount).ToList());

                decimal totalRegisterVote = 0;
                if (epochSummary != null)
                {
                    var amount = JsonUtils.ParseJsonBDecimal(epochSummary.amount);
                    if (amount.HasValue)
                        totalRegisterVote = amount.Value;
                }

                // chart_stats: top 10 activeVotes, chia cho 1 triệu
                var chartStats = activeVotes
                    .Take(10)
                    .Select(v =>
                    {
                        var amount = JsonUtils.ParseJsonBDecimal(v.amount);
                        if (amount.HasValue)
                            return (int)(amount.Value / million);
                        return 0;
                    })
                    .ToList();

                return new TotalDrepResponseDto
                {
                    total_active = NumberUtils.DivideAndTruncate(totalActive.ToString(), trillion, 1),
                    total_no_confidence = NumberUtils.DivideAndTruncate(totalNoConfidence.ToString(), trillion, 2),
                    total_abstain = NumberUtils.DivideAndTruncate(totalAbstain.ToString(), trillion, 1),
                    total_register = NumberUtils.DivideAndTruncate(totalRegisterVote.ToString(), trillion, 1),
                    chart_stats = chartStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stake numbers");
                throw;
            }
        }

        public async Task<List<DrepVotingPowerHistoryResponseDto>?> GetDrepVotingPowerHistoryAsync()
        {
            try
            {
                _logger.LogInformation("Getting top DRep voting power");
                using var context = await _contextFactory.CreateDbContextAsync();
                var epoch = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .MaxAsync(h => (int?)h.epoch_no);

                if (!epoch.HasValue)
                {
                    _logger.LogWarning("No epoch summary found");
                    return new List<DrepVotingPowerHistoryResponseDto>();
                }

                // Fetch all powers for total calculation
                var allPowersForTotal = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(s => s.epoch_no == epoch && s.drep_id != null)
                    .Select(s => s.amount)
                    .ToListAsync();

                // Calculate total vote including all DReps
                var totalRegisterVote = allPowersForTotal.Sum(d => JsonUtils.ParseJsonBDecimal(d)) ?? -1;
                if (totalRegisterVote == 0)
                {
                    _logger.LogWarning("Total registered vote is zero");
                    return new List<DrepVotingPowerHistoryResponseDto>();
                }

                // Fetch filtered powers for processing
                var allPowers = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(s => s.epoch_no == epoch
                        && s.drep_id != null
                        && s.drep_id != "drep_always_no_confidence"
                        && s.drep_id != "drep_always_abstain")
                    .Select(s => new { s.drep_id, s.amount })
                    .ToListAsync();

                if (!allPowers.Any())
                {
                    _logger.LogWarning("No valid DRep voting power data found");
                    return new List<DrepVotingPowerHistoryResponseDto>();
                }

                const decimal million = 1_000_000;
                const decimal amountThreshold = 100_000_000_000m / million;

                // Process powers with metadata
                var powers = allPowers
                    .Select(s => new DrepVotingPowerHistoryResponseDto
                    {
                        amount = (double?)(JsonUtils.ParseJsonBDecimal(s.amount) / million),
                        drep_id = s.drep_id,
                        givenName = s.drep_id,
                        percentage = (double?)(JsonUtils.ParseJsonBDecimal(s.amount) / totalRegisterVote * 100)
                    })
                    .GroupBy(x => x.amount < (double)amountThreshold ? "Other" : x.drep_id)
                    .Select(g => new DrepVotingPowerHistoryResponseDto
                    {
                        amount = g.Key == "Other" ? g.Sum(x => x.amount) : g.First().amount,
                        drep_id = g.Key,
                        givenName = g.Key == "Other" ? "Other" : g.First().givenName,
                        percentage = g.Key == "Other"
                            ? Math.Round((double)(g.Sum(x => x.amount) / (double)totalRegisterVote * 100 ?? 0), 5)
                            : Math.Round((double)(g.First().percentage ?? 0), 5)
                    })
                    .OrderByDescending(x => x.amount)
                    .ToList();

                // Fetch metadata only for non-Other DReps
                var drepIds = powers.Where(p => p.drep_id != "Other").Select(p => p.drep_id).ToList();
                var allMetadata = await context.dreps_metadata
                    .AsNoTracking()
                    .Where(d => drepIds.Contains(d.drep_id))
                    .ToDictionaryAsync(d => d.drep_id!, d => JsonUtils.ParseGivenName(d.meta_json) ?? d.drep_id);

                // Update givenName efficiently
                foreach (var drep in powers.Where(p => p.drep_id != "Other"))
                {
                    if (drep.drep_id == null) continue;
                    if (allMetadata.TryGetValue(drep.drep_id, out var givenName))
                    {
                        if (givenName == drep.drep_id)
                        {
                            drep.givenName = $"{drep.drep_id[..6]}...{drep.drep_id[^4..]}";
                        }
                        else
                        {
                            drep.givenName = givenName;
                        }
                    }
                    else
                    {
                        drep.givenName = $"{drep.drep_id[..6]}...{drep.drep_id[^4..]}";
                    }
                }

                return powers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DRep voting power");
                throw;
            }
        }

        public async Task<double> GetDrepEpochSummaryAsync(int epochNo)
        {
            try
            {
                _logger.LogInformation("Getting DRep epoch summary for epoch: {EpochNo}", epochNo);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query the dreps_epoch_summary table
                var summary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(s => s.epoch_no)
                    .FirstOrDefaultAsync();

                if (summary != null && epochNo < summary.epoch_no)
                {
                    summary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.epoch_no == epochNo);
                }

                if (summary == null)
                {
                    _logger.LogWarning("No epoch summary found for epoch: {EpochNo}", epochNo);
                    return 0;
                }

                // Return the summary data (you may need to adjust based on actual table structure)
                return (double)NumberUtils.DivideAndTruncate(summary.amount, trillion, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep epoch summary for epoch: {EpochNo}", epochNo);
                throw;
            }
        }

        public async Task<DrepMetadataResponseDto?> GetDrepMetadataAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep metadata for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query the dreps_metadata table for the provided drep IDs
                var metadata = await context.dreps_metadata
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.drep_id == drepId);

                if (metadata == null)
                {
                    _logger.LogWarning("No metadata found for drepId: {DrepId}", drepId);
                    return null;
                }

                var result = new DrepMetadataResponseDto
                {
                    drep_id = metadata.drep_id ?? "",
                    hex = metadata.hex,
                    has_script = metadata.has_script,
                    meta_url = JsonUtils.FormatJsonBField(metadata.meta_url, _logger),
                    meta_hash = JsonUtils.FormatJsonBField(metadata.meta_hash, _logger),
                    meta_json = JsonUtils.FormatJsonBField(metadata.meta_json, _logger),
                    bytes = JsonUtils.FormatJsonBField(metadata.bytes, _logger),
                    warning = metadata.warning,
                    language = JsonUtils.FormatJsonBField(metadata.language, _logger),
                    comment = metadata.comment,
                    is_valid = metadata.is_valid
                };

                _logger.LogInformation("Successfully retrieved metadata for drepId: {DrepId}", drepId);
                _logger.LogInformation("Formatted meta_url: {MetaUrl}", result.meta_url);
                _logger.LogInformation("Formatted meta_hash: {MetaHash}", result.meta_hash);
                _logger.LogInformation("Formatted language: {Language}", result.language);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep metadata for drep id: {drepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepDelegatorsResponseDto>?> GetDrepDelegatorsAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep delegators for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query the dreps_delegators table
                var delegators = await context.dreps_delegators
                    .AsNoTracking()
                    .Where(d => d.drep_id == drepId)
                    .ToListAsync();

                var result = delegators.Select(d => new DrepDelegatorsResponseDto
                {
                    stake_address = d.stake_address,
                    stake_address_hex = d.stake_address_hex,
                    script_hash = JsonUtils.FormatJsonBField(d.script_hash),
                    epoch_no = d.epoch_no,
                    amount = d.amount
                }).ToList();

                _logger.LogInformation("Found {Count} delegators for drepId: {DrepId}", delegators?.Count ?? 0, drepId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep delegators for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<DrepHistoryResponseDto?> GetDrepHistoryAsync(int epochNo, string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep history for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var history = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.drep_id == drepId && d.epoch_no == epochNo);

                if (history == null)
                {
                    _logger.LogWarning("No history found for drepId: {DrepId} in epoch: {EpochNo}", drepId, epochNo);
                    return null;
                }

                var result = new DrepHistoryResponseDto
                {
                    drep_id = history.drep_id,
                    epoch_no = history.epoch_no,
                    amount = JsonUtils.FormatJsonBField(history.amount)
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep history for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepsUpdatesResponseDto>?> GetDrepUpdatesAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep updates for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var updates = await context.dreps_updates
                    .AsNoTracking()
                    .Where(d => d.drep_id == drepId)
                    .ToListAsync();

                if (updates == null)
                {
                    _logger.LogWarning("No history found for drepId: {DrepId}", drepId);
                    return null;
                }

                var result = updates.Select(u => new DrepsUpdatesResponseDto
                {
                    action = u.action,
                    drep_id = u.drep_id,
                    hex = u.hex,
                    has_script = u.has_script,
                    update_tx_hash = u.update_tx_hash,
                    cert_index = u.cert_index,
                    block_time = u.block_time,
                    deposit = JsonUtils.FormatJsonBField(u.deposit, _logger),
                    meta_url = JsonUtils.FormatJsonBField(u.meta_url, _logger),
                    meta_hash = JsonUtils.FormatJsonBField(u.meta_hash, _logger),
                    meta_json = JsonUtils.FormatJsonBField(u.meta_json, _logger)
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep history for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<DrepVoteInfoResponseDto?> GetDrepVotesAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep votes for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var vote = await context.vote_list
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.voter_id == drepId);

                if (vote == null)
                {
                    _logger.LogWarning("No vote found for drepId: {DrepId}", drepId);
                    return null;
                }

                return new DrepVoteInfoResponseDto
                {
                    block_time = vote.block_time,
                    proposal_id = vote.proposal_id,
                    drep_id = vote.voter_id,
                    proposal_index = vote.proposal_index,
                    proposal_tx_hash = vote.proposal_tx_hash,
                    vote = vote.vote,
                    vote_tx_hash = vote.vote_tx_hash,
                    meta_url = JsonUtils.FormatJsonBField(vote.meta_url, _logger),
                    meta_hash = JsonUtils.FormatJsonBField(vote.meta_hash, _logger)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep history for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepVotingPowerHistoryResponseDto>?> GetTop10DrepVotingPowerAsync()
        {
            try
            {
                _logger.LogInformation("Getting top 10 DRep voting power");
                using var context = await _contextFactory.CreateDbContextAsync();
                var epoch = context.dreps_voting_power_history.Max(h => h.epoch_no);

                // Lấy epoch mới nhất và total register vote
                var allPowers = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(s => s.epoch_no == epoch)
                    .ToListAsync();

                var totalRegisterVote = allPowers.Where(s => s.drep_id != null && s.drep_id.StartsWith("drep1")).Sum(d => JsonUtils.ParseJsonBDecimal(d.amount)) / million ?? -1;

                var powers = allPowers
                    .Where(s => s.drep_id != "drep_always_no_confidence" && s.drep_id != "drep_always_abstain")
                    .OrderByDescending(x => JsonUtils.ParseJsonBDecimal(x.amount))
                    .Take(10)
                    .Select(s =>
                    {
                        var amount = (double?)(JsonUtils.ParseJsonBDecimal(s.amount) / million);
                        return new DrepVotingPowerHistoryResponseDto
                        {
                            amount = amount, // Convert to millions
                            drep_id = s.drep_id,
                            givenName = s.drep_id switch
                            {
                                "drep_always_no_confidence" => "No Confidence",
                                "drep_always_abstain" => "Abstain",
                                _ => "Other"
                            },
                            percentage = Math.Round((double)(amount / (double)totalRegisterVote * 100 ?? 0), 2)
                        };
                    })
                    .ToList();

                if (powers == null)
                {
                    _logger.LogWarning("No epoch summary found");
                    return new List<DrepVotingPowerHistoryResponseDto>();
                }

                foreach (var drep in powers)
                {
                    var map = await context.dreps_metadata.AsNoTracking().FirstOrDefaultAsync(d => d.drep_id == drep.drep_id);
                    if (map == null) continue;

                    var givenName = JsonUtils.ParseGivenName(map.meta_json);
                    if (string.IsNullOrEmpty(givenName))
                    {
                        givenName = drep.drep_id?.Length > 10 ?
                            $"{drep.drep_id[..6]}...{drep.drep_id[^4..]}" :
                            drep.drep_id;
                    }

                    drep.givenName = givenName;
                }

                return powers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top 10 drep voting power");
                throw;
            }
        }

        public async Task<TotalWalletStatisticsResponseDto?> GetTotalWalletStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting total wallet statistics");
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get the latest epoch
                var latestEpochSummary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (latestEpochSummary == null)
                {
                    _logger.LogWarning("No epoch summary found");
                    return new TotalWalletStatisticsResponseDto
                    {
                        delegators = new List<WalletDrepDto>(),
                        live_delegators = new List<WalletLiveDelegatorDto>(),
                        amounts = new List<WalletAmountDto>()
                    };
                }

                var currentEpoch = latestEpochSummary.epoch_no ?? 0;
                var startEpoch = 507; // Starting epoch for DRep
                var epochCount = currentEpoch - startEpoch + 1;

                // Get delegators data from epochs table (equivalent to drepDelegators in JS)
                var delegatorsData = await context.epochs
                    .AsNoTracking()
                    .Where(e => e.no >= startEpoch && e.no <= currentEpoch)
                    .OrderByDescending(e => e.no)
                    .Take(epochCount)
                    .Select(e => new
                    {
                        epoch_no = e.no,
                        delegator = e.delegator,
                        account_with_amount = e.account_with_amount
                    })
                    .ToListAsync();

                // Process delegators data (equivalent to JS logic)
                var delegators = delegatorsData
                    .OrderBy(d => d.epoch_no)
                    .Select(d => new WalletDrepDto
                    {
                        epoch_no = d.epoch_no ?? 0,
                        delegator = d.delegator ?? 0
                    })
                    .ToList();

                var amounts = delegatorsData
                    .OrderBy(d => d.epoch_no)
                    .Select(d => new WalletAmountDto
                    {
                        epoch_no = d.epoch_no ?? 0,
                        amount = d.account_with_amount ?? 0
                    })
                    .ToList();

                // Get live_delegators data from dreps table with logic from JS
                var drepsData = await context.dreps
                    .AsNoTracking()
                    .Select(d => new
                    {
                        epoch_no = d.last_active_epoch == null ? 507 : d.last_active_epoch - 20,
                        delegator = d.delegator
                    })
                    .ToListAsync();

                // Step 1: Transform liveDelegators data (already done above)
                var liveDelegatorsData = drepsData;

                // Step 2: Group by epoch_no and sum delegators
                var grouped = new Dictionary<int, int>();
                foreach (var entry in liveDelegatorsData)
                {
                    var epoch = entry.epoch_no ?? 507;
                    if (!grouped.ContainsKey(epoch))
                    {
                        grouped[epoch] = 0;
                    }
                    grouped[epoch] += entry.delegator ?? 0;
                }

                // Step 3: Create sorted epoch array and fill missing epochs
                var epochs = grouped.Keys.OrderBy(e => e).ToList();
                var liveDelegatorsResult = new List<WalletLiveDelegatorDto>();

                if (epochs.Any())
                {
                    var minEpoch = epochs.Min();
                    var maxEpoch = epochs.Max();
                    var completeEpochs = new List<int>();

                    // Fill all epochs from min to max, including missing ones
                    for (int epoch = minEpoch; epoch <= maxEpoch; epoch++)
                    {
                        completeEpochs.Add(epoch);
                    }

                    // Step 4: Compute cumulative totals, using 0 for missing epochs
                    var cumulativeTotal = 0;

                    foreach (var epoch in completeEpochs)
                    {
                        var delegatorValue = grouped.ContainsKey(epoch) ? grouped[epoch] : 0; // Use 0 for missing epochs
                        cumulativeTotal += delegatorValue;
                        liveDelegatorsResult.Add(new WalletLiveDelegatorDto
                        {
                            epoch_no = epoch,
                            live_delegators = cumulativeTotal
                        });
                    }
                }

                var result = new TotalWalletStatisticsResponseDto
                {
                    delegators = delegators,
                    live_delegators = liveDelegatorsResult,
                    amounts = amounts
                };

                _logger.LogInformation("Total wallet statistics - Delegators: {DelegatorCount}, Live Delegators: {LiveDelegatorCount}, Amounts: {AmountCount}",
                    delegators.Count, liveDelegatorsResult.Count, amounts.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total wallet statistics");
                throw;
            }
        }

        public async Task<DrepPoolVotingThresholdResponseDto?> GetDrepAndPoolVotingThresholdAsync()
        {
            try
            {
                _logger.LogInformation("Getting DRep and pool voting threshold");
                using var context = await _contextFactory.CreateDbContextAsync();
                var epochParam = await context.epoch_protocol_parameters.AsNoTracking().FirstOrDefaultAsync(d => d.epoch_no == context.epoch_protocol_parameters.Max(f => f.epoch_no));

                if (epochParam == null)
                {
                    _logger.LogWarning("No epoch protocol parameters found");
                    return new DrepPoolVotingThresholdResponseDto();
                }

                var result = new DrepPoolVotingThresholdResponseDto
                {
                    motion_no_confidence = JsonUtils.ParseJsonDouble(epochParam.dvt_motion_no_confidence) * 100 ?? 0.0,
                    committee_normal = JsonUtils.ParseJsonDouble(epochParam.dvt_committee_normal) * 100 ?? 0.0,
                    committee_no_confidence = JsonUtils.ParseJsonDouble(epochParam.dvt_committee_no_confidence) * 100 ?? 0.0,
                    hard_fork_initiation = JsonUtils.ParseJsonDouble(epochParam.dvt_hard_fork_initiation) * 100 ?? 0.0,
                    update_to_constitution = JsonUtils.ParseJsonDouble(epochParam.dvt_update_to_constitution) * 100 ?? 0.0,
                    network_param_voting = JsonUtils.ParseJsonDouble(epochParam.dvt_p_p_network_group) * 100 ?? 0.0,
                    economic_param_voting = JsonUtils.ParseJsonDouble(epochParam.dvt_p_p_economic_group) * 100 ?? 0.0,
                    technical_param_voting = JsonUtils.ParseJsonDouble(epochParam.dvt_p_p_technical_group) * 100 ?? 0.0,
                    governance_param_voting = JsonUtils.ParseJsonDouble(epochParam.dvt_p_p_gov_group) * 100 ?? 0.0,
                    treasury_withdrawal = JsonUtils.ParseJsonDouble(epochParam.dvt_treasury_withdrawal) * 100 ?? 0.0,
                    pool_motion_no_confidence = JsonUtils.ParseJsonDouble(epochParam.pvt_motion_no_confidence) * 100 ?? 0.0,
                    pool_committee_normal = JsonUtils.ParseJsonDouble(epochParam.pvt_committee_normal) * 100 ?? 0.0,
                    pool_committee_no_confidence = JsonUtils.ParseJsonDouble(epochParam.pvt_committee_no_confidence) * 100 ?? 0.0,
                    pool_hard_fork_initiation = JsonUtils.ParseJsonDouble(epochParam.pvt_hard_fork_initiation) * 100 ?? 0.0
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep and pool voting threshold");
                throw;
            }
        }

        public async Task<DrepPoolStakeThresholdResponseDto?> GetDrepTotalStakeApprovalThresholdAsync(int epochNo, string proposalType)
        {
            try
            {
                _logger.LogInformation("Getting DRep total stake approval threshold for epoch {EpochNo}, proposal type {ProposalType}", epochNo, proposalType);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query epoch protocol parameters for the specified epoch
                var epochParam = await context.epoch_protocol_parameters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ep => ep.epoch_no == epochNo);

                if (epochParam == null)
                {
                    _logger.LogWarning("No epoch protocol parameters found for epoch {EpochNo}", epochNo);
                    return null;
                }

                // Calculate thresholds based on proposal type
                switch (proposalType)
                {
                    case "NewConstitution":
                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = JsonUtils.ParseJsonDouble(epochParam.dvt_update_to_constitution) * 100 ?? 0,
                            poolTotalStake = 0
                        };

                    case "ParameterChange":
                        // Use hardcoded 0.67 as in the JavaScript code
                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = 67,
                            poolTotalStake = 0
                        };

                    case "TreasuryWithdrawals":
                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = JsonUtils.ParseJsonDouble(epochParam.dvt_treasury_withdrawal) * 100 ?? 0,
                            poolTotalStake = 0
                        };

                    case "HardForkInitiation":
                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = JsonUtils.ParseJsonDouble(epochParam.dvt_hard_fork_initiation) * 100 ?? 0,
                            poolTotalStake = JsonUtils.ParseJsonDouble(epochParam.pvt_hard_fork_initiation) * 100 ?? 0
                        };

                    case "NoConfidence":
                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = JsonUtils.ParseJsonDouble(epochParam.dvt_motion_no_confidence) * 100 ?? 0,
                            poolTotalStake = JsonUtils.ParseJsonDouble(epochParam.pvt_motion_no_confidence) * 100 ?? 0
                        };

                    case "NewCommittee":
                        var drepCommitteeNormal = JsonUtils.ParseJsonDouble(epochParam.dvt_committee_normal) * 100 ?? 0;
                        var drepCommitteeNoConfidence = JsonUtils.ParseJsonDouble(epochParam.dvt_committee_no_confidence) * 100 ?? 0;
                        var poolCommitteeNormal = JsonUtils.ParseJsonDouble(epochParam.pvt_committee_normal) * 100 ?? 0;
                        var poolCommitteeNoConfidence = JsonUtils.ParseJsonDouble(epochParam.pvt_committee_no_confidence) * 100 ?? 0;

                        return new DrepPoolStakeThresholdResponseDto
                        {
                            drepTotalStake = Math.Max(drepCommitteeNormal, drepCommitteeNoConfidence),
                            poolTotalStake = Math.Max(poolCommitteeNormal, poolCommitteeNoConfidence)
                        };

                    default:
                        _logger.LogWarning("Unknown proposal type: {ProposalType}", proposalType);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep total stake approval threshold for epoch {EpochNo}, proposal type {ProposalType}", epochNo, proposalType);
                throw;
            }
        }

        public async Task<DrepCardDataResponseDto?> GetDrepCardDataAsync()
        {
            try
            {
                _logger.LogInformation("Getting DRep card data");
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get latest epoch from drep_epoch_summary (same as lastEpoch in JS)
                var latestEpochSummary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (latestEpochSummary == null)
                {
                    _logger.LogWarning("No epoch summary found");
                    return new DrepCardDataResponseDto();
                }

                var currentEpoch = latestEpochSummary.epoch_no ?? 0;
                var previousEpoch = currentEpoch - 1;

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

                var previousDreps = lastSummary?.dreps ?? 0;
                previousDreps += drepZeroCount
                   .Where(zeroEntry => zeroEntry.epoch_no < lastEpochNo)
                   .Sum(zeroEntry => zeroEntry.count);


                var drepsChange = previousDreps > 0 ?
                    (((totalDreps - previousDreps) / (double)previousDreps) * 100) : 0;

                // Get delegators data from epochs table (same as delegatorsRequest in JS)
                // Order by "no" desc and take 2 items (current and previous epoch)
                var epochsData = await context.epochs
                    .AsNoTracking()
                    .OrderByDescending(e => e.no)
                    .Take(2)
                    .Select(e => new { epoch_no = e.no, delegator = e.delegator })
                    .ToListAsync();

                if (epochsData.Count < 2)
                {
                    _logger.LogWarning("Insufficient epochs data");
                    return new DrepCardDataResponseDto();
                }

                // Sort ascending to match JS logic (newer epoch will be [1], older [0])
                epochsData = epochsData.OrderBy(e => e.epoch_no).ToList();

                var totalDelegatedDrep = epochsData[1].delegator ?? 0;
                var lastTotalDelegatedDrep = epochsData[0].delegator ?? 0;
                var totalDelegatedDrepChange = lastTotalDelegatedDrep > 0 ?
                    (((totalDelegatedDrep - lastTotalDelegatedDrep) / (double)lastTotalDelegatedDrep) * 100) : 0;

                // Get current epoch active votes from dreps_voting_power_history (same as currentEpochRes in JS)
                var currentActiveVotes = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(h => h.epoch_no == currentEpoch && h.drep_id != null && h.drep_id.StartsWith("drep1"))
                    .ToListAsync();

                // Get previous epoch active votes
                var previousActiveVotes = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(h => h.epoch_no == previousEpoch && h.drep_id != null && h.drep_id.StartsWith("drep1"))
                    .ToListAsync();

                // Filter current active votes (dreps starting with "drep1")

                var currentTotalActiveAmount = SumFilteredAmounts(currentActiveVotes.Select(d => d.amount).ToList());
                var currentTotalActive = NumberUtils.DivideAndTruncate(
                    currentTotalActiveAmount.ToString(), trillion, 1);


                var previousTotalActiveAmount = SumFilteredAmounts(previousActiveVotes.Select(d => d.amount).ToList());
                var previousTotalActive = NumberUtils.DivideAndTruncate(
                    previousTotalActiveAmount.ToString(), trillion, 1);

                var totalActiveChange = previousTotalActiveAmount > 0 ?
                    (((currentTotalActiveAmount - previousTotalActiveAmount) / previousTotalActiveAmount) * 100) : 0;

                // Build response
                var result = new DrepCardDataResponseDto
                {
                    dreps = totalDreps,
                    drepsChange = drepsChange.ToString("F2"),
                    totalDelegatedDrep = totalDelegatedDrep,
                    totalDelegatedDrepChange = totalDelegatedDrepChange.ToString("F3"),
                    currentTotalActive = (double)currentTotalActive,
                    totalActiveChange = totalActiveChange.ToString("F2")
                };

                _logger.LogInformation("DRep card data - Total: {Total}, Change: {Change}%, Delegated: {Delegated}, Active: {Active}",
                    result.dreps, result.drepsChange, result.totalDelegatedDrep, result.currentTotalActive);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep card data");
                throw;
            }
        }

        public async Task<DrepCardDataByIdResponseDto?> GetDrepCardDataByIdAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep card data by id for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get latest epoch from drep_epoch_summary (same as lastEpoch in JS)
                var latestEpochSummary = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (latestEpochSummary == null)
                {
                    _logger.LogWarning("No epoch summary found");
                    return null;
                }

                var currentEpoch = latestEpochSummary.epoch_no ?? 0;
                var previousEpoch = currentEpoch - 1;

                // Get current epoch voting power for specific drep_id (same as recentEpochResponse in JS)
                var recentEpochData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(h => h.epoch_no == currentEpoch && h.drep_id == drepId)
                    .FirstOrDefaultAsync();

                // Get previous epoch voting power for specific drep_id (same as previousEpochResponse in JS)
                var previousEpochData = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .Where(h => h.epoch_no == previousEpoch && h.drep_id == drepId)
                    .FirstOrDefaultAsync();

                // Calculate amounts in millions (divide by 1e6 like in JS)
                var recentAmount = recentEpochData?.amount != null ?
                    (JsonUtils.ParseJsonBDecimal(recentEpochData.amount) ?? 0) / 1_000_000 : 0;
                var previousAmount = previousEpochData?.amount != null ?
                    (JsonUtils.ParseJsonBDecimal(previousEpochData.amount) ?? 0) / 1_000_000 : 0;

                // Calculate percentage change
                var percentageChange = previousAmount > 0 ?
                    ((recentAmount - previousAmount) / previousAmount) * 100 : 0;

                // Get metadata (same as metadataResponse in JS)
                var metadata = await context.dreps_metadata
                    .AsNoTracking()
                    .Where(m => m.drep_id == drepId)
                    .FirstOrDefaultAsync();

                if (metadata == null)
                {
                    _logger.LogWarning("No metadata found for DRep ID: {DrepId}", drepId);
                    return null;
                }

                // Get drep updates for registration date (same as drepUpdatesResponse in JS)
                var drepUpdates = await context.dreps_updates
                    .AsNoTracking()
                    .Where(u => u.drep_id == drepId)
                    .OrderBy(u => u.block_time)  // Get first registration
                    .FirstOrDefaultAsync();

                // Get registration date - look for first "registered" action
                var registrationDate = drepUpdates?.action == "registered" ?
                    drepUpdates.block_time : null;

                // Parse metadata JSON using utility methods
                var formattedMetaJson = JsonUtils.FormatJsonBField(metadata.meta_json, _logger);

                var givenName = JsonUtils.ParseGivenName(formattedMetaJson) ?? "N/A";
                var imageUrl = JsonUtils.ParseImageUrl(metadata.meta_json);
                var objectives = JsonUtils.ParseObjectives(formattedMetaJson);
                var motivations = JsonUtils.ParseMotivations(formattedMetaJson);
                var qualifications = JsonUtils.ParseQualifications(formattedMetaJson);
                var references = JsonUtils.ParseReferences(formattedMetaJson);

                // Build response (same structure as JS responseData)
                var result = new DrepCardDataByIdResponseDto
                {
                    givenName = givenName,
                    votingPower = (double)recentAmount,
                    previousVotingPower = (double)previousAmount,
                    votingPowerChange = (double)percentageChange,
                    image = imageUrl,
                    objectives = objectives,
                    motivations = motivations,
                    qualifications = qualifications,
                    references = references,
                    registrationDate = NumberUtils.ConvertTimestampToDate(registrationDate ?? 0)
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep card data by id for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepVoteInfoResponseDto>?> GetDrepVoteInfoAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep vote info for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get votes from vote_list table where voter_id = drepId and voter_role = "DRep"
                var drepVotes = await context.vote_list
                    .AsNoTracking()
                    .Where(v => v.voter_id == drepId && v.voter_role == "DRep")
                    .ToListAsync();

                if (!drepVotes.Any())
                {
                    _logger.LogInformation("No votes found for drepId: {DrepId}", drepId);
                    return new List<DrepVoteInfoResponseDto>();
                }

                // Get all proposal info for title and type mapping
                var proposalIds = drepVotes.Select(v => v.proposal_id).Distinct().ToList();
                var proposals = await context.proposals_list
                    .AsNoTracking()
                    .Where(p => proposalIds.Contains(p.proposal_id))
                    .ToListAsync();

                // Build a map from proposal_id to { proposal_type, proposal_title }
                var proposalMap = new Dictionary<string, (string? proposal_type, string? proposal_title)>();
                foreach (var proposal in proposals)
                {
                    if (!string.IsNullOrEmpty(proposal.proposal_id))
                    {
                        var proposalType = proposal.proposal_type;
                        var proposalTitle = JsonUtils.ParseJsonBString(proposal.meta_json, "body.title") ?? "N/A";
                        proposalMap[proposal.proposal_id] = (proposalType, proposalTitle);
                    }
                }

                // Map each vote to include proposal_type and proposal_title (matching JavaScript logic)
                var result = drepVotes.Select(vote =>
                {
                    var proposalInfo = proposalMap.GetValueOrDefault(vote.proposal_id ?? string.Empty, (null, "N/A"));
                    return new DrepVoteInfoResponseDto
                    {
                        proposal_id = vote.proposal_id,
                        proposal_type = proposalInfo.proposal_type,
                        proposal_title = proposalInfo.proposal_title ?? "N/A",
                        vote = vote.vote,
                        meta_url = JsonUtils.FormatJsonBField(vote.meta_url, _logger) ?? "N/A",
                        block_time = vote.block_time
                    };
                }).ToList();

                _logger.LogInformation("Found {Count} votes for drepId: {DrepId}", result.Count, drepId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep vote info for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<DrepDelegationResponseDto?> GetDrepDelegationAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep delegation for drepId: {DrepId}", drepId);

                if (string.IsNullOrEmpty(drepId))
                {
                    throw new ArgumentException("drep_id is required");
                }
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get delegators from database
                var delegation = await context.dreps_delegators
                    .AsNoTracking()
                    .Where(d => d.drep_id == drepId && !string.IsNullOrEmpty(d.stake_address))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} delegators flrom database for drepId: {DrepId}", delegation.Count, drepId);

                if (!delegation.Any())
                {
                    return new DrepDelegationResponseDto
                    {
                        total_delegators = 0,
                        delegation_data = new List<DrepDelegationTableResponseDto>()
                    };
                }

                // Extract stake addresses
                var stakeAddresses = delegation.Select(d => d.stake_address).Where(addr => !string.IsNullOrEmpty(addr)).ToList();

                // Get account updates from database for these stake addresses
                var accountUpdates = await context.account_updates
                    .AsNoTracking()
                    .Where(au => stakeAddresses.Contains(au.stake_address) && au.action_type == "delegation_drep")
                    .ToListAsync();

                _logger.LogInformation("Found {Count} account updates from database", accountUpdates.Count);

                var allResponseData = new List<DrepDelegationTableResponseDto>();

                // Process delegators and their latest delegation updates
                foreach (var delegator in delegation)
                {
                    if (string.IsNullOrEmpty(delegator.stake_address)) continue;

                    // Get delegation updates for this stake address
                    var delegationUpdates = accountUpdates
                        .Where(au => au.stake_address == delegator.stake_address)
                        .OrderByDescending(au => au.epoch_no)
                        .ToList();

                    if (delegationUpdates.Any())
                    {
                        var latestDelegation = delegationUpdates.First();

                        allResponseData.Add(new DrepDelegationTableResponseDto
                        {
                            stake_address = delegator.stake_address,
                            block_time = latestDelegation.block_time ?? 0,
                            amount = !string.IsNullOrEmpty(delegator.amount) ?
                                (double?)DivideAndTruncate(decimal.Parse(delegator.amount), million, 1) : null
                        });
                    }
                    else
                    {
                        // Include delegator even without delegation updates for completeness
                        allResponseData.Add(new DrepDelegationTableResponseDto
                        {
                            stake_address = delegator.stake_address,
                            block_time = 0,
                            amount = !string.IsNullOrEmpty(delegator.amount) ?
                                (double?)DivideAndTruncate(decimal.Parse(delegator.amount), million, 1) : null
                        });
                    }
                }

                var result = new DrepDelegationResponseDto
                {
                    total_delegators = delegation.Count,
                    delegation_data = allResponseData
                };

                _logger.LogInformation("Processed {Count} delegators for drepId: {DRepId}", result.delegation_data?.Count, drepId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep delegation for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepRegistrationTableResponseDto>?> GetDrepRegistrationAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep registration for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query the dreps_updates table for the given drep_id
                var registrationData = await context.dreps_updates
                    .AsNoTracking()
                    .Where(d => d.drep_id == drepId)
                    .ToListAsync();

                if (registrationData == null || !registrationData.Any())
                {
                    _logger.LogWarning("No registration data found for drepId: {DrepId}", drepId);
                    return new List<DrepRegistrationTableResponseDto>();
                }

                // Map the data to match the JavaScript API response
                var result = registrationData.Select(data => new DrepRegistrationTableResponseDto
                {
                    block_time = data.block_time,
                    given_name = JsonUtils.ParseGivenName(data.meta_json) ?? "N/A",
                    action = data.action,
                    meta_url = JsonUtils.FormatJsonBField(data.meta_url, _logger) ?? "N/A"
                }).ToList();

                _logger.LogInformation("Found {Count} registration records for drepId: {DrepId}", result.Count, drepId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep registration for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<List<DrepDetailsVotingPowerResponseDto>?> GetDrepDetailsVotingPowerAsync(string drepId)
        {
            try
            {
                _logger.LogInformation("Getting DRep details voting power for drepId: {DrepId}", drepId);
                using var context = await _contextFactory.CreateDbContextAsync();
                // Query the dreps_voting_power_history table
                var votingPowerHistory = await context.dreps_voting_power_history
                    .Where(h => h.drep_id == drepId)
                    .OrderBy(h => h.epoch_no)
                    .ToListAsync();

                var result = votingPowerHistory.Select(h => new DrepDetailsVotingPowerResponseDto
                {
                    epoch_no = h.epoch_no,
                    amount = (double)Math.Round((decimal)(JsonUtils.ParseJsonBDecimal(h.amount) / million))
                }).ToList();

                _logger.LogInformation("Found {Count} voting power records for drepId: {DrepId}", result.Count, drepId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep details voting power for drep_id: {DrepId}", drepId);
                throw;
            }
        }

        public async Task<DrepsVotingPowerResponseDto?> GetDrepsVotingPowerAsync()
        {
            try
            {
                _logger.LogInformation("Getting DReps voting power");
                using var context = await _contextFactory.CreateDbContextAsync();

                var drepResponse = await context.dreps_voting_power_history
                    .AsNoTracking()
                    .ToListAsync();


                // Map no confidence data
                var noConfidentData = drepResponse.Where(d => d.drep_id == "drep_always_no_confidence").Select(d => new VotingPowerDataDto
                {
                    epoch_no = d.epoch_no,
                    voting_power = (double)NumberUtils.DivideAndTruncate(
                        JsonUtils.ParseJsonBDecimal(d.amount)?.ToString() ?? "0",
                        million, 1)
                }).ToList();

                // Map abstain data
                var abstainData = drepResponse.Where(d => d.drep_id == "drep_always_abstain").Select(d => new VotingPowerDataDto
                {
                    epoch_no = d.epoch_no,
                    voting_power = (double)NumberUtils.DivideAndTruncate(
                        JsonUtils.ParseJsonBDecimal(d.amount)?.ToString() ?? "0",
                        million, 1)
                }).ToList();

                var totalDrepData = drepResponse.Where(d => d.drep_id != null && d.drep_id.StartsWith("drep1")).Select(d => new VotingPowerDataDto
                {
                    epoch_no = d.epoch_no,
                    voting_power = (double)NumberUtils.DivideAndTruncate(
                        JsonUtils.ParseJsonBDecimal(d.amount)?.ToString() ?? "0",
                        million, 1)
                })
                .GroupBy(s => s.epoch_no).Select(s => new VotingPowerDataDto
                {
                    epoch_no = s.Key,
                    voting_power = s.Sum(v => v.voting_power)
                }).ToList();
                // Add epoch 507 with 0 voting power to all datasets
                totalDrepData.Add(new VotingPowerDataDto
                {
                    epoch_no = 507,
                    voting_power = 0
                });

                noConfidentData.Add(new VotingPowerDataDto
                {
                    epoch_no = 507,
                    voting_power = 0
                });

                abstainData.Add(new VotingPowerDataDto
                {
                    epoch_no = 507,
                    voting_power = 0
                });

                // Sort all datasets by epoch_no
                totalDrepData = [.. totalDrepData.OrderBy(d => d.epoch_no)];
                noConfidentData = [.. noConfidentData.OrderBy(d => d.epoch_no)];
                abstainData = [.. abstainData.OrderBy(d => d.epoch_no)];

                var result = new DrepsVotingPowerResponseDto
                {
                    abstain_data = abstainData,
                    no_confident_data = noConfidentData,
                    total_drep_data = totalDrepData
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dreps voting power");
                throw;
            }
        }

        public async Task<List<DrepNewRegisterResponseDto>?> GetDrepNewRegisterAsync()
        {
            try
            {
                _logger.LogInformation("Getting DRep new register data");

                using var context = await _contextFactory.CreateDbContextAsync();
                var updates = await context.dreps_updates
                     .AsNoTracking()
                     .Where(d => d.action == "registered")
                     .OrderByDescending(d => d.block_time)
                     .Take(50)
                     .Select(s => new
                     {
                         drep_id = s.drep_id,
                         block_time = s.block_time,
                         action = s.action
                     })
                     .ToListAsync();

                var latestByDrep = new Dictionary<string, DrepNewRegisterResponseDto>();
                var result = new List<DrepNewRegisterResponseDto>();

                foreach (var record in updates)
                {
                    if (!latestByDrep.ContainsKey(record.drep_id))
                    {
                        latestByDrep[record.drep_id] = new DrepNewRegisterResponseDto
                        {
                            drep_id = record.drep_id,
                            block_time = record.block_time,
                            action = record.action
                        };
                    }

                    if (latestByDrep.Count == 15)
                        break;
                }

                result = latestByDrep.Values.ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drep new register data");
                throw;
            }
        }

        private async Task<string?> CalculateActiveUntil(string? expiresEpochNo, int? currentEpoch)
        {
            if (string.IsNullOrEmpty(expiresEpochNo) || !currentEpoch.HasValue)
                return "N/A";

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var expiresEpoch = JsonUtils.ParseJsonInt(expiresEpochNo);
                if (!expiresEpoch.HasValue || expiresEpoch.Value <= 0)
                    return "N/A";

                var epochOffset = (expiresEpoch.Value - currentEpoch.Value) * 5 * 86400; // 5 days per epoch
                if (epochOffset <= 0)
                    return "N/A";

                // Get current epoch end time to calculate the actual date
                var currentEpochEndTime = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no == currentEpoch.Value)
                    .Select(e => e.end_time)
                    .FirstOrDefaultAsync();

                if (!currentEpochEndTime.HasValue)
                    return "N/A";

                // Calculate the actual timestamp for the expiry date
                var expiryTimestamp = currentEpochEndTime.Value + epochOffset;

                // Convert timestamp to date format like JavaScript
                var date = DateTimeOffset.FromUnixTimeSeconds((long)expiryTimestamp).UtcDateTime;

                return date.ToString("dd/MM/yyyy");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating active until date for epoch: {ExpiresEpochNo}", expiresEpochNo);
                return "N/A";
            }
        }

        private string? ParseImageUrl(string? metaJson)
        {
            if (string.IsNullOrEmpty(metaJson))
                return null;

            try
            {
                var meta = JsonUtils.ParseJson<dynamic>(metaJson);
                return meta?.body?.image?.contentUrl?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private double? GetVotingPower(List<object> votingPowerData, string? drepId)
        {
            if (string.IsNullOrEmpty(drepId) || votingPowerData == null)
                return 0;

            try
            {
                var data = votingPowerData.Cast<dynamic>().FirstOrDefault(v => v.drep_id == drepId);
                if (data == null)
                    return 0;

                var amountString = data.amount?.ToString();
                if (string.IsNullOrEmpty(amountString))
                    return 0;

                decimal? amount = JsonUtils.ParseJsonBDecimal(amountString);
                if (amount.HasValue)
                {
                    return (double)(amount.Value / million);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting voting power for DRep: {DrepId}", drepId);
                return 0;
            }
        }

        private int GetDelegatorsCount(List<object> delegatorsData, string? drepId)
        {
            if (string.IsNullOrEmpty(drepId) || delegatorsData == null)
                return 0;

            var data = delegatorsData.Cast<dynamic>().FirstOrDefault(d => d.drep_id == drepId);
            return data?.count ?? 0;
        }

        private int GetTimesVotedCount(List<object> timesVotedData, string? drepId)
        {
            if (string.IsNullOrEmpty(drepId) || timesVotedData == null)
                return 0;

            var data = timesVotedData.Cast<dynamic>().FirstOrDefault(t => t.drep_id == drepId);
            return data?.count ?? 0;
        }

        private decimal SumFilteredAmounts(List<string?>? list)
        {
            if (list == null || !list.Any())
                return 0;
            decimal sum = 0;
            foreach (var d in list)
            {
                var amount = JsonUtils.ParseJsonBDecimal(d);
                if (amount.HasValue)
                {
                    sum += amount.Value;
                }
            }
            return sum;
        }

        private async Task<object> GetParticipateInVotingDataAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                // Get current epoch
                var currentEpochQuery = await context.epochs
                    .AsNoTracking()
                    .OrderByDescending(e => e.no)
                    .FirstOrDefaultAsync();

                var currentEpoch = currentEpochQuery?.no ?? 0;

                // Get drep epoch summary data (from epoch 507 onwards)
                var drepEpochData = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .Where(d => d.epoch_no >= 507 && d.epoch_no <= currentEpoch)
                    .OrderBy(d => d.epoch_no)
                    .ToListAsync();

                // Add initial epoch data if not present
                if (!drepEpochData.Any(d => d.epoch_no == 507))
                {
                    drepEpochData.Insert(0, new SharedLibrary.Models.MDDrepsEpochSummary
                    {
                        epoch_no = 507,
                        dreps = 0
                    });
                }

                // Get zero delegator dreps (dreps with 0 delegators)
                var drepDelegatorCounts = await context.dreps_delegators
                    .AsNoTracking()
                    .GroupBy(d => d.drep_id)
                    .Select(g => new { drep_id = g.Key, count = g.Count() })
                    .ToListAsync();

                // Find dreps with 0 delegators using dreps_info
                var allDreps = await context.dreps_info
                    .AsNoTracking()
                    .Where(d => d.drep_id != null)
                    .ToListAsync();

                // Map zero delegator dreps to registration epoch (using expires_epoch_no - 19 as approximation)
                var drepZeroData = allDreps
                    .Where(d => !drepDelegatorCounts.Any(dc => dc.drep_id == d.drep_id))
                    .Where(d => d.expires_epoch_no != null)
                    .Select(d => new
                    {
                        epoch_no = (JsonUtils.ParseJsonInt(d.expires_epoch_no) ?? 0) - 19
                    })
                    .Where(d => d.epoch_no >= 507)
                    .ToList();

                // Count zero delegator dreps by epoch
                var drepZeroCountData = drepZeroData
                    .GroupBy(d => d.epoch_no)
                    .Select(g => new { epoch_no = g.Key, count = g.Count() })
                    .OrderBy(d => d.epoch_no)
                    .ToList();

                // Calculate final drep result including zero delegator dreps
                var drepResult = new List<object>();
                foreach (var drepEntry in drepEpochData)
                {
                    var epoch = drepEntry.epoch_no ?? 0;
                    var total = drepEntry.dreps ?? 0;

                    // Add zero delegator dreps for current and all previous epochs (cumulative)
                    foreach (var zeroEntry in drepZeroCountData.Where(z => z.epoch_no <= epoch))
                    {
                        total += zeroEntry.count;
                    }

                    drepResult.Add(new { epoch_no = epoch, dreps = total });
                }

                return new
                {
                    drep = drepResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participate in voting data");
                throw;
            }
        }

        private decimal DivideAndTruncate(decimal value, decimal divisor, int decimalPlaces)
        {
            if (divisor == 0) return 0;
            var result = value / divisor;
            return Math.Round(result, decimalPlaces);
        }
    }
}