namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepVotingPowerHistoryQuery : IRequest<List<DrepVotingPowerHistoryResponseDto>?>
    {
    }

    public class GetDrepVotingPowerHistoryQueryHandler : IRequestHandler<GetDrepVotingPowerHistoryQuery, List<DrepVotingPowerHistoryResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetDrepVotingPowerHistoryQueryHandler> _logger;

        public GetDrepVotingPowerHistoryQueryHandler(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<GetDrepVotingPowerHistoryQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepVotingPowerHistoryResponseDto>?> Handle(GetDrepVotingPowerHistoryQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.DREP_VOTING_POWER_HISTORY;

            try
            {
                _logger.LogInformation("Processing GetDrepVotingPowerHistoryQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep voting power history data");
                        var data = await _drepService.GetDrepVotingPowerHistoryAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep voting power history data");
                throw;
            }
        }
    }
}