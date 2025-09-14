namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepsVotingPowerQuery : IRequest<DrepsVotingPowerResponseDto?>
    {
    }

    public class GetDrepsVotingPowerQueryHandler : IRequestHandler<GetDrepsVotingPowerQuery, DrepsVotingPowerResponseDto?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepsVotingPowerQueryHandler> _logger;

        public GetDrepsVotingPowerQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepsVotingPowerQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<DrepsVotingPowerResponseDto?> Handle(GetDrepsVotingPowerQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.DREPS_VOTING_POWER;

            try
            {
                _logger.LogInformation("Processing GetDrepsVotingPowerQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching dreps voting power data");
                        var data = await _drepService.GetDrepsVotingPowerAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dreps voting power data");
                throw;
            }
        }
    }
}