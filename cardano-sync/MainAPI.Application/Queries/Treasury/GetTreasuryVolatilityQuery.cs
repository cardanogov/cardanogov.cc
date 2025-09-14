namespace MainAPI.Application.Queries.Treasury
{
    public class GetTreasuryVolatilityQuery : IRequest<TreasuryResponseDto?> { }

    public class GetTreasuryVolatilityQueryHandler : IRequestHandler<GetTreasuryVolatilityQuery, TreasuryResponseDto?>
    {
        private readonly ITreasuryService _treasuryService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTreasuryVolatilityQueryHandler> _logger;

        public GetTreasuryVolatilityQueryHandler(
            ITreasuryService treasuryService,
            ICacheService cacheService,
            ILogger<GetTreasuryVolatilityQueryHandler> logger)
        {
            _treasuryService = treasuryService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TreasuryResponseDto?> Handle(GetTreasuryVolatilityQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TREASURY_VOLATILITY;

            try
            {
                _logger.LogInformation("Processing GetTreasuryVolatilityQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching treasury volatility data");
                        var data = await _treasuryService.GetTreasuryVolatilityAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving treasury volatility data");
                throw;
            }
        }
    }
}