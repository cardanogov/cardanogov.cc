namespace MainAPI.Application.Queries.Pool
{
    public class GetAdaStatisticsQuery : IRequest<AdaStatisticsResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetAdaStatisticsQueryHandler : IRequestHandler<GetAdaStatisticsQuery, AdaStatisticsResponseDto?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAdaStatisticsQueryHandler> _logger;

        public GetAdaStatisticsQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetAdaStatisticsQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<AdaStatisticsResponseDto?> Handle(GetAdaStatisticsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.ADA_STATISTICS;

            try
            {
                _logger.LogInformation("Processing GetAdaStatisticsQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching ada statistics data");
                        var data = await _poolService.GetAdaStatisticsAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ada statistics data");
                throw;
            }
        }
    }
}