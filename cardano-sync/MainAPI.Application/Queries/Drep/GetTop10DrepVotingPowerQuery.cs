namespace MainAPI.Application.Queries.Drep
{
    public class GetTop10DrepVotingPowerQuery : IRequest<List<DrepVotingPowerHistoryResponseDto>?>
    {
    }

    public class GetTop10DrepVotingPowerQueryHandler : IRequestHandler<GetTop10DrepVotingPowerQuery, List<DrepVotingPowerHistoryResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetTop10DrepVotingPowerQueryHandler> _logger;

        public GetTop10DrepVotingPowerQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetTop10DrepVotingPowerQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepVotingPowerHistoryResponseDto>?> Handle(GetTop10DrepVotingPowerQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.TOP_10_DREP_VOTING_POWER;

            try
            {
                _logger.LogInformation("Processing GetTop10DrepVotingPowerQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching top 10 drep voting power data");
                        var data = await _drepService.GetTop10DrepVotingPowerAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top 10 drep voting power data");
                throw;
            }
        }
    }
}