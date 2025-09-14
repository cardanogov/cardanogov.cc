namespace MainAPI.Application.Queries.Treasury
{
    public class GetTotalTreasuryQuery : IRequest<TreasuryDataResponseDto?> { }

    public class GetTotalTreasuryQueryHandler : IRequestHandler<GetTotalTreasuryQuery, TreasuryDataResponseDto?>
    {
        private readonly ITreasuryService _treasuryService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalTreasuryQueryHandler> _logger;

        public GetTotalTreasuryQueryHandler(
            ITreasuryService treasuryService,
            ICacheService cacheService,
            ILogger<GetTotalTreasuryQueryHandler> logger)
        {
            _treasuryService = treasuryService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TreasuryDataResponseDto?> Handle(GetTotalTreasuryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_TREASURY;

            try
            {
                _logger.LogInformation("Processing GetTotalTreasuryQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total treasury data");
                        var data = await _treasuryService.GetTotalTreasuryAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total treasury data");
                throw;
            }
        }
    }
}