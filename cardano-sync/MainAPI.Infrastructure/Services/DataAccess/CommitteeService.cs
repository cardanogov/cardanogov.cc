using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class CommitteeService : ICommitteeService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<CommitteeService> _logger;

        public CommitteeService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<CommitteeService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int?> GetTotalCommitteeAsync()
        {
            try
            {
                _logger.LogInformation("Getting total committee count");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get total count from committee_information table
                var totalCommittee = await context.committee_information
                    .AsNoTracking()
                    .Select(ci => ci.members)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(totalCommittee))
                {
                    return 0;
                }

                var committeeMembers = JsonUtils.ParseJsonList(totalCommittee);
                var count = committeeMembers?.Count ?? 0;

                _logger.LogInformation("Total committee count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total committee count");
                throw;
            }
        }

        public async Task<List<CommitteeVotesResponseDto>?> GetCommitteeVotesAsync(string ccHotId)
        {
            try
            {
                _logger.LogInformation("Getting committee votes for cc_hot_id: {CcHotId}", ccHotId);

                using var context = await _contextFactory.CreateDbContextAsync();

                var votes = await context.committee_votes
                    .AsNoTracking()
                    .Where(cv => cv.cc_hot_id == ccHotId)
                    .OrderByDescending(cv => cv.block_time)
                    .ToListAsync();

                var result = votes.Select(cv => new CommitteeVotesResponseDto
                {
                    cc_hot_id = cv.cc_hot_id,
                    proposal_id = cv.proposal_id,
                    proposal_tx_hash = !string.IsNullOrEmpty(cv.proposal_tx_hash) && int.TryParse(cv.proposal_tx_hash, out var txHash) ? txHash : null,
                    proposal_index = cv.proposal_index,
                    vote_tx_hash = cv.vote_tx_hash,
                    block_time = cv.block_time,
                    vote = cv.vote,
                    meta_url = cv.meta_url,
                    meta_hash = cv.meta_hash
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} committee votes", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting committee votes for cc_hot_id: {CcHotId}", ccHotId);
                throw;
            }
        }

        public async Task<List<CommitteeInfoResponseDto>?> GetCommitteeInfoAsync()
        {
            try
            {
                _logger.LogInformation("Getting committee info");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Load raw data first to avoid JSON parsing in SQL
                var rawData = await context.committee_information
                    .AsNoTracking()
                    .OrderByDescending(ci => ci.proposal_index)
                    .ToListAsync();

                // Process JSON in memory
                var committeeInfo = rawData.Select(ci => new CommitteeInfoResponseDto
                {
                    proposal_id = ci.proposal_id,
                    proposal_tx_hash = ci.proposal_tx_hash,
                    proposal_index = ci.proposal_index,
                    quorum_numerator = ci.quorum_numerator,
                    quorum_denominator = ci.quorum_denominator,
                    members = JsonUtils.ParseJson<List<MemberDto>>(ci.members)
                }).ToList();

                _logger.LogInformation("Successfully retrieved committee info with {Count} records", committeeInfo.Count);
                return committeeInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting committee info");
                throw;
            }
        }
    }
}