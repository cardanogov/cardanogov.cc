namespace MainAPI.Application.Queries.Pool
{
    public class GetTotalPoolQuery : IRequest<int?>
    {
        // No parameters needed for this query
    }

    public class GetTotalPoolQueryHandler : IRequestHandler<GetTotalPoolQuery, int?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalPoolQueryHandler> _logger;

        public GetTotalPoolQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetTotalPoolQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<int?> Handle(GetTotalPoolQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_POOL;

            try
            {
                _logger.LogInformation("Processing GetTotalPoolQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total pool data");
                        var data = await _poolService.GetTotalPoolAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total pool data");
                throw;
            }
        }
    }
}