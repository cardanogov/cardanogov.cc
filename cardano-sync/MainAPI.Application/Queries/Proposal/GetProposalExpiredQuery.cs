namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalExpiredQuery : IRequest<GovernanceActionResponseDto?>
    {
        // No parameters needed for this query
    }

    public class GetProposalExpiredQueryHandler : IRequestHandler<GetProposalExpiredQuery, GovernanceActionResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalExpiredQueryHandler> _logger;

        public GetProposalExpiredQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalExpiredQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionResponseDto?> Handle(GetProposalExpiredQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.PROPOSAL_EXPIRED;

            try
            {
                _logger.LogInformation("Processing GetProposalExpiredQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal expired data");
                        var data = await _proposalService.GetProposalExpiredAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal expired data");
                throw;
            }
        }
    }
}