namespace MainAPI.Application.Queries.Drep
{
    public class GetTotalWalletStatisticsQuery : IRequest<TotalWalletStatisticsResponseDto?>
    {
    }

    public class GetTotalWalletStatisticsQueryHandler : IRequestHandler<GetTotalWalletStatisticsQuery, TotalWalletStatisticsResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetTotalWalletStatisticsQueryHandler> _logger;

        public GetTotalWalletStatisticsQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetTotalWalletStatisticsQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TotalWalletStatisticsResponseDto?> Handle(GetTotalWalletStatisticsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_WALLET_STATISTICS;

            try
            {
                _logger.LogInformation("Processing GetTotalWalletStatisticsQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total wallet statistics data");
                        var data = await _drepService.GetTotalWalletStatisticsAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total wallet statistics data");
                throw;
            }
        }
    }
}