namespace MainAPI.Application.Queries.Proposal
{
    public class GetGovernanceActionsStatisticsByEpochQuery : IRequest<GovernanceActionsStatisticsByEpochResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetGovernanceActionsStatisticsByEpochQueryHandler : IRequestHandler<GetGovernanceActionsStatisticsByEpochQuery, GovernanceActionsStatisticsByEpochResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetGovernanceActionsStatisticsByEpochQueryHandler> _logger;

        public GetGovernanceActionsStatisticsByEpochQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetGovernanceActionsStatisticsByEpochQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionsStatisticsByEpochResponseDto?> Handle(GetGovernanceActionsStatisticsByEpochQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.GOVERNANCE_ACTIONS_STATISTICS_BY_EPOCH;

            try
            {
                _logger.LogInformation("Processing GetGovernanceActionsStatisticsByEpochQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching governance actions statistics by epoch data");
                        var data = await _proposalService.GetGovernanceActionsStatisticsByEpochAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving governance actions statistics by epoch data");
                throw;
            }
        }
    }
}