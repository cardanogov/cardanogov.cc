namespace MainAPI.Application.Queries.Pool
{
    public class GetAdaStatisticsPercentageQuery : IRequest<AdaStatisticsPercentageResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetAdaStatisticsPercentageQueryHandler : IRequestHandler<GetAdaStatisticsPercentageQuery, AdaStatisticsPercentageResponseDto?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAdaStatisticsPercentageQueryHandler> _logger;

        public GetAdaStatisticsPercentageQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetAdaStatisticsPercentageQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<AdaStatisticsPercentageResponseDto?> Handle(GetAdaStatisticsPercentageQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.ADA_STATISTICS_PERCENTAGE;

            try
            {
                _logger.LogInformation("Processing GetAdaStatisticsPercentageQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching ada statistics percentage data");
                        var data = await _poolService.GetAdaStatisticsPercentageAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ada statistics percentage data");
                throw;
            }
        }
    }
}