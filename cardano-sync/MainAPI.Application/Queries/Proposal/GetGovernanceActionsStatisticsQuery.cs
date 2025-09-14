namespace MainAPI.Application.Queries.Proposal
{
    public class GetGovernanceActionsStatisticsQuery : IRequest<GovernanceActionsStatisticsResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetGovernanceActionsStatisticsQueryHandler : IRequestHandler<GetGovernanceActionsStatisticsQuery, GovernanceActionsStatisticsResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetGovernanceActionsStatisticsQueryHandler> _logger;

        public GetGovernanceActionsStatisticsQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetGovernanceActionsStatisticsQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionsStatisticsResponseDto?> Handle(GetGovernanceActionsStatisticsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.GOVERNANCE_ACTIONS_STATISTICS;

            try
            {
                _logger.LogInformation("Processing GetGovernanceActionsStatisticsQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching governance actions statistics data");
                        var data = await _proposalService.GetGovernanceActionsStatisticsAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving governance actions statistics data");
                throw;
            }
        }
    }
}