namespace MainAPI.Application.Queries.Pool
{
    public class GetSpoVotingPowerHistoryQuery : IRequest<List<SpoVotingPowerHistoryResponseDto>?>
    {
        // No parameters needed for this query
    }

    public class GetSpoVotingPowerHistoryQueryHandler : IRequestHandler<GetSpoVotingPowerHistoryQuery, List<SpoVotingPowerHistoryResponseDto>?>
    {
        private readonly IPoolService _poolService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetSpoVotingPowerHistoryQueryHandler> _logger;

        public GetSpoVotingPowerHistoryQueryHandler(
            IPoolService poolService,
            ICacheService cacheService,
            ILogger<GetSpoVotingPowerHistoryQueryHandler> logger)
        {
            _poolService = poolService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<SpoVotingPowerHistoryResponseDto>?> Handle(GetSpoVotingPowerHistoryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.SPO_VOTING_POWER_HISTORY;

            try
            {
                _logger.LogInformation("Processing GetSpoVotingPowerHistoryQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching spo voting power history data");
                        var data = await _poolService.GetSpoVotingPowerHistoryAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving spo voting power history data");
                throw;
            }
        }
    }
}