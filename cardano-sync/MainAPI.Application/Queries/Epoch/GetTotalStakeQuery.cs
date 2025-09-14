namespace MainAPI.Application.Queries.Epoch
{
    public class GetTotalStakeQuery : IRequest<TotalStakeResponseDto?>
    {
    }

    public class GetTotalStakeQueryHandler : IRequestHandler<GetTotalStakeQuery, TotalStakeResponseDto?>
    {
        private readonly IEpochService _epochService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetTotalStakeQueryHandler> _logger;

        public GetTotalStakeQueryHandler(
            IEpochService epochService,
            ICacheService cacheService,
            ILogger<GetTotalStakeQueryHandler> logger)
        {
            _epochService = epochService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TotalStakeResponseDto?> Handle(GetTotalStakeQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOTAL_STAKE;

            try
            {
                _logger.LogInformation("Processing GetTotalStakeQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching total stake data");
                        var data = await _epochService.GetTotalStakeAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total stake data");
                throw;
            }
        }
    }
}