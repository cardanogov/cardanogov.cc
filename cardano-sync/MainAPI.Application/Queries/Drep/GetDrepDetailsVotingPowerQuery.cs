namespace MainAPI.Application.Queries.Drep
{
    public class GetDrepDetailsVotingPowerQuery : IRequest<List<DrepDetailsVotingPowerResponseDto>?>
    {
        public string DrepId { get; }

        public GetDrepDetailsVotingPowerQuery(string drepId)
        {
            DrepId = drepId;
        }
    }

    public class GetDrepDetailsVotingPowerQueryHandler : IRequestHandler<GetDrepDetailsVotingPowerQuery, List<DrepDetailsVotingPowerResponseDto>?>
    {
        private readonly IDrepService _drepService;
        private readonly MainAPI.Core.Interfaces.ICacheService _cacheService;
        private readonly Microsoft.Extensions.Logging.ILogger<GetDrepDetailsVotingPowerQueryHandler> _logger;

        public GetDrepDetailsVotingPowerQueryHandler(
            IDrepService drepService,
            MainAPI.Core.Interfaces.ICacheService cacheService,
            Microsoft.Extensions.Logging.ILogger<GetDrepDetailsVotingPowerQueryHandler> logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<DrepDetailsVotingPowerResponseDto>?> Handle(GetDrepDetailsVotingPowerQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.DREP_DETAILS_VOTING_POWER}_{request.DrepId}";

            try
            {
                _logger.LogInformation("Processing GetDrepDetailsVotingPowerQuery for DrepId: {DrepId} with cache key: {CacheKey}", request.DrepId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching drep details voting power data for DrepId: {DrepId}", request.DrepId);
                        var data = await _drepService.GetDrepDetailsVotingPowerAsync(request.DrepId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drep details voting power for drep {DrepId}", request.DrepId);
                throw;
            }
        }
    }
}