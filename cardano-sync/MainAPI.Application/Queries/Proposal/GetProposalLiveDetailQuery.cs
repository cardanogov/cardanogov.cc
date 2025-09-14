namespace MainAPI.Application.Queries.Proposal
{
    public class GetProposalLiveDetailQuery : IRequest<List<ProposalInfoResponseDto>?>
    {
        public string? ProposalId { get; set; }

        public GetProposalLiveDetailQuery(string? proposalId)
        {
            ProposalId = proposalId;
        }
    }

    public class GetProposalLiveDetailQueryHandler : IRequestHandler<GetProposalLiveDetailQuery, List<ProposalInfoResponseDto>?>
    {
        private readonly IProposalService _proposalService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetProposalLiveDetailQueryHandler> _logger;

        public GetProposalLiveDetailQueryHandler(
            IProposalService proposalService,
            ICacheService cacheService,
            ILogger<GetProposalLiveDetailQueryHandler> logger)
        {
            _proposalService = proposalService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<ProposalInfoResponseDto>?> Handle(GetProposalLiveDetailQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeys.PROPOSAL_LIVE_DETAIL}_{request.ProposalId}";

            try
            {
                _logger.LogInformation("Processing GetProposalLiveDetailQuery for ProposalId: {ProposalId} with cache key: {CacheKey}", request.ProposalId, cacheKey);

                // Use cache-aside pattern with GetOrSetAsync
                var result = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Cache miss - fetching proposal live detail data for ProposalId: {ProposalId}", request.ProposalId);
                        var data = await _proposalService.GetProposalLiveByIdAsync(request.ProposalId);
                        return data;
                    },
                    TimeUtils.GetSecondsUntilEndOfDay() // Cache until end of day as per JS implementation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposal live detail for proposal {ProposalId}", request.ProposalId);
                throw;
            }
        }
    }
}
