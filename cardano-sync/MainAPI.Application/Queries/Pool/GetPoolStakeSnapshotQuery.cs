namespace MainAPI.Application.Queries.Pool
{
    public class GetPoolStakeSnapshotQuery : IRequest<object?>
    {
        public string PoolBech32 { get; }

        public GetPoolStakeSnapshotQuery(string poolBech32)
        {
            PoolBech32 = poolBech32;
        }
    }

    public class GetPoolStakeSnapshotQueryHandler : IRequestHandler<GetPoolStakeSnapshotQuery, object?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetPoolStakeSnapshotQueryHandler> _logger;

        public GetPoolStakeSnapshotQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetPoolStakeSnapshotQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<object?> Handle(GetPoolStakeSnapshotQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.POOL_STAKE_SNAPSHOT}_{request.PoolBech32}";

            try
            {
                _logger.LogInformation("Processing GetPoolStakeSnapshotQuery for PoolBech32: {PoolBech32} with cache key: {CacheKey}", request.PoolBech32, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching pool stake snapshot data for PoolBech32: {PoolBech32}", request.PoolBech32);
                        var data = await _poolService.GetPoolStakeSnapshotAsync(request.PoolBech32);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pool stake snapshot for pool {PoolBech32}", request.PoolBech32);
                throw;
            }
        }
    }
}