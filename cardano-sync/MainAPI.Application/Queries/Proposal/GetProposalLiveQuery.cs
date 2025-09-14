namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalLiveQuery : IRequest<List<ProposalInfoResponseDto>?>
    {
        // No parameters needed for this query
    }

    public class GetProposalLiveQueryHandler : IRequestHandler<GetProposalLiveQuery, List<ProposalInfoResponseDto>?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalLiveQueryHandler> _logger;

        public GetProposalLiveQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalLiveQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<ProposalInfoResponseDto>?> Handle(GetProposalLiveQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.PROPOSAL_LIVE;

            try
            {
                _logger.LogInformation("Processing GetProposalLiveQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal live data");
                        var data = await _proposalService.GetProposalLiveAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal live data");
                throw;
            }
        }
    }
}
