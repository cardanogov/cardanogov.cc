namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepAndPoolVotingThresholdQuery : IRequest<DrepPoolVotingThresholdResponseDto?>
    {
    }

    public class GetDrepAndPoolVotingThresholdQueryHandler : IRequestHandler<GetDrepAndPoolVotingThresholdQuery, DrepPoolVotingThresholdResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepAndPoolVotingThresholdQueryHandler> _logger;

        public GetDrepAndPoolVotingThresholdQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepAndPoolVotingThresholdQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepPoolVotingThresholdResponseDto?> Handle(GetDrepAndPoolVotingThresholdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.DREP_AND_POOL_VOTING_THRESHOLD;

            try
            {
                _logger.LogInformation("Processing GetDrepAndPoolVotingThresholdQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep and pool voting threshold data");
                        var data = await _drepService.GetDrepAndPoolVotingThresholdAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep and pool voting threshold");
                throw;
            }
        }
    }
}