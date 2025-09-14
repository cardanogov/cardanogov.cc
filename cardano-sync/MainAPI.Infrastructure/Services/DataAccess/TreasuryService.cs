using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class TreasuryService : ITreasuryService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IPriceService _priceService;
        private readonly ILogger<TreasuryService> _logger;

        public TreasuryService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TreasuryService> logger, IPriceService priceService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _priceService = priceService;
        }

        public async Task<TreasuryResponseDto?> GetTreasuryVolatilityAsync()
        {
            try
            {
                _logger.LogInformation("Getting treasury volatility");
                using var context = await _contextFactory.CreateDbContextAsync();

                // Lấy giá USD mới nhất từ bảng usd
                var usdPrice = await _priceService.GetUsdPriceAsync();

                // Lấy các bản ghi treasury theo epoch
                var volatilityRaw = await context.totals
                    .AsNoTracking()
                    .OrderBy(t => t.epoch_no)
                    .Select(t => new { t.epoch_no, t.treasury })
                    .ToListAsync();

                var volatility = volatilityRaw.Select(t =>
                {
                    double treasuryVal = 0;
                    double.TryParse(t.treasury, out treasuryVal);
                    double treasury = (double)(treasuryVal / 1e6);
                    double treasuryUsd = Math.Round(treasury * (double)usdPrice);
                    return new TreasuryVolatilityResponseDto
                    {
                        epoch_no = t.epoch_no,
                        treasury = treasury,
                        treasury_usd = treasuryUsd
                    };
                }).ToList();

                var withdrawals = await GetTreasuryWithdrawalsAsync() ?? [];

                // Bắt đầu từ epoch 209
                var startEpoch = volatility.Min(d => d.epoch_no) ?? 209;
                var endEpoch = volatility.Max(d => d.epoch_no);

                var withdrawalsResult = new List<TreasuryWithdrawalsResponseDto>();
                double cumulative = 0;
                int currentIndex = 0;

                for (int epoch = startEpoch; epoch <= endEpoch; epoch++)
                {
                    if (currentIndex < withdrawals.Count && withdrawals[currentIndex].epoch_no == epoch)
                    {
                        // Epoch có dữ liệu
                        cumulative += withdrawals[currentIndex].amount ?? 0;
                        currentIndex++;
                    }
                    // Epoch không có dữ liệu thì giữ nguyên cumulative

                    withdrawalsResult.Add(new TreasuryWithdrawalsResponseDto
                    {
                        epoch_no = epoch,
                        amount = cumulative
                    });
                }

                _logger.LogInformation("Successfully retrieved treasury volatility");
                return new TreasuryResponseDto
                {
                    volatilities = volatility,
                    withdrawals = withdrawalsResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting treasury volatility");
                throw;
            }
        }

        public async Task<List<TreasuryWithdrawalsResponseDto>?> GetTreasuryWithdrawalsAsync()
        {
            try
            {
                _logger.LogInformation("Getting treasury withdrawals");
                using var context = await _contextFactory.CreateDbContextAsync();

                // Lấy tổng withdrawals theo epoch
                var withdrawals = await context.treasury_withdrawals
                    .AsNoTracking()
                    .GroupBy(w => w.epoch_no)
                    .Select(g => new TreasuryWithdrawalsResponseDto
                    {
                        epoch_no = g.Key,
                        amount = g.Sum(x => x.sum ?? 0) / 1_000_000.0
                    })
                    .OrderBy(w => w.epoch_no)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved treasury withdrawals");
                return withdrawals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting treasury withdrawals");
                throw;
            }
        }

        public async Task<TreasuryDataResponseDto?> GetTotalTreasuryAsync()
        {
            try
            {
                _logger.LogInformation("Getting total treasury");
                using var context = await _contextFactory.CreateDbContextAsync();

                // Lấy bản ghi mới nhất từ totals
                var latestTotal = await context.totals
                    .AsNoTracking()
                    .OrderByDescending(t => t.epoch_no)
                    .FirstOrDefaultAsync();

                // Lấy tổng withdrawals
                var totalWithdrawals = await context.treasury_withdrawals
                    .AsNoTracking()
                    .SumAsync(w => w.sum ?? 0);

                // Lấy chart stats (các giá trị treasury của 10 epoch gần nhất)
                var chartStatsRaw = await context.totals
                    .AsNoTracking()
                    .Take(10)
                    .Select(t => t.treasury)
                    .ToListAsync();
                var chartStatsInt = chartStatsRaw
                    .Select(t =>
                    {
                        decimal v = 0;
                        decimal.TryParse(t, out v);
                        return (int)(v / 1_000_000);
                    })
                    .ToList();

                var result = new TreasuryDataResponseDto
                {
                    treasury = latestTotal != null && decimal.TryParse(latestTotal.treasury, out var treasuryVal) ? (double)(treasuryVal / 1_000_000) : 0,
                    total_withdrawals = (double)(totalWithdrawals / 1_000_000),
                    chart_stats = chartStatsInt
                };
                _logger.LogInformation("Successfully retrieved total treasury");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total treasury");
                throw;
            }
        }
    }
}