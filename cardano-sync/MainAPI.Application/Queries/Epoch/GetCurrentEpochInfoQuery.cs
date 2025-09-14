namespace MainAPI.Application.Queries.Epoch
{
    public class GetCurrentEpochInfoQuery : IRequest<EpochInfoResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetCurrentEpochInfoQueryHandler : IRequestHandler<GetCurrentEpochInfoQuery, EpochInfoResponseDto?>
    {
        private readonly IEpochService _epochService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetCurrentEpochInfoQueryHandler> _logger;

        public GetCurrentEpochInfoQueryHandler(
            IEpochService epochService,
            ICacheService cacheService,
            ILogger<GetCurrentEpochInfoQueryHandler> logger)
        {
            _epochService = epochService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<EpochInfoResponseDto?> Handle(GetCurrentEpochInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.CURRENT_EPOCH_INFO;

            try
            {
                _logger.LogInformation("Processing GetCurrentEpochInfoQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching current epoch info data");
                        var data = await _epochService.GetCurrentEpochInfoAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current epoch info data");
                throw;
            }
        }
    }
}