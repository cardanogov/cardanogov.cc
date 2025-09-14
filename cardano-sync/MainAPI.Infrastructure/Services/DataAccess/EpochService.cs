using MainAPI.Core.Utils;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class EpochService : IEpochService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<EpochService> _logger;
        const decimal trillion = 1_000_000_000_000_000m;
        const decimal million = 1_000_000m;

        public EpochService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<EpochService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Get epoch info - matching getEpochInfo in epochApi.js
        /// </summary>
        public async Task<EpochInfoResponseDto?> GetEpochInfoAsync(int epochNo)
        {
            try
            {
                _logger.LogInformation("Getting epoch info for epoch: {EpochNo}", epochNo);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get epoch info from database
                var epochInfo = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no == epochNo)
                    .FirstOrDefaultAsync();

                if (epochInfo == null)
                {
                    _logger.LogWarning("No epoch info found for epoch: {EpochNo}", epochNo);
                    return null;
                }

                // Map to response DTO
                var result = new EpochInfoResponseDto
                {
                    epoch_no = epochInfo.epoch_no,
                    out_sum = epochInfo.out_sum,
                    fees = epochInfo.fees,
                    tx_count = (int?)epochInfo.tx_count,
                    blk_count = (int?)epochInfo.blk_count,
                    start_time = (int?)epochInfo.start_time,
                    end_time = (int?)epochInfo.end_time,
                    first_block_time = (int?)epochInfo.first_block_time,
                    last_block_time = (int?)epochInfo.last_block_time,
                    active_stake = JsonUtils.FormatJsonBField(epochInfo.active_stake),
                    total_rewards = epochInfo.total_rewards,
                    avg_blk_reward = JsonUtils.FormatJsonBField(epochInfo.avg_blk_reward)
                };

                _logger.LogInformation("Successfully retrieved epoch info for epoch: {EpochNo}", epochNo);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting epoch info for epoch: {EpochNo}", epochNo);
                throw;
            }
        }

        /// <summary>
        /// Get current epoch - matching getCurrentEpoch in epochApi.js
        /// </summary>
        public async Task<List<CurrentEpochResponseDto>?> GetCurrentEpochAsync()
        {
            try
            {
                _logger.LogInformation("Getting current epoch");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get current epoch from database
                var currentEpoch = await context.dreps_epoch_summary
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No current epoch found");
                    return new List<CurrentEpochResponseDto>();
                }

                // Return as single item list to match JavaScript format
                var result = new List<CurrentEpochResponseDto>
                {
                    new CurrentEpochResponseDto
                    {
                        epoch_no = currentEpoch.epoch_no,
                        amount = currentEpoch.amount,
                        dreps = currentEpoch.dreps
                    }
                };

                _logger.LogInformation("Successfully retrieved current epoch: {EpochNo}", currentEpoch.epoch_no);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current epoch");
                throw;
            }
        }

        /// <summary>
        /// Get total stake - matching getTotalStake in epochApi.js
        /// </summary>
        public async Task<TotalStakeResponseDto?> GetTotalStakeAsync()
        {
            try
            {
                _logger.LogInformation("Getting total stake");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get all epochs with stake data
                var epochsWithStake = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.active_stake != null)
                    .OrderBy(e => e.epoch_no)
                    .Select(e => new { e.epoch_no, e.active_stake })
                    .ToListAsync();

                var total = await context.totals.AsNoTracking().OrderByDescending(t => t.epoch_no).FirstOrDefaultAsync();

                if (!epochsWithStake.Any() && total == null)
                {
                    _logger.LogWarning("No stake data found");
                    return new TotalStakeResponseDto { totalADA = 0, totalSupply = 0, chartStats = new List<double>() };
                }

                var totalSupply = double.TryParse(total?.supply, out double supply) ? NumberUtils.DivideAndTruncate(supply.ToString(), trillion, 2) : 0;
                var totalADA = NumberUtils.DivideAndTruncate(JsonUtils.FormatJsonBField(epochsWithStake.Last().active_stake), trillion, 2);
                var chartStats = epochsWithStake.TakeLast(10).Select(e => (double)(decimal.TryParse(JsonUtils.FormatJsonBField(e.active_stake), out var stake) ? NumberUtils.DivideAndTruncate(stake.ToString(), trillion, 2) : 0)).ToList();

                var result = new TotalStakeResponseDto
                {
                    totalADA = totalADA,
                    totalSupply = totalSupply,
                    chartStats = chartStats
                };

                _logger.LogInformation("Successfully retrieved total stake for {Count} epochs", epochsWithStake.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stake");
                throw;
            }
        }

        /// <summary>
        /// Get current epoch info
        /// </summary>
        public async Task<EpochInfoResponseDto?> GetCurrentEpochInfoAsync()
        {
            try
            {
                _logger.LogInformation("Getting current epoch info");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get current epoch
                var currentEpoch = await context.epoch
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No current epoch found");
                    return null;
                }

                var result = new EpochInfoResponseDto
                {
                    epoch_no = currentEpoch.epoch_no,
                    out_sum = currentEpoch.out_sum,
                    fees = currentEpoch.fees,
                    tx_count = (int?)currentEpoch.tx_count,
                    blk_count = (int?)currentEpoch.blk_count,
                    start_time = (int?)currentEpoch.start_time,
                    end_time = (int?)currentEpoch.end_time,
                    first_block_time = (int?)currentEpoch.first_block_time,
                    last_block_time = (int?)currentEpoch.last_block_time,
                    active_stake = JsonUtils.FormatJsonBField(currentEpoch.active_stake),
                    total_rewards = currentEpoch.total_rewards,
                    avg_blk_reward = JsonUtils.FormatJsonBField(currentEpoch.avg_blk_reward)
                };

                _logger.LogInformation("Successfully retrieved current epoch info for epoch: {EpochNo}", currentEpoch.epoch_no);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current epoch info");
                throw;
            }
        }

        /// <summary>
        /// Get epoch info for SPO
        /// </summary>
        public async Task<int?> GetEpochInfoSpoAsync(int epochNo)
        {
            try
            {
                _logger.LogInformation("Getting epoch info SPO for epoch: {EpochNo}", epochNo);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Get current epoch to validate request
                var currentEpoch = await context.epoch
                    .AsNoTracking()
                    .OrderByDescending(e => e.epoch_no)
                    .FirstOrDefaultAsync();

                if (currentEpoch == null)
                {
                    _logger.LogWarning("No epoch found");
                    return null;
                }

                // If requested epoch is greater than current, return current epoch data
                var targetEpochNo = epochNo > currentEpoch.epoch_no ? currentEpoch.epoch_no : epochNo;

                // Get epoch info
                var epochInfo = await context.epoch
                    .AsNoTracking()
                    .Where(e => e.epoch_no == targetEpochNo)
                    .FirstOrDefaultAsync();

                if (epochInfo == null)
                {
                    _logger.LogWarning("No epoch found for epoch: {EpochNo}", targetEpochNo);
                    return null;
                }

                // Parse active stake
                var activeStake = double.TryParse(JsonUtils.FormatJsonBField(epochInfo.active_stake), out var parsedStake) ? parsedStake : 0;

                _logger.LogInformation("Successfully retrieved epoch info SPO for epoch: {EpochNo}", targetEpochNo);
                return (int)NumberUtils.DivideAndTruncate(activeStake.ToString(), trillion, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting epoch info SPO for epoch: {EpochNo}", epochNo);
                throw;
            }
        }

        /// <summary>
        /// Get epoch protocol parameters
        /// </summary>
        public async Task<EpochProtocolParametersResponseDto?> GetEpochProtocolParametersAsync(int epochNo)
        {
            try
            {
                _logger.LogInformation("Getting epoch protocol parameters for epoch: {EpochNo}", epochNo);

                // TODO: Implement when epoch_param table is available
                _logger.LogWarning("Epoch protocol parameters not implemented yet");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting epoch protocol parameters for epoch: {EpochNo}", epochNo);
                throw;
            }
        }
    }
}