namespace MainAPI.Application.Queries.Epoch
{
    public class GetCurrentEpochQuery : IRequest<List<CurrentEpochResponseDto>?>
    {
        // No parameters needed for this query
    }

    public class GetCurrentEpochQueryHandler : IRequestHandler<GetCurrentEpochQuery, List<CurrentEpochResponseDto>?>
    {
        private readonly IEpochService _epochService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetCurrentEpochQueryHandler> _logger;

        public GetCurrentEpochQueryHandler(
            IEpochService epochService,
            ICacheService cacheService,
            ILogger<GetCurrentEpochQueryHandler> logger)
        {
            _epochService = epochService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<CurrentEpochResponseDto>?> Handle(GetCurrentEpochQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.CURRENT_EPOCH;

            try
            {
                _logger.LogInformation("Processing GetCurrentEpochQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching current epoch data");
                        var data = await _epochService.GetCurrentEpochAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current epoch data");
                throw;
            }
        }
    }
}