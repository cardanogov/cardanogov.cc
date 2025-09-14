namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalExpiredDetailQuery : IRequest<GovernanceActionResponseDto?>
    {
        public string? ProposalId { get; set; }

        public GetProposalExpiredDetailQuery(string? proposalId)
        {
            ProposalId = proposalId;
        }
    }

    public class GetProposalExpiredDetailQueryHandler : IRequestHandler<GetProposalExpiredDetailQuery, GovernanceActionResponseDto?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalExpiredDetailQueryHandler> _logger;

        public GetProposalExpiredDetailQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalExpiredDetailQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<GovernanceActionResponseDto?> Handle(GetProposalExpiredDetailQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.PROPOSAL_EXPIRED_DETAIL}_{request.ProposalId}";

            try
            {
                _logger.LogInformation("Processing GetProposalExpiredDetailQuery for ProposalId: {ProposalId} with cache key: {CacheKey}", request.ProposalId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal expired detail data for ProposalId: {ProposalId}", request.ProposalId);
                        var data = await _proposalService.GetProposalExpiredByIdAsync(request.ProposalId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal expired detail for proposal {ProposalId}", request.ProposalId);
                throw;
            }
        }
    }
}
