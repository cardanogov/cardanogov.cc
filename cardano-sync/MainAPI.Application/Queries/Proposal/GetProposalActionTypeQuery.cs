namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalActionTypeQuery : IRequest<List<ProposalActionTypeResponseDto>?>
    {
        // No parameters needed for this query
    }

    public class GetProposalActionTypeQueryHandler : IRequestHandler<GetProposalActionTypeQuery, List<ProposalActionTypeResponseDto>?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalActionTypeQueryHandler> _logger;

        public GetProposalActionTypeQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalActionTypeQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<ProposalActionTypeResponseDto>?> Handle(GetProposalActionTypeQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.PROPOSAL_ACTION_TYPE;

            try
            {
                _logger.LogInformation("Processing GetProposalActionTypeQuery with cache key: {CacheKey}", cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal action type data");
                        var data = await _proposalService.GetProposalActionTypeAsync();
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal action type data");
                throw;
            }
        }
    }
}