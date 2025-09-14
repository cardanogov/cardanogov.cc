namespace MainAPI.Application.Queries.Pool
{
    public class GetPoolMetadataQuery : IRequest<object?>
    {
        public string PoolId { get; }

        public GetPoolMetadataQuery(string poolId)
        {
            PoolId = poolId;
        }
    }

    public class GetPoolMetadataQueryHandler : IRequestHandler<GetPoolMetadataQuery, object?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetPoolMetadataQueryHandler> _logger;

        public GetPoolMetadataQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetPoolMetadataQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<object?> Handle(GetPoolMetadataQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"POOL_METADA_{request.PoolId}";

            try
            {
                _logger.LogInformation("Processing GetPoolMetadataQuery for PoolId: {PoolId} with cache key: {CacheKey}", request.PoolId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching pool metadata data for PoolId: {PoolId}", request.PoolId);
                        var data = await _poolService.GetPoolMetadataAsync(request.PoolId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pool metadata for pool {PoolId}", request.PoolId);
                throw;
            }
        }
    }
}