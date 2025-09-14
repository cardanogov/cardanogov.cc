namespace MainAPI.Application.Queries.Pool
{
    public class GetPoolInfoQuery : IRequest<PoolInfoDto?>
    {
        public string PoolBech32 { get; }

        public GetPoolInfoQuery(string poolBech32)
        {
            PoolBech32 = poolBech32;
        }
    }

    public class GetPoolInfoQueryHandler : IRequestHandler<GetPoolInfoQuery, PoolInfoDto?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetPoolInfoQueryHandler> _logger;

        public GetPoolInfoQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetPoolInfoQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<PoolInfoDto?> Handle(GetPoolInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.POOL_INFO}_{request.PoolBech32}";

            try
            {
                _logger.LogInformation("Processing GetPoolInfoQuery for PoolBech32: {PoolBech32} with cache key: {CacheKey}", request.PoolBech32, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching pool info data for PoolBech32: {PoolBech32}", request.PoolBech32);
                        var data = await _poolService.GetPoolInfoAsync(request.PoolBech32);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pool info for pool {PoolBech32}", request.PoolBech32);
                throw;
            }
        }
    }
}